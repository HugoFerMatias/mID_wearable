using PeterO.Cbor;
using System;

namespace cose
{
    public enum Tags
    {
        [Obsolete]
        Encrypted = 16,
        [Obsolete]
        Enveloped = 96,
        [Obsolete("Use Tags.Sign")]
        Signed = 98,
        Sign = 98,
        MAC = 97, MAC0 = 17,
        [Obsolete("Use Sign1")]
        Signed0 = 18,
        Sign1 = 18,
        Unknown = 0,
        Encrypt0 = 16,
        Encrypt = 96
    }

    public enum GeneralValuesInt
    {
        KeyType_EC2 = 2,
        P256 = 1, P384 = 2, P521 = 3
    }

    public static class GeneralValuesIntExtensions
    {
        public static string GetGeneralString(GeneralValuesInt value)
        {
            switch (value)
            {
                case GeneralValuesInt.P256:
                    return "P-256";
                case GeneralValuesInt.P384:
                    return "P-384";
                case GeneralValuesInt.P521:
                    return "P-521";
                default:
                    return "ERROR: INVALID PARAMETER";
            }
        }
    }

    // Cria o CBOR object a partir dos valores atribuidos
    // Valores genéricos de tipos de chave e curvas
    public class GeneralValues
    {
        public static readonly CBORObject KeyType_EC = CBORObject.FromObject(GeneralValuesInt.KeyType_EC2);
        public static readonly CBORObject P256 = CBORObject.FromObject(GeneralValuesInt.P256);
        public static readonly CBORObject P384 = CBORObject.FromObject(GeneralValuesInt.P384);
        public static readonly CBORObject P521 = CBORObject.FromObject(GeneralValuesInt.P521);
    }

    // Rótulo
    public class CoseKeyKeys
    {
        public static readonly CBORObject KeyType = CBORObject.FromObject(1);
        public static readonly CBORObject Key_Operations = CBORObject.FromObject(4);
    } 

    // Rótulo para identificar parametro de curva
    public class CoseKeyParameterKeys
    {
        public static readonly CBORObject EC_Curve = CBORObject.FromObject(-1); // crv 
        public static readonly CBORObject EC_X = CBORObject.FromObject(-2);
        public static readonly CBORObject EC_Y = CBORObject.FromObject(-3);
        public static readonly CBORObject EC_D = CBORObject.FromObject(-4);
    }


    // Identificadores de algoritmos
    public enum AlgorithmValuesInt
    {
        AES_GCM_128 = 1, AES_GCM_192 = 2, AES_GCM_256 = 3,
        ECDSA_256 = -7, ECDSA_384 = -35, ECDSA_512 = -36,


    }

    // Identificadores de algoritmos em cbor
    public class AlgorithmValues
    {

        public static readonly CBORObject AES_GCM_128 = CBORObject.FromObject(AlgorithmValuesInt.AES_GCM_128);
        public static readonly CBORObject AES_GCM_192 = CBORObject.FromObject(AlgorithmValuesInt.AES_GCM_192);
        public static readonly CBORObject AES_GCM_256 = CBORObject.FromObject(AlgorithmValuesInt.AES_GCM_256);

        public static readonly CBORObject ECDSA_256 = CBORObject.FromObject(AlgorithmValuesInt.ECDSA_256);
        public static readonly CBORObject ECDSA_384 = CBORObject.FromObject(AlgorithmValuesInt.ECDSA_384);
        public static readonly CBORObject ECDSA_512 = CBORObject.FromObject(AlgorithmValuesInt.ECDSA_512);

    }

    // Rótulos utilizados nos protected e unprotected buckets da estrutura COSE_Sign1
    public class HeaderKeys
    {
        public static readonly CBORObject Algorithm = CBORObject.FromObject(1);
        public static readonly CBORObject KeyId = CBORObject.FromObject(4);
        public static readonly CBORObject IV = CBORObject.FromObject(5);
       
    }

   
}
