using System;
using server_retrieval;
using System.Collections.Generic;
using Java.Util;
using PeterO.Cbor;

namespace device_engagement
{
    [Serializable]
    public class Device_Engagement
    {

        public CBORObject map;

        public byte[] ble_address;
        public string version = "1.0";
        public Security _Security;
        public DeviceRetrievalMethods _TransferMethods; /* Apenas para device engagement com Qr code */
        //public ServerRetrievalMethods ServerRetrievalMethods = new ServerRetrievalMethods();
        public string ProtocolInfo = "1";
        

        public Device_Engagement(byte[] ?deviceKeyBytes/*, Document mDoc = null*/)
        {
            _Security = new Security();
            _TransferMethods = new DeviceRetrievalMethods();
            DeviceKeyPubBytes = deviceKeyBytes;

            
        }


        /*public void dev_eng_cbor(CBORObject ?PUBMAP)
        {
            map = CBORObject.NewMap()
                .Add(0, version)
                .Add(1, CBORObject.NewArray().Add(1).Add(DeviceKeyPubBytes))
                .Add(2, CBORObject.NewArray().Add(CBORObject.NewArray().Add(2).Add(1).Add(CBORObject.NewMap().Add(0, true).Add(1, false).Add(11,ble_address))));

        }*/

        public void dev_eng_cbor(CBORObject? PUBMAP)
        {
            map = CBORObject.NewMap()
                .Add(0, version)
                .Add(1, CBORObject.NewArray().Add(1).Add(DeviceKeyPubBytes))
                .Add(2, CBORObject.NewArray().Add(CBORObject.NewArray().Add(2).Add(1).Add(CBORObject.NewMap().Add(0, true).Add(1, false).Add(11, ble_address))));

        }

        public CBORObject Eng_Map 
        { 
            get { return map; }
            set { map = value; } 
        }

        public byte[] BLE_Device_Address
        {
            get { return ble_address; }
            set { ble_address = value; }
        }

        public Device_Engagement()
        {
            _Security = new Security();
            _TransferMethods = new DeviceRetrievalMethods();

        }

        public byte[] DeviceKeyPubBytes
        {
            get { return _Security.DeviceKeyPub; }
            set { _Security.DeviceKeyPub = value; }
        }

        public Device_Engagement(DeviceRetrievalMethods deviceRetrievalMethods, ServerRetrievalMethods serverRetrievalMethods)
        {
           
            this._TransferMethods = deviceRetrievalMethods;
            //this.ServerRetrievalMethods = serverRetrievalMethods;
        }

        /*public byte[] Key_Exchange()
        {
            return Security.KeyExchange(this.BuildUrl());
        }*/


    }


    [Serializable]
    public class Security
    {
        private int Cipher_ID;
        private byte[] EDeviceKeyBytes;

        public Security()
        {
            this.Cipher_ID = 1;

        }

        public byte[] DeviceKeyPub
        {
            get { return EDeviceKeyBytes; }
            set { EDeviceKeyBytes = value; }
        }
    }

    [Serializable]
    public class DeviceRetrievalMethods
    {

        // Para testes, remover depois
        public int _type = 2; 

        // uint é o type
        private Dictionary<uint, DeviceRetrieval_Method> Methods = new Dictionary<uint, DeviceRetrieval_Method>();

        // Por enquanto apenas BLE
        public DeviceRetrievalMethods()
        {
            /* Ciclo para quando forem implementados outros metodos para além do BLE
            int threshold = methods.Length;

            for (uint i = 1; i < threshold; i++)
            {
                Methods.Add(i,methods[i]);   
            }*/

            // 2 = BLE
            Methods.Add(2, new DeviceRetrieval_Method());
        }

        [Serializable]
        public class DeviceRetrieval_Method
        {
            // 1 = NFC; 2 = BLE ; 3 = Wifi-Aware 
            private uint type = 2;
            private uint version = 1;
            private Device_Retrieval_Options Device_Retrieval_Options = new BleOptions(true,true,null); // BleOptions porque apenas se esta a considerar BLE

            public uint GetMethodType()
            {
                return type;
            }
        }

        [Serializable]
        abstract class Device_Retrieval_Options
        {
            uint type;
            public uint Type
            {
                get { return type; }
                set { type = value; }
            }

        }

        [Serializable]
        class NfcOptions : Device_Retrieval_Options
        {
            uint Max_Length_Command_Field;
            uint Max_Length_Response_Field;
        }

        [Serializable]
        class BleOptions : Device_Retrieval_Options
        {
            bool Support_Peripheral_Server_Mode;
            bool Support_Central_Client_Mode;
            public byte[] Peripheral_Server_Mode_UUID = Guid.NewGuid().ToByteArray();
            public byte[] Central_Client_Mode_UUID = Guid.NewGuid().ToByteArray(); // Encoded according to RFC 4122
            public byte[] Peripheral_Server_Mode_Device_Address; // Endereço do dispositivo BLE para conecção mais rápida do que com identificação po UUID
            
            public BleOptions(bool psm, bool ccm, byte[]? address)
            {
                Support_Peripheral_Server_Mode = psm;
                Support_Central_Client_Mode = ccm;    
                BLE_Device_Address = address;
            }

            public byte[] Peripheral_Server_UUID
            {
                get { return Peripheral_Server_Mode_UUID; }
                set { Peripheral_Server_Mode_UUID = value; }

            }

            public byte[] Central_Client_UUID
            {
                get { return Central_Client_Mode_UUID; }
                set { Central_Client_UUID = value; }
            }

            public byte[] BLE_Device_Address
            {
                get { return Peripheral_Server_Mode_Device_Address; }
                set { Peripheral_Server_Mode_Device_Address = value; }
            }


        }

        [Serializable]
        class WifiOptions : Device_Retrieval_Options
        {
            string Pass_Phrase_Info;
            uint Channel_Info_Op_Class;
            uint Channel_Info_Number;
            byte[] Band_Info_Supported_Bands;
        }

        public class SessionEstablishment
        {
            public byte[] EReaderKeyBytes;
            byte[] data; // Encrypted mdoc Request

            public SessionEstablishment(byte[] eReaderBytes, byte[] d)
            {
                EReaderKeyBytes = eReaderBytes;
                data = d;
            }

            public byte[] Data
            {
                get { return data; }
                set { data = value; }
            }

            public byte[] ReaderPubKey
            {
                get { return EReaderKeyBytes; }
                set { EReaderKeyBytes = value; }
            }
        }

        public class SessionData
        {
            public byte[] data;
            public uint status;

            public SessionData(byte[] d, uint s)
            {
                data = d;
                status = s;
            }

            public byte[] Data
            {
                get { return data; }
                set { data = value; }
            }

            public byte[] Status
            {
                get { return Status; }
                set { Status = value; }
            }

        }
    }
}