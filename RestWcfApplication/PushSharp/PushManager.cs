using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using PushSharp;
using PushSharp.Apple;

namespace RestWcfApplication.PushSharp
{
  public static class PushManager
  {
    public static void PushToIos(string deviceId, string alert, int badge = 7, string sound = "sound.caf")
    {
      //Create our push services broker
      var push = new PushBroker();

      //Registering the Apple Service and sending an iOS Notification
      var appleCert = File.ReadAllBytes("ApnsSandboxCert.p12");
      push.RegisterAppleService(new ApplePushChannelSettings(appleCert, "pwd"));
      push.QueueNotification(new AppleNotification()
                                 .ForDeviceToken(deviceId)
                                 .WithAlert(alert)
                                 .WithBadge(badge)
                                 .WithSound(sound));

    }
  }
}