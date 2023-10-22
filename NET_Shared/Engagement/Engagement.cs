using PeterO.Cbor;

namespace engagement
{
    public class Engagement
    {

        public CBORObject map;

        public string version = "1.0";
        public string ProtocolInfo = "1";
        public byte[] ble_peripheral_server_uuid;

        public Engagement(byte[] deviceKeyBytes)
        {
            GenerateStructure(deviceKeyBytes);
        }

        public byte[] PeripheralServer_UUID { 
            
            get { return ble_peripheral_server_uuid; }
            set 
            {
                ble_peripheral_server_uuid = value;
                map[2][0][2].Add(10,value);   
            } 
        
        }

        public void GenerateStructure(byte[] deviceKeyBytes)
        {
            
            map = CBORObject.NewMap()
                .Add(0, version)
                .Add(1, CBORObject.NewArray().Add(1).Add(deviceKeyBytes))
                .Add(2, CBORObject.NewArray().Add(CBORObject.NewArray().Add(2).Add(1).Add(CBORObject.NewMap().Add(0, true).Add(1, false))));

        }

        public CBORObject Eng_Map 
        { 
            get { return map; }
            set { map = value; } 
        }


    }

   
    
}