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
            // Guard: invalid/empty/wrong-length hex returns 0 instead of throwing
            if (string.IsNullOrWhiteSpace(hexString))
            {
                return 0;
            }

            if (!uint.TryParse(hexString, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out uint intValue))
            {
                return 0;
            }

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
