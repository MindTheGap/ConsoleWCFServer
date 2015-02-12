using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using PushSharp;
using PushSharp.Apple;
using PushSharp.Core;

namespace RestWcfApplication.PushSharp
{
  public static class PushManager
  {
    private static readonly PushBroker PushBrokerDev = new PushBroker();
    private static readonly PushBroker PushBrokerProd = new PushBroker();


    static PushManager()
    {
      PushBrokerDev.OnNotificationSent += NotificationSent;
      PushBrokerDev.OnChannelException += ChannelException;
      PushBrokerDev.OnServiceException += ServiceException;
      PushBrokerDev.OnNotificationFailed += NotificationFailed;
      PushBrokerDev.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
      PushBrokerDev.OnDeviceSubscriptionChanged += DeviceSubscriptionChanged;
      PushBrokerDev.OnChannelCreated += ChannelCreated;
      PushBrokerDev.OnChannelDestroyed += ChannelDestroyed;

      PushBrokerProd.OnNotificationSent += NotificationSent;
      PushBrokerProd.OnChannelException += ChannelException;
      PushBrokerProd.OnServiceException += ServiceException;
      PushBrokerProd.OnNotificationFailed += NotificationFailed;
      PushBrokerProd.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
      PushBrokerProd.OnDeviceSubscriptionChanged += DeviceSubscriptionChanged;
      PushBrokerProd.OnChannelCreated += ChannelCreated;
      PushBrokerProd.OnChannelDestroyed += ChannelDestroyed;

      try
      {
        //Registering the Apple Service and sending an iOS Notification
        var appleCertProd = File.ReadAllBytes("HowToNotificationsProd.p12");
        PushBrokerProd.RegisterAppleService(new ApplePushChannelSettings(true, appleCertProd, @"aaazzz123"));
        var appleCertDev = File.ReadAllBytes("HowToNotificationsDev.p12");
        PushBrokerDev.RegisterAppleService(new ApplePushChannelSettings(false, appleCertDev, @"aaazzz123"));
      }
      catch (Exception e)
      {
        Console.WriteLine("Error: {0}", e.Message);
      }
    }

    public static void PushToIos(string deviceId, string alert)
    {
      PushBrokerDev.QueueNotification(new AppleNotification()
                                 .ForDeviceToken(deviceId)
                                 .WithAlert(alert)
                                 .WithSound("default"));
      PushBrokerProd.QueueNotification(new AppleNotification()
                                 .ForDeviceToken(deviceId)
                                 .WithAlert(alert)
                                 .WithSound("default"));
    }

    public static void PushToIos(string deviceId)
    {
      PushBrokerDev.QueueNotification(new AppleNotification()
                                 .ForDeviceToken(deviceId));
      PushBrokerProd.QueueNotification(new AppleNotification()
                                 .ForDeviceToken(deviceId));
    }

    static void DeviceSubscriptionChanged(object sender,
        string oldSubscriptionId, string newSubscriptionId, INotification notification)
    {
      //Do something here
    }

    //this even raised when a notification is successfully sent
    static void NotificationSent(object sender, INotification notification)
    {
      //Do something here
    }

    //this is raised when a notification is failed due to some reason
    static void NotificationFailed(object sender,
    INotification notification, Exception notificationFailureException)
    {
      var error = notificationFailureException as NotificationFailureException;

      int i = 2;
      i++;
      //Do something here
    }

    //this is fired when there is exception is raised by the channel
    static void ChannelException
      (object sender, IPushChannel channel, Exception exception)
    {
      //Do something here
    }

    //this is fired when there is exception is raised by the service
    static void ServiceException(object sender, Exception exception)
    {
      //Do something here
    }

    //this is raised when the particular device subscription is expired
    static void DeviceSubscriptionExpired(object sender,
    string expiredDeviceSubscriptionId,
      DateTime timestamp, INotification notification)
    {
      //Do something here
    }

    //this is raised when the channel is destroyed
    static void ChannelDestroyed(object sender)
    {
      //Do something here
    }

    //this is raised when the channel is created
    static void ChannelCreated(object sender, IPushChannel pushChannel)
    {
      //Do something here
    }
  }
}