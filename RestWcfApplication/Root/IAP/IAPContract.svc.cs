using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using RestWcfApplication.PushSharp;
using RestWcfApplication.Root.Shared;

namespace RestWcfApplication.Root.IAP
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                   ConcurrencyMode = ConcurrencyMode.Multiple)]
  public class IAPContract : IIAPContract
  {
    private const string AppleIapSandboxUrl = "https://sandbox.itunes.apple.com/verifyReceipt";
    private const string AppleIapProductionUrl = "https://buy.itunes.apple.com/verifyReceipt";

    public string Purchase(string userId, Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, stream, out jsonObject, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var sourceUserId = sourceUser.Id;
        var productId = jsonObject["productId"] as string;
        var transcationId = jsonObject["transcationId"] as string;
        var receiptData = jsonObject["receiptData"] as string;
        if (transcationId == null || productId == null || receiptData == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var product = SharedHelper.QueryForObject<Product>("Products", p => p.ProductId == productId);
        if (product == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var responseJsonObject = CheckTransactionReceipt(AppleIapProductionUrl, receiptData);
        var statusStr = responseJsonObject["status"].ToString();
        if (statusStr == "21007")
        {
          responseJsonObject = CheckTransactionReceipt(AppleIapSandboxUrl, receiptData);
          statusStr = responseJsonObject["status"].ToString();
        }

        if (statusStr != "0")
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = string.Format("status code ({0}) is not zero", statusStr);
          return CommManager.SendMessage(toSend);
        }

        var result = CheckIapResponseIntegrity(responseJsonObject, product);
        if (result)
        {
          using (var context = new Entities())
          {
            context.Configuration.ProxyCreationEnabled = false;

            context.Users.Attach(sourceUser);
            context.Products.Attach(product);

            sourceUser.Coins += product.CoinsAmount;

            var newNotification = new Notification
            {
              UserId = sourceUserId,
              CoinAmount = product.CoinsAmount,
              Text = string.Format("You purchased {0} coins!", product.CoinsAmount)
            };

            context.Notifications.Add(newNotification);

            context.SaveChanges();
          }
        }

        toSend.CoinAmount = sourceUser.Coins;
        toSend.responseText = responseJsonObject;
        toSend.Type = EMessagesTypesToClient.Ok;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public JObject CheckTransactionReceipt(string url, string receiptData)
    {
      var json = new JObject(new JProperty("receipt-data", receiptData)).ToString();

      byte[] postBytes = Encoding.UTF8.GetBytes(json);

      var request = WebRequest.Create(url);
      request.Method = "POST";
      request.ContentType = "application/json";
      request.ContentLength = postBytes.Length;

      using (var stream = request.GetRequestStream())
      {
        stream.Write(postBytes, 0, postBytes.Length);
        stream.Flush();
      }

      var sendResponse = request.GetResponse();
      var buf = new byte[8192];
      var resStream = sendResponse.GetResponseStream();
      if (resStream == null) return null;

      var sb = new StringBuilder();
      var count = 0;
      do
      {
        count = resStream.Read(buf, 0, buf.Length);
        if (count != 0)
        {
          var tempString = Encoding.ASCII.GetString(buf, 0, count);
          sb.Append(tempString);
        }
      }
      while (count > 0);
      var fd = JObject.Parse(sb.ToString());

      return fd;
    }

    private bool CheckIapResponseIntegrity(JObject fd, Product product)
    {
      var receipt = fd["receipt"];

      // Product ID does not match what we expected
      var productIdStr = receipt["in_app"][0]["product_id"].ToString().Replace("\"", "").Trim();
      if (productIdStr != product.ProductId)
      {
        throw new Exception(string.Format("receipt.product_id ({0}) != productId ({1})", productIdStr, product.ProductId));
      }

      // This transaction didn't occur within 24 hours in either direction; somebody is reusing a receipt
      var transDate = DateTime.SpecifyKind(DateTime.Parse(receipt["in_app"][0]["purchase_date"].ToString().Replace("\"", "").Replace("Etc/GMT", "")), DateTimeKind.Utc);
      var delay = DateTime.UtcNow - transDate;
      if (delay.TotalHours > 24 || delay.TotalHours < -24)
      {
        // returning true because we want to dismiss this request for it not to come back
        // but we don't do any change to the DB
        return false;
      }

      return true;
    }

    public string GetAllPurchases(string userId, Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, stream, out jsonObject, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        List<string> productIds;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          productIds = context.Products.Select(p => p.ProductId).ToList();
        }

        toSend.MultipleMessages = productIds;
        toSend.Type = EMessagesTypesToClient.Ok;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }
  }
}
