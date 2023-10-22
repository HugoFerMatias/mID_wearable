using System;
using System.Collections.Generic;
using PeterO.Cbor;

using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

using crypto;
using device_engagement;
using device_retrieval_response;
using System.Text;
using utils;

namespace holder
{
    public class Holder
    {

        
        // "Custom" key with values from both public and private key
        // *Note: This key is only used to retrieve public and private keys, so it might not be necessary
        // as there are methods that can return the public and private keys separately
        private static Key EDeviceKey;

        // Public key structure to send to Reader
        public CBORObject Map_PubKey;
        private CBORObject Map_PrivKey;

        // Shared secret of key agreement between holder and reader
        private static byte[] sharedSecret;
        // Signature os holder
        private static byte[] Signature;

        // Keys of holder in their "pure" key form
        private AsymmetricKeyParameter PrivKey;
        public AsymmetricKeyParameter PubKey;

        // Keys of holder in their "custom" form
        private Key OneKeyPriv;
        public Key OneKeyPub;

        //public AsymmetricCipherKeyPair AuthPubKey;
        //public AsymmetricCipherKeyPair AuthPrivKey;
        
        // Holder's Session key
        private byte[] SKDeviceKey;

        private SessionTranscripts transcripts;
        private byte[] SessionTranscriptBytes;

        private SessionTranscripts HolderSessionTranscript = new SessionTranscripts();
        private static byte[] HolderSessionTranscriptBytes;

        // Reader's public key
        public AsymmetricKeyParameter ReaderPubKey;

        // Used to print CBOR maps in order to check key components
        private PrettyPrint print = new PrettyPrint();

        // CBOR encoded COSE public key
        public static byte[] EDeviceKeyBytes;
        // Holder's Device Engagement structure
        private static Device_Engagement device_Engagement;
        private static byte[] device_engagement_bytes;

        private void Device_Eng_to_Bytes(Device_Engagement device_eng)
        {
            CBORObject data_cbor_obj = CBORObject.FromObject(device_eng);
            device_engagement_bytes = data_cbor_obj.EncodeToBytes();
        }



        public byte[] Secret
        {
            get { return sharedSecret; }
            set { sharedSecret = value; }
        }

        public byte[] PublicKeyBytes
        {
            get { return EDeviceKeyBytes; }
        }

        public Device_Engagement DeviceEngagement
        {
            get { return device_Engagement; }
            set { device_Engagement = value; }
        }

        public byte[] DeviceEngagementBytes
        {
            get { return device_engagement_bytes; }
            set { device_engagement_bytes = value; }
        }

        public CBORObject PubMAP
        {
            get { return Map_PubKey; }
        }

        // Method that takes the reader's public key from the SessionTranscripts class
        public void GetReaderPubKey()
        {
            byte[] keyBytes = transcripts.ReaderPubKey;
            CBORObject keyCBOR = CBORObject.DecodeFromBytes(keyBytes);
            Key temp = new Key(keyCBOR);
            ReaderPubKey = temp.AsPublicKey();

        }

        private static void EncodeCBORSessionTrans(SessionTranscripts sessionT)
        {
            CBORObject key_cbor_obj = CBORObject.FromObject(sessionT);
            HolderSessionTranscriptBytes = key_cbor_obj.EncodeToBytes(CBOREncodeOptions.DefaultCtap2Canonical);
        }

        private static byte[] EncodeKeyBytes(CBORObject obj)
        {
            //CBORObject key_cbor_obj = CBORObject.FromObject(pubKey);
            byte[] pub_key_enc = obj.EncodeToBytes();

            return pub_key_enc;
        }
       

        // algorithm exemple: AlgorithmValues.HMAC_SHA_256 // Por enquanto so funciona com a curva P-256
        public Holder(string algorithm,string keyType,string parameters)
        {
            
            if (algorithm == "ECDSA_256" && keyType == "EC")
            {
                EDeviceKey = Key.GenerateKey(AlgorithmValues.ECDH_ES_HKDF_256, GeneralValues.KeyType_EC, parameters);
                PrivKey = EDeviceKey.AsPrivateKey();
                PubKey = EDeviceKey.AsPublicKey();

                OneKeyPub = new Key(parameters, PubKey);
                OneKeyPriv = new Key(parameters, PrivKey);

                Map_PubKey = OneKeyPub.Map;
                Map_PrivKey = OneKeyPriv.Map;

                EDeviceKeyBytes = EncodeKeyBytes(Map_PubKey);

                device_Engagement = new Device_Engagement(EDeviceKeyBytes);

                Console.WriteLine("Private Key is: " + print._PrintCBOR(OneKeyPriv.Map,0));

                //Signing Message steps: 1- Create Signer, 2- set private Key, 3- Add his protected attributes,
                //4- Create message, 5- Add created signer as signer of this message, 6- Sign Message 
                Signer signer = new Signer();
                signer.SetKey(OneKeyPriv);
                signer.AddAttribute(HeaderKeys.Algorithm, AlgorithmValues.ECDSA_256, Attributes.PROTECTED);

                Sign1Message msg = new Sign1Message();
                msg.SetContent("Jony");

                //msg.AddSigner(OneKeyPriv,AlgorithmValues.ECDSA_256);
                msg.Sign(OneKeyPriv,AlgorithmValues.ECDSA_256);
                var msgB = msg.EncodeToBytes();

                // Validate Message with a respective public key
                bool t = msg.Validate(OneKeyPub);
                Console.WriteLine("private Key" + print.PrintCBOR(EncodeKeyBytes(Map_PrivKey)));
                Console.WriteLine("Sign1 structure: " + print._PrintCBOR(msg.Encode(),0));
                Console.WriteLine("Payload value: " + BitConverter.ToString(msg.GetContent()));
                Console.WriteLine("Signature value: " + BitConverter.ToString(msg.Signature));
                Console.WriteLine("Protected value: " + print._PrintCBOR(msg.ProtectedMap, 0));
                Console.WriteLine("Unprotected value: " + print._PrintCBOR(msg.UnprotectedMap, 0));
                Console.WriteLine("Validation result: " + t.ToString());


            }
            if (algorithm == "ECDSA_384" && keyType == "EC")
            {
              
                EDeviceKey = Key.GenerateKey(AlgorithmValues.ECDH_ES_HKDF_256, GeneralValues.KeyType_EC, parameters);
                AsymmetricKeyParameter EDeviceKeyPriv = EDeviceKey.AsPrivateKey();
                PubKey = EDeviceKey._publicKey;
                PrivKey = EDeviceKey.PrivateKey;

                OneKeyPub = new Key(parameters, PubKey);
                OneKeyPriv = new Key(parameters, PrivKey);


                Map_PubKey = OneKeyPub.Map;
                Map_PrivKey = OneKeyPriv.Map;

                EDeviceKeyBytes = EncodeKeyBytes(Map_PubKey);

                device_Engagement = new Device_Engagement(EDeviceKeyBytes);

                // Signing Message steps: 1 - Create Signer, 2 - set private Key, 3- Add his protected attributes,
                //4- Create message, 5- Add created signer as signer of this message, 6- Sign Message 
                Signer signer = new Signer();
                signer.SetKey(OneKeyPriv);
                signer.AddAttribute(HeaderKeys.Algorithm, AlgorithmValues.ECDSA_256, Attributes.PROTECTED);

                Sign1Message msg = new Sign1Message();
                msg.SetContent("hugo");
                msg.SetExternalData(new byte[0]);

                //msg.AddSigner(OneKeyPriv,AlgorithmValues.ECDSA_256);
                msg.Sign(OneKeyPriv, AlgorithmValues.ECDSA_256);

                // Validate Message with a respective public key
                bool t = msg.Validate(OneKeyPub);
                Console.WriteLine("private Key" + print.PrintCBOR(EDeviceKeyBytes));
                Console.WriteLine("Sign1 structure: " + print._PrintCBOR(msg.EncodeToCBORObject(),0));
                Console.WriteLine("Payload value: " + Encoding.Default.GetString(msg.GetContent()));
                Console.WriteLine("Signature value: " + BitConverter.ToString(signer.Signature));
                Console.WriteLine("Protected value: " + print._PrintCBOR(msg.ProtectedMap,0));
                Console.WriteLine("Unprotected value: " + print._PrintCBOR(msg.UnprotectedMap,0));
       
            }
            if (algorithm == "ECDSA_512" && keyType == "EC")
            {
                PrettyPrint print = new PrettyPrint();

                EDeviceKey = Key.GenerateKey(AlgorithmValues.ECDH_ES_HKDF_512, GeneralValues.KeyType_EC, parameters);
                AsymmetricKeyParameter EDeviceKeyPriv = EDeviceKey.AsPrivateKey();
                PubKey = EDeviceKey._publicKey;
                PrivKey = EDeviceKey.PrivateKey;

                OneKeyPub = new Key(parameters, PubKey);
                OneKeyPriv = new Key(parameters, PrivKey);

                

                Map_PubKey = OneKeyPub.Map;
                Map_PrivKey = OneKeyPriv.Map;

                EDeviceKeyBytes = EncodeKeyBytes(Map_PubKey);

                device_Engagement = new Device_Engagement(EDeviceKeyBytes);

                // Signing Message steps: 1 - Create Signer, 2 - set private Key, 3- Add his protected attributes,
                //4- Create message, 5- Add created signer as signer of this message, 6- Sign Message 
                Signer signer = new Signer();
                signer.SetKey(OneKeyPriv);
                signer.AddAttribute(HeaderKeys.Algorithm, AlgorithmValues.ECDSA_256, Attributes.PROTECTED);

                Sign1Message msg = new Sign1Message();
                msg.SetContent("hugo");

                //msg.AddSigner(OneKeyPriv,AlgorithmValues.ECDSA_256);
                msg.Sign(OneKeyPriv, AlgorithmValues.ECDSA_256);

                // Validate Message with a respective public key
                bool t = msg.Validate(OneKeyPub);
                Console.WriteLine("Bool value: " + t.ToString());

            }

            // Create DeviceEnagement structure
            DeviceEngagement = new Device_Engagement(EDeviceKeyBytes);
            // Encode device engagement to cbor bytes
            Device_Eng_to_Bytes(DeviceEngagement);
            // add device engagement bytes to SessionTranscript structure
            Console.WriteLine("lenght " + device_engagement_bytes.Length);
            HolderSessionTranscript.DeviceEngagement = device_engagement_bytes;
            // Session Transcript to bytes
            EncodeCBORSessionTrans(HolderSessionTranscript);


        }

        // Used to perform ECDH key agreement with the return of the shared secret
        public void KeyAgreementSecret()
        {
            IBasicAgreement agreement = AgreementUtilities.GetBasicAgreement("ECDH");
            agreement.Init(PrivKey);
            // Note: Instead of "PubKey" here should be inserted the reader's public key
            BigInteger shared_secret = agreement.CalculateAgreement(PubKey);
            
            sharedSecret = shared_secret.ToByteArrayUnsigned();
           
        }

        // Used to derive holder's SKDevice key for connection establishment
        public void DeriveDeviceKey()
        {
            byte[] bytes = Encoding.Default.GetBytes("SKDevice");
            string deviceInfo = Encoding.UTF8.GetString(bytes);

            SKDeviceKey = HKDF.DeriveKey(HolderSessionTranscriptBytes, sharedSecret, Encoding.UTF8.GetBytes(deviceInfo), 32);
        }


    }
}