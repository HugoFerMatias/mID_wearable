using device_engagement;

namespace device_retrieval_response
{

    // Session transcript encontra-se em ReaderAuth enviado pelo reader na mensagem de device request, deverá ser retirado desta estrutura e 
    // os seus valores armazenados nesta classe
    public class SessionTranscripts
    {
        private byte[] deviceEngagement;
        public byte[] EReaderKeyBytes;
        private string handover = null;
        // EDeviceReader

        public byte[] ReaderPubKey
        {
            get { return EReaderKeyBytes; }
            set { EReaderKeyBytes = value; }
        }

        public byte[] DeviceEngagement
        {
            get { return deviceEngagement; }
            set { deviceEngagement = value; }
        }

    }
}