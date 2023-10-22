namespace device_retrieval_response
{
    public class DocumentError
    {
        private int errorCode;

        public int ErrorCode
        {
            get => errorCode;
            set => errorCode = value >= -31 && value <= 7
                ? value
                : throw new System.Exception("Error code value is out of bounds.");
        }
    }
}