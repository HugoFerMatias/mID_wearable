using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using droid;
using Java.Util;
using System;



namespace ble
{

    public delegate AdvertisingSetCallback callbackDelegate();

    public class Advertiser
    {
        public static BluetoothLeAdvertiser advertiser;
        //public static Context context;
        public static BluetoothManager bluetoothManager;
        public static UUID service_UUID;
       
        public static AdvertiseData data;
        public static AdvertisingSetCallback callback;
        
        
        public UUID Service_UUID
        {
            set { service_UUID = value; }
        }

        public Advertiser(UUID service_UUID, BluetoothManager ble_manager)
        {
           //context = Application.Context;
           bluetoothManager = ble_manager;
           Service_UUID = service_UUID;
        }

        public void StartAdvertising(int psm)
        {
            advertiser = bluetoothManager.Adapter.BluetoothLeAdvertiser;
            Guid service_uuid = new Guid(service_UUID.ToString());


            AdvertisingSetParameters parameters = (new AdvertisingSetParameters.Builder())
           .SetLegacyMode(true) 
           .SetScannable(true)
           .SetInterval(AdvertisingSetParameters.IntervalLow)
           .SetTxPowerLevel(AdvertiseTxPower.High)
           .Build();
 

            data = new AdvertiseData.Builder()
            .AddServiceData(DroidUtils.ParseUUID(service_uuid),BitConverter.GetBytes(psm))
            .Build();
            advertiser.StartAdvertisingSet(parameters, data, data, null, null, new AdvertisingCallbackCustom());
        }

        public class AdvertisingCallbackCustom : AdvertisingSetCallback
        {
           
            public override void OnAdvertisingDataSet(AdvertisingSet advertisingSet, [GeneratedEnum] AdvertiseResult status)
            {
                Console.WriteLine("Advertising Data Set Created");
                base.OnAdvertisingDataSet(advertisingSet, status);
            }

            public override void OnAdvertisingSetStarted(AdvertisingSet advertisingSet, int txPower, [GeneratedEnum] AdvertiseResult status)
            {
                Console.WriteLine("Advertising Started");
                base.OnAdvertisingSetStarted(advertisingSet, txPower, status);
            }

            public override void OnScanResponseDataSet(AdvertisingSet advertisingSet, [GeneratedEnum] AdvertiseResult status)
            {
                Console.WriteLine("Scan Response Given");
                base.OnScanResponseDataSet(advertisingSet, status);
            }



        }

        public class Advertising_Callback : AdvertisingSetCallback
        {
            public Advertising_Callback(AdvertisingSet? advertisingSet, int txPower, int status)
            {
                Console.WriteLine("The advertising set: with " + txPower.ToString()
                                 + " txPower with " + status + " status. " + "set id is ");
            }
        }

      

        

    }
}