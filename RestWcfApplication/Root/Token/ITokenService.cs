using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace RestWcfApplication.Root.Token
{
  [ServiceContract]
  public interface ITokenService
  {
    /// <summary>
    /// Generates token via email address
    /// </summary>
    /// <param name="email"></param>
    /// <returns>token string</returns>
    [OperationContract]
    [WebInvoke(Method = "GET", UriTemplate = "generate?email={email}&password={password}")]
    string GenerateTokenByEmail(string email, string password);

    /// <summary>
    /// Generates token via phone number
    /// </summary>
    /// <param name="email"></param>
    /// <returns>token string</returns>
    [OperationContract]
    [WebInvoke(Method = "GET", UriTemplate = "generate?phone={phone}&password={password}")]
    string GenerateTokenByPhone(string phoneNumber, string password);
  }
}
