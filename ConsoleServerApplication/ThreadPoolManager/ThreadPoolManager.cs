using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using ChildsTubeConsoleServer.DB;
using ChildsTubeConsoleServer.Helpers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Dynamic;
using ChildsTubeConsoleServer.ViewModel;
using ChildsTubeConsoleServer.Communications;
using ChildsTubeConsoleServer.Communications.Models;
using Newtonsoft.Json.Linq;

namespace ChildsTubeConsoleServer.ThreadPoolManagerNS
{
    public class ThreadPoolManager
    {
        MainWindowViewModel MainWindowViewModel { get; set; }

        public CommunicationsManager CommunicationsManager { get; set; }

        public ThreadPoolManager(MainWindowViewModel mainWindowViewModel)
        {
            this.MainWindowViewModel = mainWindowViewModel;

            CommunicationsManager = new CommunicationsManager(mainWindowViewModel);
        }

        public void AddTask(HttpListenerContext context)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadPoolWorkerCallback), context);
        }

        private void ThreadPoolWorkerCallback(object state)
        {
            var context = state as HttpListenerContext;
            if (context == null) return;

            var request = context.Request;
            if (request != null && request.HasEntityBody)
            {
                using (var body = request.InputStream) // here we have data
                {
                    using (var reader = new StreamReader(body, request.ContentEncoding))
                    {
                        string strData = reader.ReadToEnd();

                        dynamic dynamicObject = JsonConvert.DeserializeObject(strData);

                        CommunicationsHelper.UserToServerMessage? type = dynamicObject.Type;
                        if (type != null)
                        {
                            switch (type)
                            {
                                case CommunicationsHelper.UserToServerMessage.GetAllTvSeries:

                                    CommunicationsManager.HandleGetAllTvSeries(dynamicObject, context);

                                    break;

                                case CommunicationsHelper.UserToServerMessage.SearchTvSeries:

                                    CommunicationsManager.HandleSearchTvSeries(dynamicObject, context);
                                    
                                    break;

                              case CommunicationsHelper.UserToServerMessage.GetEpisodesForTvSeries:

                                    CommunicationsManager.HandleGetEpisodesForTvSeries(dynamicObject, context);

                                    break;

                                default:

                                    MainWindowViewModel.LogManager.PrintWarningMessage("User --> Server: Unknown");

                                    break;
                                
                            }
                        }
                    }
                }
            }
        }


        //private void HandleGetAllTvSeries(dynamic dynamicObject, HttpListenerContext context)
        //{
        //    try
        //    {
        //        dynamic sendFlexible = new ExpandoObject();
        //        sendFlexible.Type = CommunicationsHelper.ServerToUserMessage.OK;
        //        var weddings = from wedding in MainWindowViewModel.dataEntities.Wedding
        //                       where wedding.Member.First_Name.Contains("Chen") || wedding.Member1.First_Name.Contains("Chen") || wedding.Member.First_Name.Contains("Eti") || wedding.Member1.First_Name.Contains("Eti")
        //                       select wedding;

        //        List<ExpandoObject> flexibleWeddingList = new List<ExpandoObject>();
        //        foreach (var weddingLinq in weddings)
        //        {
        //            dynamic flexibleWedding = new ExpandoObject();
        //            flexibleWedding.BrideFullName = weddingLinq.Member.First_Name + " " + weddingLinq.Member.Last_Name;
        //            flexibleWedding.GroomFullName = weddingLinq.Member1.First_Name + " " + weddingLinq.Member1.Last_Name;
        //            flexibleWedding.Date = weddingLinq.Date;
        //            flexibleWedding.Place = weddingLinq.Place;
        //            if (weddingLinq.Photo != null)
        //                flexibleWedding.Image = weddingLinq.Photo.Image_Path;
        //            flexibleWeddingList.Add(flexibleWedding);
        //        }

        //        sendFlexible.Weddings = flexibleWeddingList;

        //        SendMessage(context, sendFlexible);
        //    }
        //    catch (Exception exception)
        //    {
        //        LogAddMessage("Message has exception: " + exception.Message + ". InnerMessage: " + exception.InnerException);
        //    }
        //}

        //private void HandleLikeGreetingMessage(dynamic dynamicObject, HttpListenerContext context)
        //{
        //    try
        //    {
        //        Member userOut = null;
        //        List<Member> membersList = MainWindowViewModel.dataEntities.Member.ToList();
        //        string email = dynamicObject.Email;
        //        if (UserExists(membersList, email, out userOut) == true)
        //        {
        //            Debug.Assert(userOut != null);

        //            if (userOut.Is_Blocked == true)
        //            {
        //                LogAddMessage("Received message from blocked user: " + dynamicObject.UserFirstName + " " + dynamicObject.UserLastName + ". ignoring.");

        //                return;
        //            }

        //            int? greetingId = dynamicObject.GreetingId;
        //            if (greetingId != null)
        //            {
        //                foreach (var greeting in this.MainWindowViewModel.dataEntities.Greeting)
        //                {
        //                    if (greeting.Greeting_ID == (int)greetingId)
        //                    {
        //                        greeting.Like.Add(new Like() { Greeting_ID = greetingId, Member_ID = userOut.Member_ID });
                                
        //                        dynamic sendFlexible = new ExpandoObject();
        //                        sendFlexible.Type = Helpers.Helpers.MessageTypeToClient.AOK;

        //                        SendMessage(context, sendFlexible);

        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        LogAddMessage("Message has exception: " + exception.Message + ". InnerMessage: " + exception.InnerException);
        //    }
        //}


        //private void HandleSearchResultsMessage(dynamic dynamicObject, HttpListenerContext context)
        //{
        //    try
        //    {
        //        dynamic sendFlexible = new ExpandoObject();
        //        sendFlexible.Type = Helpers.Helpers.MessageTypeToClient.AOK;
        //        var weddings = from wedding in MainWindowViewModel.dataEntities.Wedding
        //                       where wedding.Member.First_Name.Contains("Chen") || wedding.Member1.First_Name.Contains("Chen") || wedding.Member.First_Name.Contains("Eti") || wedding.Member1.First_Name.Contains("Eti")
        //                       select wedding;

        //        List<ExpandoObject> flexibleWeddingList = new List<ExpandoObject>();
        //        foreach (var weddingLinq in weddings.Take(10))
        //        {
        //            dynamic flexibleWedding = new ExpandoObject();
        //            flexibleWedding.BrideFullName = weddingLinq.Member.First_Name + " " + weddingLinq.Member.Last_Name;
        //            flexibleWedding.GroomFullName = weddingLinq.Member1.First_Name + " " + weddingLinq.Member1.Last_Name;
        //            flexibleWedding.Date = weddingLinq.Date;
        //            flexibleWedding.Place = weddingLinq.Place;
        //            if (weddingLinq.Photo != null)
        //                flexibleWedding.Image = weddingLinq.Photo.Image_Path;
        //            flexibleWeddingList.Add(flexibleWedding);
        //        }

        //        sendFlexible.Results = flexibleWeddingList;

        //        SendMessage(context, sendFlexible);
        //    }
        //    catch (Exception exception)
        //    {
        //        LogAddMessage("Message has exception: " + exception.Message + ". InnerMessage: " + exception.InnerException);
        //    }
        //}

        

        private void SendMessage(HttpListenerContext context, dynamic dynamicObject)
        {
            // Obtain a response object.
            HttpListenerResponse response = context.Response;

            string responseString = JsonConvert.SerializeObject(dynamicObject);

            MainWindowViewModel.LogManager.PrintSuccessMessage("Server --> User: " + responseString);

            // Construct a response.
            byte[] buffer = Encoding.Unicode.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            response.ContentEncoding = Encoding.Unicode;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }
    }
}
