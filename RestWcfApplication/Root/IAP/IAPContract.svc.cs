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

namespace RestWcfApplication.Root.IAP
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class IAPContract : IIAPContract
  {
    public string Purchase(string userId, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(text);
        if (jsonObject == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var productId = jsonObject["productId"] as string;
        var transcationId = jsonObject["transcationId"] as string;
        var receiptDataStr = jsonObject["receiptData"] as string;
        if (transcationId == null || productId == null || receiptDataStr == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }
        var receiptDataByte = Convert.FromBase64String(receiptDataStr);
        var receiptData = Encoding.UTF8.GetString(receiptDataByte);

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          sourceUser.LastSeen = DateTime.Now.ToString("u");

          var product = context.Products.FirstOrDefault(p => p.ProductId == productId);
          if (product == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          var responseText = CheckTransactionReceipt(receiptData);



          //sourceUser.Coins += product.CoinsAmount;


          //context.SaveChanges();

          // create a new notification
          toSend.responseText = responseText;
          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.Error = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public string CheckTransactionReceipt(string receiptData)
    {
      var json = new JObject(new JProperty("receipt-data", receiptData)).ToString();


      byte[] postBytes = Encoding.UTF8.GetBytes(json);

      var request = WebRequest.Create("https://sandbox.itunes.apple.com/verifyReceipt");
      request.Method = "POST";
      request.ContentType = "application/json";
      request.ContentLength = postBytes.Length;

      using (var stream = request.GetRequestStream())
      {
        stream.Write(postBytes, 0, postBytes.Length);
        stream.Flush();
      }

      var sendresponse = request.GetResponse();

      var responseStream = sendresponse.GetResponseStream();
      if (responseStream != null)
      {
        string sendResponseText;
        using (var streamReader = new StreamReader(responseStream))
        {
          sendResponseText = streamReader.ReadToEnd().Trim();
        }
        return sendResponseText;
      }

      return string.Empty;
    }

    public string GetAllPurchases(string userId, System.IO.Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(text);
        if (jsonObject == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          sourceUser.LastSeen = DateTime.Now.ToString("u");

          var products = context.Products.ToList();

          toSend.MultipleMessages = products;
          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.Error = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }
  }
}
