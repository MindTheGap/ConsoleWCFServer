using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Activation;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using RestWcfApplication.Root.UserAction;
using RestWcfApplication.Root.IAP;
using RestWcfApplication.Root.Register;
using RestWcfApplication.Root.Update;
using RestWcfApplication.Root.Want;

namespace RestWcfApplication
{
  public class Global : System.Web.HttpApplication
  {

    protected void Application_Start(object sender, EventArgs e)
    {
      RouteTable.Routes.Add(new ServiceRoute("want", new WebServiceHostFactory(), typeof(WantContract)));
      RouteTable.Routes.Add(new ServiceRoute("register", new WebServiceHostFactory(), typeof(RegisterContract)));
      RouteTable.Routes.Add(new ServiceRoute("update", new WebServiceHostFactory(), typeof(UpdateContract)));
      RouteTable.Routes.Add(new ServiceRoute("useraction", new WebServiceHostFactory(), typeof(UserActionContract)));
      RouteTable.Routes.Add(new ServiceRoute("iap", new WebServiceHostFactory(), typeof(IAPContract)));
    }

    protected void Session_Start(object sender, EventArgs e)
    {

    }

    protected void Application_BeginRequest(object sender, EventArgs e)
    {

    }

    protected void Application_AuthenticateRequest(object sender, EventArgs e)
    {

    }

    protected void Application_Error(object sender, EventArgs e)
    {

    }

    protected void Session_End(object sender, EventArgs e)
    {

    }

    protected void Application_End(object sender, EventArgs e)
    {

    }
  }
}