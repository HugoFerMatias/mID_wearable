using System;

namespace device_engagement
{
    [Serializable]
    abstract class ServerRetrievalMethod
    {
        public class WebApi
        {
            uint version;
            string issuer_url;
            string server_retrieval_token;
        }

        public class Oidc
        {
            uint version;
            string issuer_url;
            string server_retrieval_token;
        }
    }
}