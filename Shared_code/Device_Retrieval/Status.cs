namespace device_retrieval_response
{
    public class Status
    {

        public int code;
              
    }

    public class OK : Status
    {
        public OK() : base()
        {
            base.code = 0;
        }
    }

    public class General_Error : Status
    {
        public General_Error() : base()
        {
            base.code = 10;
        }
    }
    public class CBOR_Decoding_Error : Status
    {
        public CBOR_Decoding_Error() : base()
        {
            base.code = 11;
        }
    }
    public class CBOR_Validation_Error : Status
    {
        public CBOR_Validation_Error() : base()
        {
            base.code = 12;
        }
    }
}