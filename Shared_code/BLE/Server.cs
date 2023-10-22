using System;

namespace ble
{

    public class Characteristic
    {
        public string UUID;
        public Accessibility accessibility;

        public string GetUUID { get { return UUID; } }
    }

    public class State : Characteristic
    {
        private enum State_Values { Start, End }

        private static State_Values value;

        public State()
        {
            UUID = "00000001-A123-48CE-896B-4C76973373E6";
            accessibility = new Accessibility(true, true, true, true, true);
        }


        public string Value
        {
            get
            {
                if (value == State_Values.Start) return "Start";
                else if (value == State_Values.End) return "End";
                else return null;
            }

            set { value = value.ToString(); }

        }

        public void ChangeState(string new_state)
        {
            if (new_state == "Start")
            {
                value = State_Values.Start;
            }

            if (new_state == "End")
            {
                value = State_Values.End;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }


    }

    public class Server2Client : Characteristic
    {
        public Accessibility accessibility = new Accessibility(true, true, true, false, true);


        public Server2Client()
        {
            UUID = "00000003-A123-48CE-896B-4C76973373E6";
            accessibility = new Accessibility(true, true, true, false, true);
        }
    }

    public class Client2Server : Characteristic
    {
        public Accessibility accessibility = new Accessibility(true, true, false, true, true);


        public Client2Server()
        {
            UUID = "00000002-A123-48CE-896B-4C76973373E6";
            accessibility= new Accessibility(true, true, false, true, true);
        }

    }

    public class L2CAP
    {
        public const string UUID = "0000000A-A123-48CE-896B-4C76973373E6";
    }

    public class Accessibility
    {

        Permissions permissions = new Permissions();
        Properties properties = new Properties();

        public Accessibility(bool read, bool write, bool notify, bool writeWithoutResponse, bool writeNoResponsee)
        {
            permissions.Read = read;
            permissions.Write = write;
            properties.Notify = notify;
            properties.WriteWithoutResponse = writeWithoutResponse;
            properties.WriteNoResponse = writeNoResponsee;

        }

        private class Permissions
        {
            public bool read;
            public bool write;

            public bool Read
            {
                get { return Read; }
                set { read = value; }
            }
            public bool Write
            {
                get { return Write; }
                set { write = value; }
            }
            //public bool readEncryptionRequired;
            //public bool writeEncryptionRequired;

        }

        private class Properties
        {
            public bool notify;
            public bool writeWithoutResponse;
            public bool writeNoResponse;

            public bool Notify
            {
                get { return notify; }
                set { notify = value; }
            }

            public bool WriteWithoutResponse
            {
                get { return writeWithoutResponse; }
                set { writeWithoutResponse = value; }
            }

            public bool WriteNoResponse
            {
                get { return writeNoResponse; }
                set { writeNoResponse = value; }
            }

        }

    }
}