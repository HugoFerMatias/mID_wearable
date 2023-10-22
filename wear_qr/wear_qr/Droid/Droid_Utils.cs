using Android.OS;
using Java.Lang;
using Java.Nio;
using Java.Util;
using System;
using utils;

namespace droid
{
   public static class DroidUtils
   {
       
        // Converts UUID to byte array
        public static byte[] UUIDToByteArray(UUID uuid)
        {
            long mostSign = uuid.MostSignificantBits;
            long leastSign = uuid.LeastSignificantBits;
            Utils.Uuid aux_uuid = new Utils.Uuid(mostSign, leastSign);

            return aux_uuid.ToByteArray();
        }

        public static Utils.Uuid UUIDToUuid(UUID uuid)
        {
            long mostSign = uuid.MostSignificantBits;
            long leastSign = uuid.LeastSignificantBits;
            Utils.Uuid aux_uuid = new Utils.Uuid(mostSign, leastSign);

            return aux_uuid;
        }

        public static ParcelUuid ParseUUID(Guid uuid)
        {
            int serviceUuid = 0xFEAA;
            byte[] serviceUuidBytes = new byte[] {
                            (byte) (serviceUuid & 0xff),
                            (byte) ((serviceUuid >> 8) & 0xff)};
            return ParseUuidFrom(uuid.ToByteArray());
        }

        private static ParcelUuid ParseUuidFrom(byte[] uuidBytes)
        {

            /** Length of bytes for 16 bit UUID */
            const int UUID_BYTES_16_BIT = 2;
            /** Length of bytes for 32 bit UUID */
            const int UUID_BYTES_32_BIT = 4;
            /** Length of bytes for 128 bit UUID */
            const int UUID_BYTES_128_BIT = 16;

            ParcelUuid BASE_UUID =
                    ParcelUuid.FromString("00000000-0000-1000-8000-00805F9B34FB");
            if (uuidBytes == null)
            {
                throw new IllegalArgumentException("uuidBytes cannot be null");
            }
            int length = uuidBytes.Length;
            if (length != UUID_BYTES_16_BIT && length != UUID_BYTES_32_BIT &&
                    length != UUID_BYTES_128_BIT)
            {
                throw new IllegalArgumentException("uuidBytes length invalid - " + length);
            }
            // Construct a 128 bit UUID.
            long msb;
            long lsb;
            if (length == UUID_BYTES_128_BIT)
            {
                ByteBuffer buf = ByteBuffer.Wrap(uuidBytes).Order(ByteOrder.BigEndian);
                msb = buf.GetLong(0);
                lsb = buf.GetLong(8);
                return new ParcelUuid(new UUID(msb, lsb));
            }
            // For 16 bit and 32 bit UUID we need to convert them to 128 bit value.
            // 128_bit_value = uuid * 2^96 + BASE_UUID
            long shortUuid;
            if (length == UUID_BYTES_16_BIT)
            {
                shortUuid = uuidBytes[0] & 0xFF;
                shortUuid += (uuidBytes[1] & 0xFF) << 8;
            }
            else
            {
                shortUuid = uuidBytes[0] & 0xFF;
                shortUuid += (uuidBytes[1] & 0xFF) << 8;
                shortUuid += (uuidBytes[2] & 0xFF) << 16;
                shortUuid += (uuidBytes[3] & 0xFF) << 24;

            }

            msb = BASE_UUID.Uuid.MostSignificantBits + (shortUuid << 32);
            lsb = BASE_UUID.Uuid.LeastSignificantBits;
            return new ParcelUuid(new UUID(msb, lsb));
        }



    }
}