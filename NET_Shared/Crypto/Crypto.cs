using System;
using System.Text;

using PeterO.Cbor;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

using utils;
using cose;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;

namespace crypto
{

    public class Hkdf
    {
        public Func<byte[], byte[], byte[]> keyedHash;

        public Hkdf()
        {
            var hmac = new HMACSHA256();
            keyedHash = (key, message) =>
            {
                hmac.Key = key;
                return hmac.ComputeHash(message);
            };
        }

        public byte[] Extract(byte[] salt, byte[] inputKeyMaterial)
        {
            return keyedHash(salt, inputKeyMaterial);
        }

        public byte[] Expand(byte[] prk, byte[] info, int outputLength)
        {
            var resultBlock = new byte[0];
            var result = new byte[outputLength];
            var bytesRemaining = outputLength;
            for (int i = 1; bytesRemaining > 0; i++)
            {
                var currentInfo = new byte[resultBlock.Length + info.Length + 1];
                Array.Copy(resultBlock, 0, currentInfo, 0, resultBlock.Length);
                Array.Copy(info, 0, currentInfo, resultBlock.Length, info.Length);
                currentInfo[currentInfo.Length - 1] = (byte)i;
                resultBlock = keyedHash(prk, currentInfo);
                Array.Copy(resultBlock, 0, result, outputLength - bytesRemaining, Math.Min(resultBlock.Length, bytesRemaining));
                bytesRemaining -= resultBlock.Length;
            }
            return result;
        }

        public byte[] DeriveKey(byte[] salt, byte[] inputKeyMaterial, byte[] info, int outputLength)
        {
            var prk = Extract(salt, inputKeyMaterial);
            var result = Expand(prk, info, outputLength);
            return result;
        }
    }

    public class AesGcm256
    {

        private AesGcm256() { }

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            byte[] encrypted_data;
            try
            {

                GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());

                KeyParameter keyy = new KeyParameter(key);

                AeadParameters parameters =
                             new AeadParameters(new KeyParameter(key), 128, iv, null);

                cipher.Init(true, parameters);

                encrypted_data = new byte[cipher.GetOutputSize(data.Length)];

                int retLen = cipher.ProcessBytes
                               (data, 0, data.Length, encrypted_data, 0);

                cipher.DoFinal(encrypted_data, retLen);

                return encrypted_data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return null;

        }

        public static byte[] Decrypt(byte[] encrypted_data, byte[] key, byte[] iv)
        {
            byte[] decrypted_data = null;
            try
            {

                GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
                AeadParameters parameters =
                          new AeadParameters(new KeyParameter(key), 128, iv, null);


                cipher.Init(false, parameters);

                decrypted_data = new byte[cipher.GetOutputSize(encrypted_data.Length)];

                Int32 retLen = cipher.ProcessBytes
                               (encrypted_data, 0, encrypted_data.Length, decrypted_data, 0);

                cipher.DoFinal(decrypted_data, retLen);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return decrypted_data;
        }
    }

    public static class Crypto
    {


        // Used to perform ECDH key agreement with the return of the shared secret
        public static byte[] KeyAgreementSecret(byte[] readerPubKey,byte[] devicePrivKey)
        {
           
            CBORObject reader_key = Utils.BytesToCBOR(readerPubKey); 
            CBORObject device_priv_key = Utils.BytesToCBOR(devicePrivKey);

            AsymmetricKeyParameter ReaderPubKey = (new Key(reader_key)).AsPublicKey();
            AsymmetricKeyParameter DevicePrivKey = (new Key(device_priv_key)).AsPrivateKey();
         
            IBasicAgreement agreement = AgreementUtilities.GetBasicAgreement("ECDH");
            agreement.Init(DevicePrivKey);
            // Note: Use reader public key
            BigInteger shared_secret = agreement.CalculateAgreement(ReaderPubKey);

            byte[] sharedSecret = shared_secret.ToByteArrayUnsigned();

            return sharedSecret;

        }


        // Used to derive holder's SKDevice key for connection establishment
        public static Tuple<byte[], byte[]> DeriveDeviceKey(byte[] readerPubKey, byte[] devicePrivKey, byte[] holderSessionTranscriptBytes)
        {

            Hkdf HKDFS_D = new Hkdf();
            Hkdf HKDFS_R = new Hkdf();

            byte[] sharedSecret = KeyAgreementSecret(readerPubKey,devicePrivKey);
 
            byte[] bytesD = Encoding.Default.GetBytes("SKDevice");
            byte[] bytesR = Encoding.Default.GetBytes("SKReader");

            string deviceInfo = Encoding.UTF8.GetString(bytesD);
            string readerInfo = Encoding.UTF8.GetString(bytesR);

            byte[] skDeviceKey = HKDFS_D.DeriveKey(holderSessionTranscriptBytes, sharedSecret, Encoding.UTF8.GetBytes(deviceInfo), 32);
            byte[] skReaderKey = HKDFS_R.DeriveKey(holderSessionTranscriptBytes, sharedSecret, Encoding.UTF8.GetBytes(readerInfo), 32);

            Tuple<byte[], byte[]> pair = new Tuple<byte[], byte[]>(skDeviceKey,skReaderKey);
            return pair;
        }

        public static byte[] EncryptSessionData(byte[] SessionData, byte[] skDeviceKey, byte[] identifier, uint message_counter)
        {
            byte[] iv = CreateIV(identifier, message_counter);
            
            byte[] encrypted_data = AesGcm256.Encrypt(SessionData, skDeviceKey, iv);

            return encrypted_data;

        }
        public static byte[] DecryptSessionData(byte[] data, byte[] identifier, uint message_counter, byte[] skDeviceKey)
        {

            byte[] iv = CreateIV(identifier, message_counter);
            byte[] decrypted_data = AesGcm256.Decrypt(data, skDeviceKey, iv);

            return decrypted_data;
        }

        // Criar IV de acordo com a ISO
        private static byte[] CreateIV(byte[] identifier, uint message_counter)
        {
            byte[] mc_bytes = Utils.UIntToBytes(message_counter);
            byte[] iv = Utils.Combine(identifier, mc_bytes);

            return iv;

        }

        

    }

    
}