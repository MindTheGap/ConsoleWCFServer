using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace RestWcfApplication.Root.UserAction
{
  [ServiceContract]
  public interface IUserActionContract
  {
    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "guessContactUser?userId={userId}&token={token}")]
    string GuessContactUser(string userId, string token, Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "guessFacebookContactUser?userId={userId}&token={token}")]
    string GuessFacebookContactUser(string userId, string token, Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "openChat?userId={userId}&token={token}")]
    string OpenChat(string userId, string token, Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "typing?userId={userId}&token={token}")]
    string UserIsTyping(string userId, string token, Stream stream);
  }
}
