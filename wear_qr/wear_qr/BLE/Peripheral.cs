using Android.App;
using Android.Bluetooth;
using Android.Content;


using System.Timers;
using Java.Lang;
using Java.Util;
using PeterO.Cbor;
using System;
using System.Diagnostics;
using System.IO;
using utils;

namespace ble
{
    public class DataInputEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
       
    }

    public class Peripheral 
    {

        public static event EventHandler<DataInputEventArgs> PeripheralHandler;
        public static event EventHandler<EventArgs> WaitRequestHandler;


        // Method responsible for notifying subscribers



        // Para propositos de teste, esta chave deve ser enviada pela estrutura device engagement e não pela conexão l2cap
        public static byte[] eDeviceKey; 

        // Custom Gatt Server callback class
        public BLE_Server_Callback callback = new BLE_Server_Callback();

        public static Advertiser advertiser;
        public static BluetoothAdapter adapter;
        private readonly static Context context = Application.Context;
        public static BluetoothManager bluetoothManager = (BluetoothManager)context.GetSystemService(Context.BluetoothService);

        // Peripheral server mode UUID
        public static UUID peripheral_server_uuid;

        // L2cap ServerSocket
        private static BluetoothServerSocket L2CAPServerSocket;
        // Thread to listen to incoming l2cap socket connections
        private static AcceptThread acceptThread;
        // Thread to manage l2cap connection
        private static ConnectedThread connectedThread;

        public static BluetoothGattCharacteristic L2CAP =
            new BluetoothGattCharacteristic(UUID.FromString("0000000A-A123-48CE-896B-4C76973373E6"), GattProperty.Read, GattPermission.Read);

        /*
         * This service corresponds to the peripheral server mode UUID sent in the Device Engagement structure
         * This service is created when creating an instance of BLE_Peripheral Object
         */
        public static BluetoothGattService BluetoothService;

        public Peripheral()
        {
            peripheral_server_uuid = UUID.RandomUUID();
            BluetoothService = new BluetoothGattService(peripheral_server_uuid, GattServiceType.Primary);
            adapter = bluetoothManager.Adapter;
        }

        private static void OnConnectionAccepted(object sender, EventArgs args)
        {
            Console.WriteLine("Connection accepted");
            WaitRequestHandler.Invoke(sender, args);
        }

        private static void OnDataReceived(object sender, DataInputEventArgs args)
        {
            Console.WriteLine("Peripheral stored data " + args.Data.Length);
            PeripheralHandler.Invoke(sender, args);
        }

        public static void OnWaitRequest(object sender, EventArgs args)
        {
            Console.WriteLine("Waiting for request...");
            WaitRequestHandler.Invoke(sender, args);
        }

        public bool IsOpened { get; private set; }

     
        public static BluetoothGattServer gattServer;

        private static Peripheral _instance;

        public static Peripheral Instance
        {
            get { return _instance ?? (_instance = new Peripheral()); }
        }


        public UUID Peripheral_Server_UUID
        {
            get { return peripheral_server_uuid; }
        }

       

        public void Open()
        {

            // Open Gatt Server
            gattServer = bluetoothManager.OpenGattServer(context, callback);
          
            if (gattServer == null)
            {
                Debug.WriteLine("Couldn't open Gatt Server!");
                return;
            }
            IsOpened = true;

            // Adding Gatt characteristics to service
            BluetoothService.AddCharacteristic(L2CAP);
            
            // Adding service to Gatt server
            gattServer.AddService(BluetoothService);

            Console.WriteLine("Service is " + gattServer.Services.Count.ToString());

            advertiser = new Advertiser(BluetoothService.Uuid, bluetoothManager);

            StartL2CAP();
        }

        public void Close()
        {
            if(acceptThread != null)
            {
                acceptThread.Cancel();
            }

            if(connectedThread != null)
            {
                connectedThread.Cancel();
            }

            if (gattServer != null && IsOpened)
            {
                IsOpened = false;
                gattServer.Close();
            }
        }

        public void StartL2CAP()
        {
            try
            {
                acceptThread = new AcceptThread();
                acceptThread.Handler += OnConnectionAccepted;
                
                Console.WriteLine("Starting AcceptThread...");
                acceptThread.Start();
            }
            catch (IOException ex) { Console.WriteLine("Failed to start l2cap thread");}
           
        }
        
        public void Write(byte[] output)
        {
          
            connectedThread.Write(output);
            
        }

        private static void Connected(BluetoothSocket socket)
        {
            // Start the thread to manage the connection and perform transmissions
            connectedThread = new ConnectedThread(socket,eDeviceKey);
            // Subscribe to connectedThread
            /*
             * When connectedThread receives data it will trigger OnDataReceived method in BLE_Peripheral class
             * thanks to the line of code below which subscribes BLE_Peripheral to the ConnectedThread.
             */
            connectedThread.Handler += OnDataReceived;
            connectedThread.WaitHandler += OnWaitRequest;
            connectedThread.Start();
       
        }

        public class AcceptThread: Thread
        {

            public event EventHandler<EventArgs> Handler;

            private void OnConnectionAccepted()
            {
                Handler.Invoke(this, new EventArgs());
            }

            public AcceptThread()
            {
                // Use a temporary object that is later assigned to L2CAPServerSocket,    
                // because L2CAPServerSocket is final    
                BluetoothServerSocket tmp = null;
                try
                {
                    Console.WriteLine("Creating l2cap server socket...");
                    tmp = adapter.ListenUsingInsecureL2capChannel();

                   

                    // Set PSM as l2cap's characteristic value
                    L2CAP.SetValue(BitConverter.GetBytes(tmp.Psm));
                    Console.WriteLine("Service issa " + gattServer.Services.Count.ToString());
                    
                    advertiser.StartAdvertising(tmp.Psm);

                    PrintByteArray(L2CAP.GetValue());
                    Console.WriteLine("Successfully created l2cap server socket with PSM " + tmp.Psm);
                }
                catch (IOException e) 
                {
                    Console.WriteLine("Failed to listen");
                }
                L2CAPServerSocket = tmp;
            }

            override
            public void Run()
            {
                BluetoothSocket socket = null;
                // Keep listening until exception occurs or a socket is returned    
                while (true)
                {
                    try
                    {
                        Console.WriteLine("Waiting for remote device connection...");
                        socket = L2CAPServerSocket.Accept();
                    }
                    catch (IOException e)
                    {
                        break;
                    }
                    // If a connection was accepted    
                    if (socket != null)
                    {
                        Console.WriteLine("Accepted socket from remote device!");
                        // Manage connection in different thread
                        OnConnectionAccepted();
                        Connected(socket);
                        L2CAPServerSocket.Close();
                        break;
                    }
                }
            }

            public void Cancel()
            {
                try
                {
                    L2CAPServerSocket.Close();
                }
                catch (IOException e) { }
            }

          
        }


        public static void PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            Console.WriteLine(sb.ToString());
        }

        public class ConnectedThread : Thread
        {
            private readonly BluetoothSocket Socket;
            private readonly Stream InputStream;
            private readonly Stream OutStream;
            PrettyPrint print = new PrettyPrint();
            System.Timers.Timer timerSocket = new System.Timers.Timer();
            
            public event EventHandler<DataInputEventArgs> Handler;
            public event EventHandler<EventArgs> WaitHandler;

            // Method responsible for notifying subscribers
            private void OnDataReceived(byte[] data)
            {
                Handler.Invoke(this, new DataInputEventArgs { Data = data });
            }

            private void OnWaitRequest()
            {
                WaitHandler.Invoke(this, null);
            }

            public ConnectedThread(BluetoothSocket socket,byte[] deviceKey)
            {
            
            Socket = socket;
           
            Stream tmpIn = null;
            Stream tmpOut = null;
                // Get the BluetoothSocket input and output streams
                try
            {
                tmpIn = socket.InputStream;
                tmpOut = socket.OutputStream;
            }
            catch (IOException e)
            {
               
            }
            InputStream = tmpIn;
            OutStream = tmpOut;
            }

        override
        public void Run()
        {
            Console.WriteLine("Starting ConnectedThread...");
            int bytes;
            int byte_acc = 0;
            byte[] buffer = new byte[1024];
            byte[] aux;
            timerSocket.Elapsed += Cancel;
            timerSocket.Interval = 300000;

                // ISTO É PARA TESTES
           
            OnWaitRequest();

               
                // Keep listening to the InputStream while connected
                while (true)
                {
                   
                    timerSocket.Enabled = true;
   
                    try
                    {
                        
                        Console.WriteLine("Reading from input stream...");
                        bytes = InputStream.Read(buffer);
                        aux = new byte[bytes];
                        if (byte_acc >= buffer.Length)
                            {
                                buffer = new byte[1024];
                            }

                        Buffer.BlockCopy(buffer, 0, aux, byte_acc, bytes);
                        byte_acc = byte_acc + bytes;
                        if (bytes > 0)
                        {
                            OnDataReceived(aux);
                            timerSocket.Interval = 300000;
                            Console.WriteLine("Read was successfull " + bytes.ToString());
                        }
                            
                        
                    }
                    catch
                    {
                        Console.WriteLine("Couldnt read");
                    }    
                }
        }
        /**
         * Write to the connected OutStream.
         * @param buffer  The bytes to write
         */
        public void Write(byte[] buffer)
        {
            try
            {
                CBORObject buffer_obj = CBORObject.DecodeFromBytes(buffer);
                    if (buffer_obj.ContainsKey(CBORObject.FromObject("data")))
                    {
                        Console.WriteLine("structure response is: " + print._PrintCBOR(buffer_obj,0));
                    }

                    Console.WriteLine("Writing to Output Stream...");
                OutStream.Write(buffer);
                OnWaitRequest();

            }
            catch (IOException e)
            {
                Console.WriteLine("Exception during write");
            }
        }
        public void Cancel(object source = null, ElapsedEventArgs args = null)
        {
            try
            {

                Console.WriteLine("Socket closed!!!");
                Socket.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine("Close() of connect socket failed");
            }
        }
    }

        // GattServer custom callback class
        public class BLE_Server_Callback : BluetoothGattServerCallback
        {

            
            public override void OnServiceAdded( GattStatus status, BluetoothGattService service)
            {
                Console.WriteLine("Service was added");
                Console.WriteLine("This is the service uuid: " + BluetoothService.Uuid.ToString());
                Console.WriteLine("Service is " + gattServer.Services.Count.ToString());
                base.OnServiceAdded(status, service);
            }


            
            public override void OnCharacteristicReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattCharacteristic characteristic)
            { 
                base.OnCharacteristicReadRequest(device, requestId, offset, characteristic);

                if(gattServer == null)
                {
                    return;
                }

                Console.WriteLine("Device tried to read characteristic with UUID: " + characteristic.Uuid.ToString());
                Console.WriteLine("Characteristic Value is " + BitConverter.ToString(characteristic.GetValue()));

                gattServer.SendResponse(device,requestId,GattStatus.Success,offset,characteristic.GetValue());
            }

            
            public override void OnConnectionStateChange(BluetoothDevice device, ProfileState status , ProfileState newStatus)
            {
                Console.WriteLine("State of connection has changed with device " + device.Address);
                base.OnConnectionStateChange(device, status, newStatus);
            }
            
        }
    }    

}