using crypto;
using System.Text;

namespace device_retrieval_response
{
    public class ConnectionSetup
    {
        public byte[] IdentCharValue(byte[] EDeviceKeyBytes,string BLEIdent)
        {
            //byte[] length = new byte[16];
            byte[] BLEIdentbytes = Encoding.ASCII.GetBytes(BLEIdent);
            // LENGTH 16 BYTES
            return HKDF.DeriveKey(null,EDeviceKeyBytes,BLEIdentbytes,16); 
        }
    }
}