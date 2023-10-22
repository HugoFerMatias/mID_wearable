using PeterO.Cbor;
using System;
using holder;
using System.Collections.Generic;
using System.IO;

using utils;

namespace retrieval
{

    public static class Status
    {

        public enum RetrievalStatusType
        {
            OK = 0,
            General_error = 10,
            CBOR_decoding_error = 11,
            CBOR_validation_error = 12,
        }

        public enum SessionStatusType
        {
            Error_session_encryption = 10,
            Error_CBOR_decoding = 11,
            Session_termination = 20,
        }

    }

    public static class Retrieval
    {

        public static EventHandler<ItemsEventArgs> Handler;
        public static int RetrievalStatus = (int)Status.RetrievalStatusType.OK;
        public static int SessionStatus = (int)Status.SessionStatusType.Session_termination;


        public class ItemsEventArgs : EventArgs
        {
            public List<String> Items { get; set; }

        }

        private static bool ValidateReader(Holder holder)
        {
            bool t = false;
            holder.GenerateReaderAuthentication();
            try
            {
                t = holder.ValidateReader();
            }
            catch
            {
                Console.WriteLine("Failed to authenticate reader");
            }
            return t;
        }

        public static int CountRequests(CBORObject mdoc_request)
        {
            return mdoc_request.Count;

        }

        // Returns all of the types of the requested documents in order
        public static CBORObject GetDocTypes(Holder holder)
        {
            CBORObject typeArray = CBORObject.NewArray();

            for (int i = 0; i < CountRequests(holder.Request["docRequests"]); i++)
            {
                CBORObject docType = CBORObject.DecodeFromBytes(holder.Request["docRequests"][i]["itemsRequest"].GetByteString())["docType"];
                typeArray.Add(docType);
            }

            return typeArray;
        }

        public static void GenerateResponse(byte[] mdoc_bytes, Holder holder)
        {

            PrettyPrint print = new PrettyPrint();

            if(SessionStatus == (int)Status.SessionStatusType.Error_CBOR_decoding || SessionStatus == (int)Status.SessionStatusType.Error_session_encryption)
            {
                holder.Response = null;
            }

            else
            {
                CBORObject mdoc = ReadmDoc(mdoc_bytes);

                CBORObject response = CBORObject.NewMap().Add("version", 1.0);

                CBORObject issuerSignedArray = GenerateIssuerSigned(mdoc, holder);

                CBORObject typeArray = GetDocTypes(holder);

                CBORObject documents = CBORObject.NewArray();

                for (int i = 0; i < issuerSignedArray.Count; i++)
                {
                    holder.DocType = typeArray[i].AsString();

                    CBORObject issuerSigned = issuerSignedArray[i];

                    CBORObject deviceSigned = GenerateDeviceSigned(holder);

                    CBORObject document = CBORObject.NewMap()
                        .Add("docType", typeArray[i])
                        .Add("issuerSigned", issuerSigned)
                        .Add("deviceSigned", deviceSigned)
                        .Add("status", SessionStatus);

                    documents.Add(document);
                    Console.WriteLine("doctypass " + print._PrintCBOR(typeArray[i],0));
                    
                   

                }

                response.Add("documents", documents);


                
                holder.Response = response.EncodeToBytes();
            }
            
        }

        public static CBORObject ReadmDoc(byte[] mdocjson)
        {
            byte[] json_bytes = mdocjson;
            CBORObject objects = CBORObject.ReadJSON(new MemoryStream(json_bytes));

            return objects;
        }

        public static CBORObject[] GetRequestedItems(Holder holder)
        {

            PrettyPrint print = new PrettyPrint();
            int numOfDocs = holder.Request["docRequests"].Count;
            CBORObject[] itemsToReturn = new CBORObject[numOfDocs];

            int i;

            for (i = 0; i < numOfDocs; i++)
            {
                itemsToReturn[i] = CBORObject.DecodeFromBytes(holder.Request["docRequests"][i]["itemsRequest"].GetByteString());
                Console.WriteLine("items is this yah " + print._PrintCBOR(itemsToReturn[i],0));
            }

            return itemsToReturn;
        }

        public static List<String> ItemsRequested(Holder holder)
        {
            PrettyPrint print = new PrettyPrint();
            int numOfDocs = holder.Request["docRequests"].Count;
            List<String> itemsToReturn = new List<String>();
            

            int i;

            for (i = 0; i < numOfDocs; i++)
            {
                foreach (CBORObject element_entry in CBORObject.DecodeFromBytes(holder.Request["docRequests"][i]["itemsRequest"].GetByteString())["nameSpaces"].Values)
                {
                    foreach(CBORObject element in element_entry.Keys)
                    {
                        itemsToReturn.Add(element.AsString());
                        Console.WriteLine("element is " + element.AsString());
                    }
                }
                



            }
            

            return itemsToReturn;
        }

        private static void OnItemListCreated(List<String> items)
        {
            Handler.Invoke(null,new ItemsEventArgs { Items = items });
        }

        public static Tuple<CBORObject, CBORObject> GetItemsRequested(CBORObject mdoc_cbor, Holder holder)
        {
            
            CBORObject[] items = GetRequestedItems(holder);
          
            List<String> items_id = new List<String>();
            CBORObject arrayToReturn = CBORObject.NewArray();
            CBORObject items_ids = CBORObject.NewArray();

            CBORObject mdoc = mdoc_cbor["mdoc"];


            foreach (CBORObject item in items)
            {
                CBORObject itemsToReturn = CBORObject.NewMap();
                if (item["docType"].CompareTo(mdoc["docType"]) == 0)
                {
                    foreach (CBORObject key in item["nameSpaces"].Keys)
                    {

                        if (mdoc["nameSpaces"].ContainsKey(key) == true)
                        {
                            itemsToReturn.Add(key, CBORObject.NewMap());
                            foreach (KeyValuePair<CBORObject, CBORObject> item_req in item["nameSpaces"][key].Entries)
                            {
                                
                                itemsToReturn[key].Add(item_req.Key, Utils.EncodedItemToCBORBytes(mdoc["nameSpaces"][key][item_req.Key]));
                                items_id.Add(item_req.Key.AsString());
                               
                            }

                        }
                    }

                }
                arrayToReturn.Add(itemsToReturn);
            }
            OnItemListCreated(items_id);
            CBORObject arrayOfItemsRequested = arrayToReturn.WithTag(24);
            return new Tuple<CBORObject, CBORObject>(items_ids, arrayOfItemsRequested);
        }

       

        public static CBORObject GetIssuerAuth(CBORObject mdoc, Holder holder)
        {


            CBORObject issuerAuthArray = CBORObject.NewArray();

            for (int i = 0; i < CountRequests(holder.Request); i++)
            {
                CBORObject issuerAuth = CBORObject.DecodeFromBytes(Utils.EncodedItemToCBORBytes(mdoc["mso"]));
                issuerAuthArray.Add(issuerAuth);

            }

            return issuerAuthArray;
        }

        public static CBORObject GenerateIssuerSigned(CBORObject mdoc, Holder holder)
        {
            PrettyPrint print = new PrettyPrint();
            CBORObject issuerSignedArray = CBORObject.NewArray();


            Tuple<CBORObject, CBORObject> item_tuple = GetItemsRequested(mdoc, holder);
            CBORObject items_ids = item_tuple.Item1;
            CBORObject items = item_tuple.Item2;


            CBORObject issuerAuthArray = GetIssuerAuth(mdoc, holder);

            for (int i = 0; i < CountRequests(holder.Request["docRequests"]); i++)
            {

                CBORObject issuerSigned = CBORObject.NewMap();

                issuerSigned.Add("nameSpaces", items[i]);

                issuerSigned.Add("issuerAuth", issuerAuthArray[i]);

                issuerSignedArray.Add(issuerSigned);

            }


            return issuerSignedArray;
        }

        public static CBORObject GenerateDeviceSigned(Holder holder)
        {
            CBORObject deviceSigned = CBORObject.NewMap();
            deviceSigned.Add("nameSpaces", CBORObject.NewMap().WithTag(24).EncodeToBytes());
            holder.GenerateDeviceAuthentication();
            holder.SignDeviceAuthentication();
            deviceSigned.Add("deviceAuth",holder.DeviceAuth);

            return deviceSigned;
        }


        public static void ProcessData(Holder holder, byte[] sessionData)
        {

            CBORObject sessionData_cbor = CBORObject.NewMap();

            try
            {
                sessionData_cbor = CBORObject.DecodeFromBytes(sessionData);

                /*
             * If data contains key "eReaderKey" it means it's a session establishment message 
             * Otherwise it's a session data message and it skips this if
             */
                if (sessionData_cbor.ContainsKey("eReaderKey"))
                {

                    // Get Reader public key from session establishment message
                    try
                    {

                        holder.ReaderPubkeyBytes = sessionData_cbor["eReaderKey"].GetByteString();

                    }
                    catch
                    {
                        Console.WriteLine("Failed to get reader's public key");
                        RetrievalStatus = (int)Status.RetrievalStatusType.CBOR_validation_error;
                    }

                    // Generate device's session key to decrypt and encrypt session data

                    holder.GenerateSessionKey();


                }

                /*
                 * mdoc request needs to be decrypted before being decoded from bytes to CBOR Object
                 */
                byte[] encrypted_request = sessionData_cbor["data"].GetByteString();

                /*
                 * Decrypts mDoc request and stores it as a cbor object inside holder instance
                 */

                try
                {

                    holder.DecryptData(encrypted_request);

                }
                catch
                {
                    Console.WriteLine("Failed to decrypt request");
                    RetrievalStatus = (int)Status.RetrievalStatusType.CBOR_validation_error;
                    
                }

                try
                {

                    bool t = ValidateReader(holder);
                    Console.WriteLine(t);
                    if (t == false) throw new Exception("Reader authentication failed");

                }
                catch
                {
                    Console.WriteLine("Failed to authenticate Reader!");
                    RetrievalStatus = (int)Status.RetrievalStatusType.CBOR_validation_error;
                   
                };

                if (RetrievalStatus != (int)Status.RetrievalStatusType.OK)
                {
                    SessionStatus = (int)Status.SessionStatusType.Error_CBOR_decoding;
                }

                Console.WriteLine("Decrypted data!");
            }
            catch (Exception e)
            {
                RetrievalStatus = (int)Status.RetrievalStatusType.CBOR_decoding_error;
                SessionStatus = (int)Status.SessionStatusType.Error_CBOR_decoding;
            }
            

            

        }
    }

}
