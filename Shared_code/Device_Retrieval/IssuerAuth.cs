using System;
using System.Collections.Generic;
using crypto;
using static device_retrieval_response.IssuerSigned;

namespace device_retrieval_response
{

    public class IssuerAuth
    {
        // Precisa de issuerSigned para retirar os namespaces
        public IssuerSigned IssuerSigned;
        public Dictionary<string, Tuple<uint, byte[]>> issuerNameSpaces = IssuerSigned.IssuerNameSpaces_forAuth;

       /* public class MobileSecurityObject
        {
            private Digest digest = new Digest();

            public string version = "1.0";
            public string digestAlgorithm;
            ValueDigests valueDigests;// key é namespace valor é o respectivo digest de todos os elementos de dados desse namespace
            DeviceKeyInfo deviceKeyInfo;
            string docType;
            ValidityInfo info;

            public string DigestAlgorithm
            {
                get { return digestAlgorithm; }
                set {
                    if (value != "SHA-256" && value != "SHA-384" && value != "SHA-512")
                    {
                        throw new System.Exception("Algoritmo de digest inválido");
                    }

                    else { digestAlgorithm = value; }
                }
            }

            public byte[] digestCalc(byte[] issuerSignedItemByte)
            {
                if (digestAlgorithm == "SHA-256")
                {
                    return digest.sha256(issuerSignedItemByte);
                }

                if (digestAlgorithm == "SHA-384")
                {
                    return digest.sha384(issuerSignedItemByte);
                }

                if (digestAlgorithm == "SHA-512")
                {
                    return digest.sha512(issuerSignedItemByte);
                }

                else { throw new System.Exception("Algoritmo de digest não encontrado"); }
            }

            // Digest de issuerNameSpaceByte com respectivo namespace
            public void valueDigest(Dictionary<string, Tuple<uint, byte[]>> issuerNameSpaces)
            {
                //Namespace com respectivo digest
                Dictionary<string, Digests> dicValueDigests = new Dictionary<string, Digests>();

                foreach (var item in issuerNameSpaces)
                {
                    byte[] digest = digestCalc(item.Value.Item2);
                    //Criar tuplo Digest com digestID e o respectivo digest
                    Digests auxvalueDigests = new Digests(item.Value.Item1,digest);
                    // Adicionar ao dicionario o digestid+digest com o respectivo namespace
                    dicValueDigests.Add(item.Key, auxvalueDigests);
                }

                valueDigests.Value = dicValueDigests;
            }

        }*/

        public class DeviceKeyInfo
        {
            //COSE_Key deviceKey; // COSE key
            KeyAuthorizations keyAuthorizations;
            int keyInfo; // positive integers are RFU, negative integers may be used for proprietary use
        }

        public class KeyAuthorizations
        {
            public string[] authorizedNameSpaces;
            public string[] authorizedDataElements;
            public string[] dataElementsArray; // array of Identifiers of mdl_data 
        }

        public class ValueDigests
        {
            // namespace e digests com id
            Dictionary<string, Digests> valueDigests;

            public Dictionary<string, Digests> Value
            {
                get { return valueDigests; }
                set { valueDigests = value; }
            }
        }

        public class Digests
        {
            // digestid e digest
            Tuple<uint, byte[]> digestIDs;
            
            public Digests(uint id,byte[] digest)
            {
                digestIDs = new Tuple<uint, byte[]>(id, digest);
            }
            
            public Tuple<uint, byte[]> ID
            {
                get { return digestIDs; }
                set { digestIDs = value; }
            }
        }
        }

        public class ValidityInfo
        {
            string signed; // String de data(tempo)
            string validityFrom;// String de data(tempo)
            string validityUntil;// String de data(tempo)
            string expectedDate;// String de data(tempo) (timestamp)
        }

}
