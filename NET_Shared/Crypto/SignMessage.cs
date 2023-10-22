using System;

using PeterO.Cbor;

using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using crypto;
using utils;

namespace cose
{
    public class SignMessage
    {
        public CBORObject structure;

        private CBORObject protectedMap;
        private CBORObject unprotectedMap;

        private byte[] payload;
        private byte[] signature;

        private readonly SecureRandom sr = new SecureRandom();

        private Key keyToSign;


        public byte[] Signature
        {
            get { return signature; }
            set { signature = value; }
        }

        public CBORObject Structure
        {
            get { return structure; }
        }

        public AsymmetricKeyParameter GetCertificatePublicKey()
        {
            X509Certificate certificate = new X509Certificate(unprotectedMap[33].GetByteString());

            return certificate.GetPublicKey();
        }

        public void GenerateStructure()
        {
            structure = CBORObject.NewArray()
                    .Add(protectedMap.EncodeToBytes())
                    .Add(unprotectedMap).Add(payload)
                    .Add(signature);
        }
        public void SetCertificate(byte[] certificate = null)
        {
            if(certificate == null)
            {
                AddAttribute(33, certificate, 2);
            }
            
        }

        public SignMessage()
        {
            protectedMap = CBORObject.NewMap();
            unprotectedMap = CBORObject.NewMap();
            payload = null;
        }

        PrettyPrint print = new PrettyPrint();
        public CBORObject FindAttribute(CBORObject label)
        {
            if (protectedMap.ContainsKey(label)) return protectedMap[label];
            if (unprotectedMap.ContainsKey(label)) return unprotectedMap[label];
            return null;
        }

        public void RemoveAttribute(CBORObject label)
        {
            if (protectedMap.ContainsKey(label)) protectedMap.Remove(label);
            if (unprotectedMap.ContainsKey(label)) unprotectedMap.Remove(label);
        }
        public void AddAttribute(int label, byte[] value, int bucket)
        {
            AddAttribute(CBORObject.FromObject(label), CBORObject.FromObject(value), bucket);
        }

        public void AddAttribute(int label, int value, int bucket)
        {
            AddAttribute(CBORObject.FromObject(label), CBORObject.FromObject(value), bucket);
        }

        public void AddAttribute(CBORObject label, CBORObject value, int bucket)
        {

            if ((label.Type != CBORType.Integer) && (label.Type != CBORType.TextString))
            {
              
                throw new CoseException("Labels must be integers or strings");
            }
            switch (bucket)
            {

                case 1:

                    RemoveAttribute(label);
                    protectedMap.Add(label, value);
                    break;

                case 2:
                    RemoveAttribute(label);
                    unprotectedMap.Add(label, value);
                    break;

                default:
                    throw new CoseException("Invalid attribute location given");
            }
        }

        public void DecodeFromCBORObject(CBORObject messageObject)
        {

            if (messageObject.Count != 4) throw new CoseException("Invalid Sign1 structure");

            if (messageObject[0].Type == CBORType.ByteString)
            {
                if (messageObject[0].GetByteString().Length == 0) protectedMap = CBORObject.NewMap();
                else
                {
                    protectedMap = CBORObject.DecodeFromBytes(messageObject[0].GetByteString());
                    
                }
            }
            else throw new CoseException("Invalid Sign1 structure");

            if (messageObject[1].Type == CBORType.Map)
            {
                unprotectedMap = messageObject[1];
            }
            else throw new CoseException("Invalid Sign1 structure");


            if (messageObject[3].Type == CBORType.ByteString) signature = messageObject[3].GetByteString();
            else throw new CoseException("Invalid Sign1 structure");
            

        }

        public void AddSigner(Key key, CBORObject algorithm = null)
        {
            if (algorithm != null)
            {
                AddAttribute(HeaderKeys.Algorithm, algorithm, 1);
            }


            if (key.ContainsName("use"))
            {
                string usage = key.AsString("use");
                if (usage != "sig") throw new CoseException("Key cannot be used for encrytion");
            }

            if (key.ContainsName(CoseKeyKeys.Key_Operations))
            {
                CBORObject usageObject = key[CoseKeyKeys.Key_Operations];
                bool validUsage = false;

                if (usageObject.Type != CBORType.Array) throw new CoseException("key_ops is incorrectly formed");
                for (int i = 0; i < usageObject.Count; i++)
                {
                    switch (usageObject[i].AsString())
                    {
                        case "encrypt":
                        case "keywrap":
                            validUsage = true;
                            break;
                    }
                }
                if (!validUsage) throw new CoseException("Key cannot be used for encryption");
            }

            keyToSign = key;
        }

        public void Sign(Key privateKey, byte[] toBeSigned, CBORObject curve_id)
        {
            if (curve_id.CompareTo(GeneralValues.P256) == 0)
            {
                AddSigner(privateKey, AlgorithmValues.ECDSA_256);
            }
            if (curve_id.CompareTo(GeneralValues.P384) == 0)
            {
                AddSigner(privateKey, AlgorithmValues.ECDSA_384);
            }
            if (curve_id.CompareTo(GeneralValues.P521) == 0)
            {
                AddSigner(privateKey, AlgorithmValues.ECDSA_512);
            }

            signature = PerformSignature(toBeSigned);
        }


        private BigInteger ConvertBigNum(CBORObject cbor)
        {
            byte[] rgb = cbor.GetByteString();
            byte[] rgb2 = new byte[rgb.Length + 2];
            rgb2[0] = 0;
            rgb2[1] = 0;
            for (int i = 0; i < rgb.Length; i++) rgb2[i + 2] = rgb[i];

            return new BigInteger(rgb2);
        }

        private byte[] PerformSignature(byte[] bytesToBeSigned)
        {
            CBORObject alg;

            alg = FindAttribute(HeaderKeys.Algorithm);

            if (alg == null)
            {
                if (keyToSign[CoseKeyKeys.KeyType].Type == CBORType.Integer)
                {
                    switch ((GeneralValuesInt)keyToSign[CoseKeyKeys.KeyType].AsInt32())
                    {

                        case GeneralValuesInt.KeyType_EC2:
                            if (keyToSign[CoseKeyParameterKeys.EC_Curve].Type == CBORType.Integer)
                            {
                                switch ((GeneralValuesInt)keyToSign[CoseKeyParameterKeys.EC_Curve].AsInt32())
                                {
                                    case GeneralValuesInt.P256:
                                        alg = AlgorithmValues.ECDSA_256;
                                        break;

                                    case GeneralValuesInt.P384:
                                        alg = AlgorithmValues.ECDSA_384;
                                        break;

                                    case GeneralValuesInt.P521:
                                        alg = AlgorithmValues.ECDSA_512;
                                        break;

                                    default:
                                        throw new CoseException("Unknown curve");
                                }
                            }
                            else if (keyToSign[CoseKeyParameterKeys.EC_Curve].Type == CBORType.TextString)
                            {
                                switch (keyToSign[CoseKeyParameterKeys.EC_Curve].AsString())
                                {
                                    case "P-384":
                                        alg = CBORObject.FromObject("ES384");
                                        break;

                                    default:
                                        throw new CoseException("Unknown curve");
                                }
                            }
                            else throw new CoseException("Curve is incorrectly encoded");
                            break;

                        default:
                            throw new CoseException("Unknown or unsupported key type " + keyToSign.AsString("kty"));
                    }
                }
                else if (keyToSign[CoseKeyKeys.KeyType].Type == CBORType.TextString)
                {
                    throw new CoseException("Unknown or unsupported key type " + keyToSign[CoseKeyKeys.KeyType].AsString());
                }
                else throw new CoseException("Key type is not correctly encoded");

            }

            IDigest digest = null;
            IDigest digest2 = null;

            if (alg.Type == CBORType.TextString)
            {
                switch (alg.AsString())
                {
                    case "ES384":
                        digest = new Sha384Digest();
                        digest2 = new Sha384Digest();
                        break;

                    default:
                        throw new CoseException("Unknown Algorithm Specified");
                }
            }
            else if (alg.Type == CBORType.Integer)
            {
                switch ((AlgorithmValuesInt)alg.AsInt32())
                {
                    case AlgorithmValuesInt.ECDSA_256:
                        digest = new Sha256Digest();
                        digest2 = new Sha256Digest();
                        break;

                    case AlgorithmValuesInt.ECDSA_384:
                        digest = new Sha384Digest();
                        digest2 = new Sha384Digest();
                        break;

                    case AlgorithmValuesInt.ECDSA_512:
                        digest = new Sha512Digest();
                        digest2 = new Sha512Digest();
                        break;

                    default:
                        throw new CoseException("Unknown Algorithm Specified");
                }
            }
            else throw new CoseException("Algorithm incorrectly encoded");

            if (alg.Type == CBORType.TextString)
            {
                switch (alg.AsString())
                {

                    default:
                        throw new CoseException("Unknown Algorithm Specified");
                }
            }
            else if (alg.Type == CBORType.Integer)
            {
                switch ((AlgorithmValuesInt)alg.AsInt32())
                {
                    case AlgorithmValuesInt.ECDSA_256:
                    case AlgorithmValuesInt.ECDSA_384:
                    case AlgorithmValuesInt.ECDSA_512:
                        {
                            CBORObject privateKeyD = keyToSign[CoseKeyParameterKeys.EC_D];
                            if (privateKeyD == null) throw new CoseException("Private key required to sign");

                            SecureRandom random = sr;

                            digest.BlockUpdate(bytesToBeSigned, 0, bytesToBeSigned.Length);
                            byte[] digestedMessage = new byte[digest.GetDigestSize()];
                            digest.DoFinal(digestedMessage, 0);

                            X9ECParameters p = keyToSign.GetCurve();
                            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                            ECPrivateKeyParameters privKey =
                                new ECPrivateKeyParameters("ECDSA", ConvertBigNum(privateKeyD), parameters);
                            ParametersWithRandom param = new ParametersWithRandom(privKey, random);

                            ECDsaSigner ecdsa = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
                            ecdsa.Init(true, param);

                            BigInteger[] sigLms = ecdsa.GenerateSignature(digestedMessage);

                            byte[] r = sigLms[0].ToByteArrayUnsigned();
                            byte[] s = sigLms[1].ToByteArrayUnsigned();

                            int cbR = (p.Curve.FieldSize + 7) / 8;

                            byte[] sigs = new byte[cbR * 2];
                            Array.Copy(r, 0, sigs, cbR - r.Length, r.Length);
                            Array.Copy(s, 0, sigs, cbR + cbR - s.Length, s.Length);

                            return sigs;
                        }

                    default:
                        throw new CoseException("Unknown Algorithm Specified");
                }
            }
            else throw new CoseException("Algorithm incorrectly encoded");
        }


        public bool Validate(Key signerKey, byte[] bytesToBeSigned)
        {
            CBORObject alg; // Get the set algorithm or infer one

            Console.WriteLine("are null " + print._PrintCBOR(signerKey.Map,0));
            alg = FindAttribute(HeaderKeys.Algorithm);

            if (alg == null)
            {
                throw new CoseException("No algorithm specified");
            }

            IDigest digest = null;
            IDigest digest2 = null;

            if (alg.Type == CBORType.TextString)
            {
                switch (alg.AsString())
                {
                    case "ES384":
                    case "PS384":
                        digest = new Sha384Digest();
                        digest2 = new Sha384Digest();
                        break;

                    default:
                        throw new CoseException("Unknown signature algorithm");
                }
            }
            else if (alg.Type == CBORType.Integer)
            {
                switch ((AlgorithmValuesInt)alg.AsInt32())
                {
                    case AlgorithmValuesInt.ECDSA_256:
                        digest = new Sha256Digest();
                        digest2 = new Sha256Digest();
                        break;

                    case AlgorithmValuesInt.ECDSA_384:
                        digest = new Sha384Digest();
                        digest2 = new Sha384Digest();
                        break;

                    case AlgorithmValuesInt.ECDSA_512:
                        digest = new Sha512Digest();
                        digest2 = new Sha512Digest();
                        break;


                    default:
                        throw new CoseException("Unknown signature algorithm");
                }
            }
            else throw new CoseException("Algorthm incorrectly encoded");

            if (alg.Type == CBORType.TextString)
            {
                switch (alg.AsString())
                {
                    default:
                        throw new CoseException("Unknown Algorithm");
                }
            }
            else if (alg.Type == CBORType.Integer)
            {
                switch ((AlgorithmValuesInt)alg.AsInt32())
                {
                    

                    case AlgorithmValuesInt.ECDSA_256:
                    case AlgorithmValuesInt.ECDSA_384:
                    case AlgorithmValuesInt.ECDSA_512:
                        {
                            Console.WriteLine("issahere1");
                            digest.BlockUpdate(bytesToBeSigned, 0, bytesToBeSigned.Length);
                            byte[] digestedMessage = new byte[digest.GetDigestSize()];
                            digest.DoFinal(digestedMessage, 0);
                            Console.WriteLine("issahere2");
                            X9ECParameters p = signerKey.GetCurve();
                            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                            ECPoint point = signerKey.GetPoint();
                            ECPublicKeyParameters param = new ECPublicKeyParameters(point, parameters);
                            Console.WriteLine("issahere3");
                            ECDsaSigner ecdsa = new ECDsaSigner();
                            ecdsa.Init(false, param);
                            Console.WriteLine("issahere4");
                            BigInteger r = new BigInteger(1, signature, 0, signature.Length / 2);
                            BigInteger s = new BigInteger(1, signature, signature.Length / 2, signature.Length / 2);
                            Console.WriteLine("issahere5");
                            return ecdsa.VerifySignature(digestedMessage, r, s);
                        }
                   

                    default:
                        throw new CoseException("Unknown Algorithm");
                }
            }
            else throw new CoseException("Algorithm incorrectly encoded");
        }




    }
}