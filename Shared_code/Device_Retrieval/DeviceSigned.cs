using System;
using System.Collections.Generic;

namespace device_retrieval_response
{
    public class DeviceSigned
    {
        private byte[] deviceNameSpacesBytes;
        // DeviceAuth

        public class DeviceSignedItems
        {
            
            public Dictionary<string, DeviceSignedItem> deviceSignedItems;
        }

        public class DeviceSignedItem
        {
            // string é o Identifier da tabela 5 object é o valor do elemento identificado
            public Tuple<string, object> deviceSignedItem;
        }
    }
}