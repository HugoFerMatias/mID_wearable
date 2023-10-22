namespace device_retrieval_response
{
    public class Errors
    {
        public int statusCode;
    }
    
    class DataNotReturned : Errors
    {
        public DataNotReturned() : base()
        {
            base.statusCode = 0;
        }
    }

    class RFU_Error : Errors
    {
        public RFU_Error() : base()
        {
            base.statusCode = 1;
        }
    }
    class App_Error : Errors
    {
        public App_Error() : base()
        {
            base.statusCode = -1;
        }
    }
}