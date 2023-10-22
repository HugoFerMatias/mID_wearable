using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Graphics;
using Microsoft.IdentityModel.Tokens;
using utils;
using qrcode;
using PeterO.Cbor;

using ble;

using droid;
using retrieval;
using System.Collections.Generic;


namespace wear_qr
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : WearableActivity
    {
       
        private Droid_Holder droid;
        private PrettyPrint print = new PrettyPrint();
       
        private void Initialize()
        {


            // Using elliptic curve p-256 for cryptographic algorithms
            droid = new Droid_Holder("P-256");
       
            // Device engagement CBOR map structure already encoded in CBOR bytes
            byte[] engagement_data = droid.DeviceEngagementBytes;
     
            // Encode to base64 Url
            string engagement_data_string = Base64UrlEncoder.Encode(engagement_data);

            Console.WriteLine("Device engagement structure: \n" + print._PrintCBOR(droid.DeviceEngagement.Eng_Map, 0));

            Console.WriteLine("mdoc:" + engagement_data_string);

            GenQRCode(engagement_data_string);
                     
            InitBLE();

        }

        private void InitBLE()
        {
            droid.InitializePeripheral();
            BLEServerStart();
        }

        private void GenQRCode(string data)
        {
            SetContentView(Resource.Layout.QR_Gen);

            byte[] imageBytes = QRCode.Generate(data);

            // Genrate QRcode with device engagement structure
            Bitmap finalImage = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);

            ImageView imageView = (ImageView)FindViewById(Resource.Id.imageView1);

            imageView.SetImageBitmap(finalImage);
        }
        
         

        private void ProcessRequest(object sender, DataInputEventArgs args)
        {
            Retrieval.ProcessData(droid,args.Data);
            Retrieval.GenerateResponse(Properties.Resource1.test, droid);   
        }

        private void SendData(object sender, EventArgs args)
        {

            
            droid.SendResponse();
            Console.WriteLine("Sent Response!");
               
        }

        private void WaitRequests(object sender, EventArgs args)
        {
            RunOnUiThread(() =>
            {
                Peripheral.PeripheralHandler += ProcessRequest;
                SetContentView(Resource.Layout.Loading);
                Button cancelButton = FindViewById<Button>(Resource.Id.CancelButton);
                cancelButton.Click += (e, o) => CloseConnection(e,o);
            });
        }

        public void CloseConnection(object sender, EventArgs args)
        {
            droid.CloseConnection();
            Initialize();
        }




        private void ShowItemList(object sender, Retrieval.ItemsEventArgs args)
        {
            List<String> items = args.Items;
            List<String> itemsFormatted = Utils.ReplaceUnderscore(items);
            
            ListView lView;
            
            RunOnUiThread(() =>
            {
                SetContentView(Resource.Layout.ItemList);
                
                lView = FindViewById<ListView>(Resource.Id.listItems);
                ArrayAdapter<String> adapter;
                adapter
                    = new ArrayAdapter<String>(
                        this,Resource.Layout.customListLayout,
                        Resource.Id.textItem,
                        itemsFormatted);
                lView.Adapter = adapter;

                Button sendButton = FindViewById<Button>(Resource.Id.enviar);
                sendButton.Click += SendData;

            });


            
        }

        private void BLEServerStart()
        {
            Peripheral.WaitRequestHandler += WaitRequests;
            
            Retrieval.Handler += ShowItemList; 

        }

     
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetAmbientEnabled();
            Initialize();
        }
    }


}


