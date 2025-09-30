using MongoDB.Bson;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class CheckImeiAvailableAction: IDisposable
    {
        public async Task<bool> CheckImeiAvailable(string imei, string url)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            bool check = false;

            try
            {
                using var client = new HttpClient();

                string urlGet = $"{url}/getImeiById?imei={imei}";

                var res = await client.GetAsync(urlGet);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();

                    var docs = JsonSerializer.Deserialize<ImeiModel>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if(docs.Imei != "")
                    {
                        if (docs.Use == false)
                        {
                            check = true;
                        }
                    }
                    
                }
                else
                {
                    await writeLogAction.WriteErrorLog($"{imei} not found");
                }

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }


            return check;
        }

        public async void UpdateUsedForImei(string imei, string url)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                using var client = new HttpClient();
                var body = new { Imei = imei, Use = true };
                string bb = JsonSerializer.Serialize(body);

                string urlUpdate = $"{url}/updateImei";

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), urlUpdate)
                {
                    Content = new StringContent(bb, Encoding.UTF8, "application/json")
                };

                var res = await client.SendAsync(request);

                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();

                    Console.WriteLine(json);
                }
                else
                {
                    await writeLogAction.WriteErrorLog($"{imei} not found");
                }

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

        }

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
                    Console.WriteLine("Disposing check imei managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~CheckImeiAvailableAction()
        {
            Dispose(false);
    }
}
}
