namespace device_retrieval_response
{
    public class Document
    {
        private string docType;
        private IssuerSigned issuer = new IssuerSigned();
        private DeviceSigned device = new DeviceSigned();
        //private int error = new Errors();
    }
}