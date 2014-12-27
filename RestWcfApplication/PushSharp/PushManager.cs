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
    public static void PushToIos(string deviceId, string alert)
    {
      //Create our push services broker
      var push = new PushBroker();

      push.OnNotificationSent += NotificationSent;
      push.OnChannelException += ChannelException;ush.OnServiceException += ServiceException;
      push.OnNotificationFailed += NotificationFailed;
      push.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
      push.OnDeviceSubscriptionChanged += DeviceSubscriptionChanged;
      push.OnChannelCreated += ChannelCreated;
      push.OnChannelDestroyed += ChannelDestroyed;

      //Registering the Apple Service and sending an iOS Notification
      var appleCert = File.ReadAllBytes("HowToNotifications.p12");
      push.RegisterAppleService(new ApplePushChannelSettings(false, appleCert, @"aaazzz123"));
      push.QueueNotification(new AppleNotification()
                                 .ForDeviceToken(deviceId)
                                 .WithAlert(alert));

      push.StopAllServices();
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