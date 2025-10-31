using Google.Apis.Auth.OAuth2;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class NotificationAction : IDisposable
    {
        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Chặn finalizer gọi lại Dispose
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Giải phóng managed resources ở đây
                    Console.WriteLine("Disposing notification...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~NotificationAction()
        {
            Dispose(false);
        }

        public async Task<bool> SubmitNotification(string loggerID, string title, string body, List<DeviceTokenAppModel> listTokenApp)
        {

            // Replace with the path to your service account key file
            string serviceAccountKeyFilePath = "C:\\web\\fbkey.json";

            // Replace with the required scopes, for example, to access Google Drive
            string[] scopes = { "https://www.googleapis.com/auth/firebase.messaging", "https://www.googleapis.com/auth/cloud-platform" };

            string accessToken = GetAccessToken(serviceAccountKeyFilePath, scopes);

            List<string> token = new List<string>();
            foreach(DeviceTokenAppModel el in listTokenApp)
            {
                token.Add(el.DeviceToken);
            }

            PushNotification(accessToken, token, title, body);

            return true;
        }

        public string GetAccessToken(string serviceAccountKeyFilePath, string[] scopes)
        {
            GoogleCredential credential;

            // Load the service account key file
            using (var stream = new FileStream(serviceAccountKeyFilePath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(scopes);
            }

            // Request an access token
            var token = credential.UnderlyingCredential.GetAccessTokenForRequestAsync().Result;

            return token;
        }

        public void PushNotification(string accessToken, List<string> fcmToken, string titleNoti, string bodyNoti)
        {
            for (int i = 0; i < fcmToken.Count; i++)
            {
                //check status sound
                try
                {
                    //server key meesapp
                    //cannot update applicationID
                    var applicationID = accessToken;
                    var senderId = "1071109504781";
                    HttpWebRequest tRequest = (HttpWebRequest)WebRequest.Create("https://fcm.googleapis.com/v1/projects/iviwater-malaysia/messages:send");
                    tRequest.Method = "post";
                    tRequest.ContentType = "application/json";

                    MessageModel message = new MessageModel();
                    message.token = fcmToken[i];
                    message.notification = new NotificationModel();
                    message.notification.title = titleNoti;
                    message.notification.body = bodyNoti;

                    var data = new
                    {
                        message = message
                    };

                    var json = JsonSerializer.Serialize(data);

                    Byte[] byteArray = Encoding.UTF8.GetBytes(json);

                    tRequest.Headers.Add(string.Format("Authorization: Bearer {0}", applicationID));

                    tRequest.Headers.Add(string.Format("Sender: id={0}", senderId));


                    tRequest.ContentLength = byteArray.Length;
                    //add new
                    tRequest.UseDefaultCredentials = true;
                    tRequest.PreAuthenticate = true;
                    tRequest.Credentials = CredentialCache.DefaultCredentials;

                    var dataStream = tRequest.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    //add new
                    using (WebResponse tResponse = tRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                String sResponseFromServer = tReader.ReadToEnd();
                                string str = sResponseFromServer;
                            }
                        }
                    }
                }

                catch (Exception ex)
                {

                    string str = ex.Message;

                }
            }
        }

    }
}
