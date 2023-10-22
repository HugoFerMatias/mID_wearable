using System;
using System.Collections.Generic;
using PeterO.Cbor;

using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;

using System.Linq;

using utils;

namespace cose
{
    public class CoseException : Exception

    {
        public CoseException(string code) : base(code) { }
    }

    public class Key
    {

        public CBORObject _map;
        public static Key pubKeyOneKey;
        public static Key privKeyOneKey;
       

        public AsymmetricKeyParameter PrivateKey { get; set; }

        //Usada para key agreement
        public AsymmetricKeyParameter _publicKey { get; set; }

        public ICollection<CBORObject> Keys => _map.Keys;

        public CBORObject this[CBORObject name]
        {
            get => _map[name];
        } 

        public string AsString(string name)
        {
            return _map[name].AsString();
        }

        public byte[] AsBytes(CBORObject name)
        {
            return _map[name].GetByteString();
        }

        public CBORObject Map
        {
            get { return _map; }
        }

        public Key()
        {
            _map = CBORObject.NewMap();

        }

        public void addTag()
        {
            byte[] auxB = _map.EncodeToBytes();
            _map = CBORObject.FromObjectAndTag(auxB,24);
        }

        public Key(CBORObject objKey)
        {
            _map = objKey;
        }

        // Utilizar identificador de curva eliptica para cosntruir estrutura Key (VER CLASSE GeneralValuesInt em CryptoUtils.cs )
        public Key(int elliptic_curve_id, AsymmetricKeyParameter key) : this()
        {
            if (key != null)
            {
                if (elliptic_curve_id == 1)
                {
                    FromKey(key, GeneralValuesIntExtensions.GetGeneralString(GeneralValuesInt.P256));
                }
                if (elliptic_curve_id == 2)
                {
                    FromKey(key, GeneralValuesIntExtensions.GetGeneralString(GeneralValuesInt.P384));
                }
                if (elliptic_curve_id == 3)
                {
                    FromKey(key, GeneralValuesIntExtensions.GetGeneralString(GeneralValuesInt.P521));
                }
            }
        }

        public Key(AsymmetricKeyParameter key, string parameters = null) : this()
        {
            if (parameters == null)
            {
                parameters = "P-256";
            }

            if (key != null)
            {

                FromKey(key, parameters);

            }
        }

        public static Key GenerateKey(CBORObject keyType = null, string parameters = null)
        {
           
            if (parameters == null) parameters = "P-256";
            return GenerateEC2Key(parameters);
             
            return null;
        }

        private static Key GenerateEC2Key(string genParameters)
        {
            PrettyPrint print = new PrettyPrint();

            X9ECParameters p = NistNamedCurves.GetByName(genParameters);


            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);

            ECKeyPairGenerator pGen = new ECKeyPairGenerator();
            ECKeyGenerationParameters genParam = new ECKeyGenerationParameters(parameters, new SecureRandom());
            pGen.Init(genParam);

            AsymmetricCipherKeyPair p1 = pGen.GenerateKeyPair();

            Key newKey = new Key();

            newKey.FromKey(p1.Public, genParameters);

            ICollection<CBORObject> keys = newKey.Map.Keys;
            ICollection<CBORObject> values = newKey.Map.Values;

            var dic = keys.Zip(values, (k, v) => new { Key = k, Value = v });

            CBORObject newmap = CBORObject.NewMap();

            int i = 0;

            foreach (var pair in dic)
            {
                newmap.Add(pair.Key, pair.Value);
            }

            pubKeyOneKey = new Key(newmap);



            Key privKey = new Key();

            privKey.FromKey(p1.Private, genParameters);

            privKeyOneKey = privKey;



            foreach (CBORObject key in privKey.Keys)
            {
                if (newKey.ContainsName(key))
                {
                    if (!privKey[key].Equals(newKey[key]))
                    {
                        throw new CoseException("Internal error merging keys");
                    }
                }
                else
                {
                    newKey.Add(key, privKey[key]);
                }
            }
           

            //Console.WriteLine(print._PrintCBOR(newKey.Map, 0));

            return newKey;
        }

        public void FromKey(AsymmetricKeyParameter x, string genParameters = null)
        {
            PrettyPrint print = new PrettyPrint();
            if (x is ECPrivateKeyParameters)
            {

                ECPrivateKeyParameters priv = (ECPrivateKeyParameters)x;
                Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_EC);
                if (genParameters == "P-256")
                {
                    Add(CoseKeyParameterKeys.EC_Curve, GeneralValues.P256);
                }
                if (genParameters == "P-384")
                {
                    Add(CoseKeyParameterKeys.EC_Curve, GeneralValues.P384);
                }
                if (genParameters == "P-521")
                {

                    Add(CoseKeyParameterKeys.EC_Curve, GeneralValues.P521);

                }
                Add(CoseKeyParameterKeys.EC_D, CBORObject.FromObject(priv.D.ToByteArrayUnsigned()));
                
            }
            else if (x is ECPublicKeyParameters)
            {
                ECPublicKeyParameters pub = (ECPublicKeyParameters)x;
                _map.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_EC);
                if (genParameters == "P-256")
                {
                    Add(CoseKeyParameterKeys.EC_Curve, GeneralValues.P256);
                }
                if (genParameters == "P-384")
                {
                    Add(CoseKeyParameterKeys.EC_Curve, GeneralValues.P384);
                }
                if (genParameters == "P-521")
                {
                    Add(CoseKeyParameterKeys.EC_Curve, GeneralValues.P521);
                }
                
                _map.Add(CoseKeyParameterKeys.EC_X, pub.Q.AffineXCoord.ToBigInteger().ToByteArrayUnsigned());
                _map.Add(CoseKeyParameterKeys.EC_Y, pub.Q.AffineYCoord.ToBigInteger().ToByteArrayUnsigned());
                
            }
            
            else
            {
                throw new CoseException("Unrecognized key type");
            }
        }
        public AsymmetricKeyParameter AsPrivateKey()
        {
            if (PrivateKey != null)
            {
                return PrivateKey;
            }

            switch (GetKeyType())
            {
                case GeneralValuesInt.KeyType_EC2:
                    X9ECParameters p = GetCurve();
                    ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                    ECPrivateKeyParameters privKey = new ECPrivateKeyParameters("ECDSA", ConvertBigNum(this[CoseKeyParameterKeys.EC_D]), parameters);
                    PrivateKey = privKey;
                    break;

              

                default:
                    throw new CoseException("Unable to get the private key.");
            }
            return PrivateKey;
        }

        public X9ECParameters GetCurve()
        {
            CBORObject cborKeyType = _map[CoseKeyKeys.KeyType];

            if (cborKeyType == null)
            {
                throw new CoseException("Malformed key struture");
            }

            if ((cborKeyType.Type != CBORType.Integer) &&
                !((cborKeyType.Equals(GeneralValues.KeyType_EC))))
            {
                throw new CoseException("Not an EC key");
            }

            CBORObject cborCurve = _map[CoseKeyParameterKeys.EC_Curve];

            if (cborCurve.Type == CBORType.Integer)
            {
                switch ((GeneralValuesInt)cborCurve.AsInt32())
                {
                    case GeneralValuesInt.P256:
                        return NistNamedCurves.GetByName("P-256");
                    case GeneralValuesInt.P384:
                        return NistNamedCurves.GetByName("P-384");
                    case GeneralValuesInt.P521:
                        return NistNamedCurves.GetByName("P-521");
                    default:
                        throw new CoseException("Unsupported key type: " + cborKeyType.AsInt32());
                }
            }
            else if (cborCurve.Type == CBORType.TextString)
            {
                switch (cborCurve.AsString())
                {
                    default:
                        throw new CoseException("Unsupported key type: " + cborKeyType.AsString());
                }
            }
            else
            {
                throw new CoseException("Incorrectly encoded key type");
            }
        }

        public AsymmetricKeyParameter AsPublicKey()
        {
            switch (GetKeyType())
            {
                case GeneralValuesInt.KeyType_EC2:
                    X9ECParameters p = GetCurve();
                    ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                    ECPoint point = GetPoint();
                    ECPublicKeyParameters param = new ECPublicKeyParameters(point, parameters);
                    _publicKey = param;
                    break;

                default:
                    throw new CoseException("Unable to get the public key.");
            }

            return _publicKey;
        }

        public ECPoint GetPoint()
        {
            X9ECParameters p = GetCurve();
            ECPoint pubPoint;

            switch ((GeneralValuesInt)this[CoseKeyKeys.KeyType].AsInt32())
            {
                case GeneralValuesInt.KeyType_EC2:
                    CBORObject y = _map[CoseKeyParameterKeys.EC_Y];

                    if (y.Type == CBORType.Boolean)
                    {
                        byte[] x = _map[CoseKeyParameterKeys.EC_X].GetByteString();
                        byte[] rgb = new byte[x.Length + 1];
                        Array.Copy(x, 0, rgb, 1, x.Length);
                        rgb[0] = (byte)(2 + (y.AsBoolean() ? 1 : 0));
                        pubPoint = p.Curve.DecodePoint(rgb);
                    }
                    else
                    {
                        pubPoint = p.Curve.CreatePoint(AsBigInteger(CoseKeyParameterKeys.EC_X), AsBigInteger(CoseKeyParameterKeys.EC_Y));
                    }
                    break;
                default:
                    throw new Exception("Unknown key type");
            }
            return pubPoint;
        }

        public BigInteger AsBigInteger(CBORObject keyName)
        {

            byte[] rgb = _map[keyName].GetByteString();
            byte[] rgb2 = new byte[rgb.Length + 2];
            rgb2[0] = 0;
            rgb2[1] = 0;
            for (int i = 0; i < rgb.Length; i++)
            {
                rgb2[i + 2] = rgb[i];
            }

            return new BigInteger(rgb2);
        }

        private static BigInteger ConvertBigNum(CBORObject cbor)
        {
            byte[] rgb = cbor.GetByteString();
            byte[] rgb2 = new byte[rgb.Length + 2];
            rgb2[0] = 0;
            rgb2[1] = 0;
            for (int i = 0; i < rgb.Length; i++)
            {
                rgb2[i + 2] = rgb[i];
            }

            return new BigInteger(rgb2);
        }

        public GeneralValuesInt GetKeyType()
        {
            return (GeneralValuesInt)_map[CoseKeyKeys.KeyType].AsInt32();
        }

        internal void Replace(CBORObject key, CBORObject value)
        {
            _map[key] = value;
        }

        public void Add(CBORObject label, CBORObject value)
        {
            _map.Add(label, value);
        }

        public void Add(string name, string value)
        {
            _map.Add(name, value);
        }

        public Boolean ContainsName(string name)
        {
            return _map.ContainsKey(name);
        }

        public Boolean ContainsName(CBORObject key)
        {
            return _map.ContainsKey(key);
        }


    }
}