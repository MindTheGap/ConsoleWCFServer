using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace RestWcfApplication.Communications
{
  public static class CommManager
  {
    public static string SendMessage(dynamic dynamicObject)
    {
      string responseString = JsonConvert.SerializeObject(dynamicObject);

      // Construct a response.
      //byte[] buffer = Encoding.Unicode.GetBytes(responseString);
      
      return responseString;
    }
  }
}