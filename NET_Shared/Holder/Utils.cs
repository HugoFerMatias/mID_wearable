using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using PeterO.Cbor;
using Microsoft.IdentityModel.Tokens;

namespace utils
{
    public static class Utils
    {

        public static byte[] EncodedItemToCBORBytes(CBORObject item)
        {
            string itemstring = item.AsString();
            
            byte[] itembytes = Base64UrlEncoder.DecodeBytes(itemstring);
           
            return itembytes;

        }


        public static List<String> ReplaceUnderscore(List<String> list)
        {
            List<String> result = new List<String>();
            foreach (String str in list)
            {
                result.Add(str.Replace("_", " "));
            }

            return result;
        }


        // Converts uint to a 4 bytes Big-Endian byte array
        public static byte[] UIntToBytes(uint number) 
        {

            byte[] bytes = new byte[4];
            bytes = AddByteToArray(bytes, Convert.ToByte(number));
            
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return bytes;
        }

        // Adds byte to byte array
        public static byte[] AddByteToArray(byte[] bArray, byte newByte)
        {
            byte[] newArray = new byte[bArray.Length];
            bArray.CopyTo(newArray, 0);
            newArray[0] = newByte;
            return newArray;
        }

        // Combines two byte arrays
        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public struct Uuid : IEquatable<Uuid>
        {
            public readonly static Uuid Empty;

            static Uuid()
            {
                Empty = new Uuid();
            }


            private readonly long _leastSignificantBits;
            private readonly long _mostSignificantBits;

            /// <summary>
            /// Constructs a new UUID using the specified data.
            /// </summary>
            /// <param name="mostSignificantBits">The most significant 64 bits of the UUID.</param>
            /// <param name="leastSignificantBits">The least significant 64 bits of the UUID</param>
            public Uuid(long mostSignificantBits, long leastSignificantBits)
            {
                _mostSignificantBits = mostSignificantBits;
                _leastSignificantBits = leastSignificantBits;
            }

            /// <summary>
            /// Constructs a new UUID using the specified data.
            /// </summary>
            /// <param name="b">Bytes array that represents the UUID.</param>
            public Uuid(byte[] b)
            {
                if (b == null)
                    throw new ArgumentNullException("b");

                if (b.Length != 16)
                    throw new ArgumentException("Length of the UUID byte array should be 16");

                _mostSignificantBits = BitConverter.ToInt64(b, 0);
                _leastSignificantBits = BitConverter.ToInt64(b, 8);
            }

            /// <summary>
            /// The least significant 64 bits of this UUID's 128 bit value.
            /// </summary>
            public long LeastSignificantBits
            {
                get { return _leastSignificantBits; }
            }

            /// <summary>
            /// The most significant 64 bits of this UUID's 128 bit value.
            /// </summary>
            public long MostSignificantBits
            {
                get { return _mostSignificantBits; }
            }

            /// <summary>
            /// Returns a value that indicates whether this instance is equal to a specified
            /// object.
            /// </summary>
            /// <param name="obj">The object to compare with this instance.</param>
            /// <returns>true if o is a <paramref name="obj"/> that has the same value as this instance; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                if (!(obj is Uuid))
                {
                    return false;
                }

                Uuid uuid = (Uuid)obj;

                return Equals(uuid);
            }

            /// <summary>
            /// Returns a value that indicates whether this instance and a specified <see cref="Uuid"/>
            /// object represent the same value.
            /// </summary>
            /// <param name="uuid">An object to compare to this instance.</param>
            /// <returns>true if <paramref name="uuid"/> is equal to this instance; otherwise, false.</returns>
            public bool Equals(Uuid uuid)
            {
                return _mostSignificantBits == uuid._mostSignificantBits && _leastSignificantBits == uuid._leastSignificantBits;
            }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            /// <returns>The hash code for this instance.</returns>
            public override int GetHashCode()
            {
                return ((Guid)this).GetHashCode();
            }

            /// <summary>
            /// Returns a String object representing this UUID.
            /// </summary>
            /// <returns>A string representation of this UUID.</returns>
            public override string ToString()
            {
                //return ((Guid)this).ToString();
                return (GetDigits(_mostSignificantBits >> 32, 8) + "-" +
                    GetDigits(_mostSignificantBits >> 16, 4) + "-" +
                    GetDigits(_mostSignificantBits, 4) + "-" +
                    GetDigits(_leastSignificantBits >> 48, 4) + "-" +
                    GetDigits(_leastSignificantBits, 12));
            }

            /// <summary>
            ///  Returns a 16-element byte array that contains the value of this instance.
            /// </summary>
            /// <returns>A 16-element byte array</returns>
            public byte[] ToByteArray()
            {
                byte[] uuidMostSignificantBytes = BitConverter.GetBytes(_mostSignificantBits);
                byte[] uuidLeastSignificantBytes = BitConverter.GetBytes(_leastSignificantBits);
                byte[] bytes =
                {
                uuidMostSignificantBytes[0],
                uuidMostSignificantBytes[1],
                uuidMostSignificantBytes[2],
                uuidMostSignificantBytes[3],
                uuidMostSignificantBytes[4],
                uuidMostSignificantBytes[5],
                uuidMostSignificantBytes[6],
                uuidMostSignificantBytes[7],
                uuidLeastSignificantBytes[0],
                uuidLeastSignificantBytes[1],
                uuidLeastSignificantBytes[2],
                uuidLeastSignificantBytes[3],
                uuidLeastSignificantBytes[4],
                uuidLeastSignificantBytes[5],
                uuidLeastSignificantBytes[6],
                uuidLeastSignificantBytes[7]
            };

                return bytes;
            }

            /// <summary>Indicates whether the values of two specified <see cref="T:Uuid" /> objects are equal.</summary>
            /// <returns>true if <paramref name="a" /> and <paramref name="b" /> are equal; otherwise, false.</returns>
            /// <param name="a">The first object to compare. </param>
            /// <param name="b">The second object to compare. </param>
            public static bool operator ==(Uuid a, Uuid b)
            {
                return a.Equals(b);
            }

            /// <summary>Indicates whether the values of two specified <see cref="T:Uuid" /> objects are not equal.</summary>
            /// <returns>true if <paramref name="a" /> and <paramref name="b" /> are not equal; otherwise, false.</returns>
            /// <param name="a">The first object to compare. </param>
            /// <param name="b">The second object to compare. </param>
            public static bool operator !=(Uuid a, Uuid b)
            {
                return !a.Equals(b);
            }

            /// <summary>Converts an <see cref="T:Uuid"/> to a <see cref="T:System.Guid" />.</summary>
            /// <param name="uuid">The value to convert. </param>
            /// <returns>A <see cref="T:System.Guid"/> that represents the converted <see cref="T:Uuid" />.</returns>
            public static explicit operator Guid(Uuid uuid)
            {
                if (uuid == default(Uuid))
                {
                    return default(Guid);
                }

                byte[] uuidMostSignificantBytes = BitConverter.GetBytes(uuid._mostSignificantBits);
                byte[] uuidLeastSignificantBytes = BitConverter.GetBytes(uuid._leastSignificantBits);
                byte[] guidBytes =
                {
                uuidMostSignificantBytes[4],
                uuidMostSignificantBytes[5],
                uuidMostSignificantBytes[6],
                uuidMostSignificantBytes[7],
                uuidMostSignificantBytes[2],
                uuidMostSignificantBytes[3],
                uuidMostSignificantBytes[0],
                uuidMostSignificantBytes[1],
                uuidLeastSignificantBytes[7],
                uuidLeastSignificantBytes[6],
                uuidLeastSignificantBytes[5],
                uuidLeastSignificantBytes[4],
                uuidLeastSignificantBytes[3],
                uuidLeastSignificantBytes[2],
                uuidLeastSignificantBytes[1],
                uuidLeastSignificantBytes[0]
            };

                return new Guid(guidBytes);
            }

            /// <summary>Converts a <see cref="T:System.Guid" /> to an <see cref="T:Uuid"/>.</summary>
            /// <param name="value">The value to convert. </param>
            /// <returns>An <see cref="T:Uuid"/> that represents the converted <see cref="T:System.Guid" />.</returns>
            public static implicit operator Uuid(Guid value)
            {
                if (value == default(Guid))
                {
                    return default(Uuid);
                }

                byte[] guidBytes = value.ToByteArray();
                byte[] uuidBytes =
                {
                guidBytes[6],
                guidBytes[7],
                guidBytes[4],
                guidBytes[5],
                guidBytes[0],
                guidBytes[1],
                guidBytes[2],
                guidBytes[3],
                guidBytes[15],
                guidBytes[14],
                guidBytes[13],
                guidBytes[12],
                guidBytes[11],
                guidBytes[10],
                guidBytes[9],
                guidBytes[8]
            };


                return new Uuid(BitConverter.ToInt64(uuidBytes, 0), BitConverter.ToInt64(uuidBytes, 8));
            }

            /// <summary>
            /// Creates a UUID from the string standard representation as described in the <see cref="ToString()"/> method.
            /// </summary>
            /// <param name="input">A string that specifies a UUID.</param>
            /// <returns>A UUID with the specified value.</returns>
            /// <exception cref="ArgumentNullException">input is null.</exception>
            /// <exception cref="FormatException">input is not in a recognized format.</exception>
            public static Uuid Parse(string input)
            {
                return Guid.Parse(input);
            }

            public static Uuid NewUuid()
            {
                return Guid.NewGuid();
            }

            private static String GetDigits(long val, int digits)
            {
                long hi = 1L << (digits * 4);
                return String.Format("{0:X}", (hi | (val & (hi - 1)))).Substring(1);
            }
        }

        // Converts Object to byte array
        public static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        // Converts byte array to Object
        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
        }

        // Converts Json string to byte array
        public static byte[] JsonToByteArray(string filename)
        {
            return File.ReadAllBytes(filename);
        }

        // Transforma documento em formato cbor em mapa <Strong,Object>
        public static Dictionary<string, Object> ReadDoc(byte[] json)
        {
            
            var reader = new StreamReader(new MemoryStream(json), Encoding.Default);
            Dictionary<string, Object> values = new Newtonsoft.Json.JsonSerializer().Deserialize<Dictionary<String, Object>>(new JsonTextReader(reader));

            return values;

        }

        // Prints byte array values
        public static void PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            Console.WriteLine(sb.ToString());
        }
        

        // Checks the encoding of string
        public static bool CheckEncoding(string value, Encoding encoding)
        {
            bool retCode;
            var charArray = value.ToCharArray();
            byte[] bytes = new byte[charArray.Length];
            for (int i = 0; i < charArray.Length; i++)
            {
                bytes[i] = (byte)charArray[i];
            }
            retCode = string.Equals(encoding.GetString(bytes, 0, bytes.Length), value, StringComparison.InvariantCulture);
            return retCode;
        }

        // Decodes byte array to CBOR Object
        public static CBORObject BytesToCBOR(byte[] data)
        {
            return CBORObject.DecodeFromBytes(data);
        }

        // Encodes CBOR Object to byte array
        public static byte[] CBORToBytes(CBORObject data)
        {
            return data.EncodeToBytes();
        }

        // Returns Reader's public key from the SessionEstablishment structure 
        public static byte[] SessionEstablishment_ReaderKey(byte[] sessionEstablishment)
        {
            CBORObject sessionEstablishmentObj = CBORObject.DecodeFromBytes(sessionEstablishment);
            return sessionEstablishmentObj["eReaderKey"].GetByteString();
        }

        // Returns Reader's mDoc request from the SessionEstablishment structure
        public static CBORObject SessionEstablishment_Request(byte[] sessionEstablishment)
        {
            CBORObject sessionEstablishmentObj = CBORObject.DecodeFromBytes(sessionEstablishment);

            // mDoc request in CBOR bytes
            byte[] request_cborbytes = sessionEstablishmentObj["data"].GetByteString();
            // mDoc request in CBOR Object
            return CBORObject.DecodeFromBytes(request_cborbytes);
        }

        public static byte[] ReadAllBytes(this Stream instream)
        {
            if (instream is MemoryStream)
                return ((MemoryStream)instream).ToArray();

            using (var memoryStream = new MemoryStream())
            {
                instream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static CBORObject ReadDocument(byte[] document)
        {
            byte[] test_json1 = document;
            string jsonStr = Encoding.UTF8.GetString(test_json1);
            var reader = new StreamReader(new MemoryStream(test_json1), Encoding.Default);

            CBORObject objects = CBORObject.ReadJSON(new MemoryStream(test_json1));

            return objects;
        }

    }
    
}