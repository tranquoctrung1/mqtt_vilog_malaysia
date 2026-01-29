using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class WriteJsonFileAction: IDisposable
    {
        private string LogPathDir = @"./Log";

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
                    Console.WriteLine("Disposing write json file managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~WriteJsonFileAction()
        {
            Dispose(false);
        }

        public async Task WriteJsonFileAsync<T>(string fileName, T data)
        {
            try
            {
                if (!Directory.Exists(LogPathDir))
                {
                    Directory.CreateDirectory(LogPathDir);
                }

                string filePath = Path.Combine(LogPathDir, fileName);

                var wrapper = new
                {
                    TimeRecieveData = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    Data = data
                };

                string jsonString = JsonSerializer.Serialize(
                    wrapper,
                    new JsonSerializerOptions { WriteIndented = true }
                );

                await File.AppendAllTextAsync(filePath, jsonString + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing JSON file: {ex.Message}");
            }
        }
    }
}
