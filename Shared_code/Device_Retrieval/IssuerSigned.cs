using System;
using System.Collections.Generic;

namespace device_retrieval_response
{
    public class IssuerSigned
    {
        public static IssuerNameSpaces issuerNameSpaces;
        public static IssuerAuth issuerAuth;

        public static Dictionary<string, Tuple<uint, byte[]>> IssuerNameSpaces_forAuth 
        { 
            get { return issuerNameSpaces.IssuerNameSpaces_forAuth; }
            set { issuerNameSpaces.IssuerNameSpaces_forAuth = value; }
        }

        public class IssuerNameSpaces
        {
            Dictionary<string, Tuple<uint,byte[]>> issuerNameSpaces = new Dictionary<string, Tuple<uint, byte[]>>(); //issurSignedItemBytes e respectivo namespace
            byte[] IssuerSignedItemBytes; // IssuerSignedItem em bstr .cbor 

            public Dictionary<string, Tuple<uint, byte[]>> IssuerNameSpaces_forAuth
            {
                get { return issuerNameSpaces; }
                set { issuerNameSpaces = value; }
            }
        }

        public class IssuerSignedItem
        {
            uint digestID;
            string dataElementIdentifier;
            object dataElementValue;


            public uint DigestID 
            { 
                get { return digestID; } 
                set { digestID = value; } 
            }
        }

    }
}