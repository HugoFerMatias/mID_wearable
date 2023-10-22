using holder;
using ble;
using System;
using PeterO.Cbor;
using retrieval;
using utils;

namespace droid

{
    /*
    * Android Specific Class which contains the shared Holder class.
    * This class contains cryptographic information and methods about the mDL holder.
    * It also contains the Bluetooth Low Energy Peripheral implementation with L2cap CoC.
    */
    public class Droid_Holder : Holder
    {

        PrettyPrint print = new PrettyPrint();

        // Android peripheral
        public Peripheral peripheral;

        public Peripheral Peripheral
        {
            get { return peripheral; }
        }
       
        public Droid_Holder(string parameters) : base(parameters)
        {
            peripheral = new Peripheral();
            DeviceEngagement.PeripheralServer_UUID = DroidUtils.UUIDToByteArray(peripheral.Peripheral_Server_UUID);
            DeviceEngagementBytes = DeviceEngagement.Eng_Map.EncodeToBytes();
            // Eventualmente tirar esta linha de código
            Peripheral.eDeviceKey = DeviceEngagementBytes;
        }

        // Open Gatt server and start service advertisement
        public void InitializePeripheral()
        {
            peripheral.Open();
        }

        /* Initialize L2cap Server Socket and create subsequent connection
         * Must come after StartAdvertising() method
         */
        

        public void CloseConnection()
        {
            peripheral.Close();
        }

        public void SendResponse()
        {
            /*
             * Right now for testing purposes i am using the received mdoc request (droid.Request.EncodeToBytes()) 
             * and sending it again to the reader
             * In the future a mdoc response will be generated and sent instead of this mdoc request
             */
            byte[] enc_mdoc_response = null;
            if (Response != null)
            {
                Console.WriteLine("Response is: " + print.PrintCBOR(Response));
                enc_mdoc_response = EncryptData(Response);
            }
            
            
            CBORObject sessionData;
            // Add state????
            if (Retrieval.SessionStatus == (int)Status.SessionStatusType.Error_CBOR_decoding || Retrieval.SessionStatus == (int)Status.SessionStatusType.Error_session_encryption) 
            {
                sessionData = CBORObject.NewMap().Add("data",Retrieval.SessionStatus);
            }
            else
            {
                
                sessionData = CBORObject.NewMap().Add("data", enc_mdoc_response).Add("status", Retrieval.SessionStatus);
                // Encrypts mDoc response data and sends it to Reader through the l2cap socket
            }

            peripheral.Write(sessionData.EncodeToBytes());
        }

        
    }
}
