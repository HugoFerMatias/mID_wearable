using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace device_retrieval_response
{
    public class Device_Retrieval_Response
    {
        string version = "1.0";
        //List<Document> documents = new List<Document>();
        List<string> documents = new List<string>();
        List<DocumentError> errors = new List<DocumentError>();
        uint status;

        public Device_Retrieval_Response()
        {

        }

        public void LoadJson()
        {
            using (StreamReader r = new StreamReader("file.json"))
            {
                string json = r.ReadToEnd();
                //List<Item> items = JsonConvert.DeserializeObject<List<Item>>(json);
            }
        }

        /*private void AddDocument(Document document)
        {
            documents.Add(document);
        }*/

        public void AddDocument(string document)
        {
            documents.Add(document);
        }
    }

    
}