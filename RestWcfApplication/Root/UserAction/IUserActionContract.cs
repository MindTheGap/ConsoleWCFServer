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
      UriTemplate = "guessContactUser?userId={userId}")]
    string GuessContactUser(string userId, Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "guessFacebookContactUser?userId={userId}")]
    string GuessFacebookContactUser(string userId, Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "openChat?userId={userId}")]
    string OpenChat(string userId, Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "typing?userId={userId}")]
    string UserIsTyping(string userId, Stream stream);
  }
}
