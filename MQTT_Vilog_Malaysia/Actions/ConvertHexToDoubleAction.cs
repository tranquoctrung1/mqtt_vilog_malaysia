using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class ConvertHexToDoubleAction : IDisposable
    {
        private bool disposed = false;
        public double ConvertHexToDouble(string hexString)
        {
            // Convert hex to byte array (from most to least significant byte)
            uint intValue = Convert.ToUInt32(hexString, 16);
            float floatValue = BitConverter.ToSingle(BitConverter.GetBytes(intValue), 0);
            return floatValue;
        }

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
                    Console.WriteLine("Disposing convert hex to double managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~ConvertHexToDoubleAction()
        {
            Dispose(false);
        }
    }
}
