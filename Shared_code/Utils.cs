using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace utils
{
    public abstract class Utils
    {
        public byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
        }
        // "C:\Users\Hugo\OneDrive\Ambiente de Trabalho\test.json"
        public static byte[] JsonToByteArray(string filename)
        {
            return File.ReadAllBytes(filename);
        }
    }
    
}