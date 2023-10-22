using PeterO.Cbor;

using Org.BouncyCastle.Crypto;

using crypto;
using engagement;

using System;
using utils;
using cose;
using Org.BouncyCastle.Asn1.Pkcs;

using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;


using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509;
using holder.certificate;

namespace holder
{
    public class Holder
    {

        /*
         * Since the same curve is gonna be used in all the algorithms, 
         * this variable stores the curve id as a CBORObject
         */
        private CBORObject curve_id;

        // Holder's Device Engagement structure
        private Engagement device_Engagement;
        private byte[] device_engagement_bytes;

        public CBORObject request;
        public byte[] response;

        public CBORObject readerAuthentication;
        public AsymmetricKeyParameter readerAuthKey;

        public byte[] deviceNameSpacesBytes = CBORObject.NewMap().EncodeToBytes();
        public string docType;

        public CBORObject deviceAuthentication;
        // Falta falar sobre isto com o professor
        private AsymmetricKeyParameter devicePrivAuthKey;
        public CBORObject deviceAuth;

        public CBORObject session_Transcript;
        public byte[] session_Transcript_bytes;

        public AsymmetricKeyParameter DeviceAuthTestPrivKey;

        // CBOR encoded COSE public key
        public byte[] EDeviceKeyBytes;
        private byte[] EDevicePrivateKey;
        public byte[] ReaderPubKey_bytes;

        // Holder's Session key used to encrypt the mdoc response
        private byte[] SKDeviceKey;
        private byte[] SKReaderKey;
        
        // ID's according to ISO
        private static readonly byte[] holder_id = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        private static readonly byte[] reader_id = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Usados para cifragem de dados de sessão
        private uint message_counter = 1;

        public CBORObject Request
        {
            get { return request; }
            set { request = value; }
        }

        public CBORObject ReaderAuthentication
        {
            get { return readerAuthentication; }
            set { readerAuthentication = value; }
        }

        public byte[] DeviceNameSpacesBytes
        {
            get { return deviceNameSpacesBytes; }
            set { deviceNameSpacesBytes = value; }
        }

        public CBORObject DeviceAuthentication
        {
            get { return deviceAuthentication; }
            set { deviceAuthentication = value; }
        }

        public CBORObject SessionTranscript
        {
            get { return session_Transcript; }
            set { session_Transcript = value; }
        }

        public byte[] Peripheral_UUID_bytes
        {
            get { return device_Engagement.PeripheralServer_UUID; }
            set { device_Engagement.PeripheralServer_UUID = value; }
        }

        public string DocType
        {
            get { return docType; }
            set { docType = value; }
        }

        public byte[] ReaderPubkeyBytes 
        { 
            get { return ReaderPubKey_bytes; }
            set { ReaderPubKey_bytes = value; }
        }

        public Engagement DeviceEngagement
        {
            get { return device_Engagement; }
            set { device_Engagement = value; }
        }

        public byte[] SessionTranscriptBytes
        {
            get { return session_Transcript_bytes; }
            set { session_Transcript_bytes = value; }
        }

        public byte[] DeviceEngagementBytes
        {
            get { return device_engagement_bytes; }
            set { device_engagement_bytes = value; }
        }

        public CBORObject DeviceAuth
        {
            get { return deviceAuth; }
            set { deviceAuth = value; }
        }

        public byte[] Response
        {
            get { return response; }
            set { response = value; }
        }
         
        /*
         * Note: Right now only curves P-256, P-384 and P-521 are implemented, as such, the argument "parameters"
         * should only accept the values representing these curves
         */
        public Holder(string parameters)
        {

            // Ver isto do key type (retirar ou deixar tar, provavelmente retirar)
            
                Key EDeviceKey = Key.GenerateKey(GeneralValues.KeyType_EC, parameters);

                AsymmetricKeyParameter PrivKey = EDeviceKey.AsPrivateKey();
                AsymmetricKeyParameter PubKey = EDeviceKey.AsPublicKey();

                DeviceAuthTestPrivKey = PrivKey;

                Key OneKeyPub = new Key(PubKey, parameters);
                Key OneKeyPriv = new Key(PrivKey, parameters);

                CBORObject Map_PubKey = OneKeyPub.Map.WithTag(24);
                CBORObject Map_PrivKey = OneKeyPriv.Map.WithTag(24);

                // Device public and private keys
                EDeviceKeyBytes =  Map_PubKey.EncodeToBytes();
                EDevicePrivateKey = Map_PrivKey.EncodeToBytes();

                if(parameters == "P-256")
                {
                    curve_id = GeneralValues.P256;
                }
                if (parameters == "P-384")
                {
                    curve_id = GeneralValues.P384;
                }
                if (parameters == "P-521")
                {
                    curve_id = GeneralValues.P521;
                }

            

            DeviceEngagement = new Engagement(EDeviceKeyBytes);
        }

        /*
         * Generates both the device and reader's session keys
         * Note: Reader's Public Key must be given to Holder before using this method
         */
        public void GenerateSessionKey()
        {
            
            GenerateSessionTranscript();
            SessionTranscriptBytes = SessionTranscript.EncodeToBytes();
            try
            {
                Tuple<byte[], byte[]> sessionKeyPair = Crypto.DeriveDeviceKey(ReaderPubkeyBytes, EDevicePrivateKey, SessionTranscriptBytes);
                SKDeviceKey = sessionKeyPair.Item1;
                SKReaderKey = sessionKeyPair.Item2;
            }
            catch
            {
                Console.WriteLine("Couldnt derive keys");
            }
        }

        // Validate Message with a respective public key
        /*
         * o argumento message deve ser o readerauthentication 
         */
        public bool ValidateReader()
        {

            SignMessage readerAuth = new SignMessage();
            readerAuth.DecodeFromCBORObject(Request["docRequests"][0]["readerAuth"]);

            readerAuth.GenerateStructure();
           
            readerAuthKey = readerAuth.GetCertificatePublicKey();

            bool t = readerAuth.Validate(new Key(readerAuthKey,null),ReaderAuthentication.EncodeToBytes());
            
            return t;
        }

        // Method used to encrypt holder's mDoc responses
        public byte[] EncryptData(byte[] data)
        {
            byte[] result = Crypto.EncryptSessionData(data, SKDeviceKey, holder_id, message_counter);
            // Check ISO to see when to reset counter
            message_counter++;
            return result;
        }

        // Method used to decrypt reader's mDoc requests
        public void DecryptData(byte[] data)
        {
            byte[] aux = Crypto.DecryptSessionData(data, reader_id, message_counter,SKReaderKey);
            Request = CBORObject.DecodeFromBytes(aux);
        }

        // Generates SessionTranscript structure used in both the device and reader's authentication mechanism
        public void GenerateSessionTranscript()
        {
            // Last element of array is null since QRCode engagement was used
            SessionTranscript = CBORObject.NewArray().Add(DeviceEngagementBytes).Add(ReaderPubkeyBytes).Add(CBORObject.Null);
        }

        // Generates the ReaderAuthentication structure for reader authentication
        public void GenerateReaderAuthentication()
        {
           
           ReaderAuthentication = CBORObject.NewArray().Add("ReaderAuthentication").Add(SessionTranscript).Add(Request["docRequests"][0]["itemsRequest"].GetByteString());

        }

        private byte[] GetCert()
        {
            Key authKeyPair = Key.GenerateKey(GeneralValues.KeyType_EC, "P-256");
            devicePrivAuthKey = authKeyPair.AsPrivateKey();

            X509Certificate certificate = CertificateX509.GenerateCertificate(devicePrivAuthKey, authKeyPair.AsPublicKey());

            //ECPublicKeyParameters pubkeyec = (ECPublicKeyParameters)certificatePubKey;
            //Console.WriteLine("OID IS " + pubkeyec.PublicKeyParamSet.Id);


            Console.WriteLine(certificate.ToString());

            return certificate.GetEncoded();

        }

        // Generates the DeviceAuthentication structure for device authentication
        public void GenerateDeviceAuthentication()
        {
            PrettyPrint print = new PrettyPrint();
            DeviceAuthentication = CBORObject.NewArray().Add("DeviceAuthentication").Add(SessionTranscript).Add(DocType).Add(DeviceNameSpacesBytes);
            Console.WriteLine("device authentication is this " + print._PrintCBOR(DeviceAuthentication,0)); ;
        }

        public void SignDeviceAuthentication()
        {
            SignMessage deviceAuthsign = new SignMessage();
            byte[] cert = GetCert();
            deviceAuthsign.SetCertificate(cert);
            deviceAuthsign.Sign(new Key(devicePrivAuthKey,null),DeviceAuthentication.EncodeToBytes(),curve_id);
            deviceAuthsign.GenerateStructure();
            DeviceAuth = deviceAuthsign.Structure;
        }

    }

  


namespace certificate
    {
        public class CertificateX509
        {
            public static X509Certificate GenerateCertificate(
                AsymmetricKeyParameter issuerPrivate,
                AsymmetricKeyParameter subjectPublic)
            {

                X509Name issuer = new X509Name("CN=HugoTest");
                X509Name subject = new X509Name("CN=TestHugo");


                ISignatureFactory signatureFactory;
                if (issuerPrivate is ECPrivateKeyParameters)
                {
                    signatureFactory = new Asn1SignatureFactory(
                        X9ObjectIdentifiers.ECDsaWithSha256.ToString(),
                        issuerPrivate);
                }
                else
                {
                    signatureFactory = new Asn1SignatureFactory(
                        PkcsObjectIdentifiers.Sha256WithRsaEncryption.ToString(),
                        issuerPrivate);
                }

                var certGenerator = new X509V3CertificateGenerator();
                certGenerator.SetIssuerDN(issuer);
                certGenerator.SetSubjectDN(subject);
                certGenerator.SetSerialNumber(BigInteger.ValueOf(1));
                certGenerator.SetNotAfter(DateTime.UtcNow.AddHours(1));
                certGenerator.SetNotBefore(DateTime.UtcNow);
                certGenerator.SetPublicKey(subjectPublic);
                return certGenerator.Generate(signatureFactory);
            }
        }
    }

}