using System.Security.Cryptography;
using System.Text;

namespace crypto
{
    public class Digest
    {
        public byte[] sha256(byte[] data)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(data);
                return bytes;
            }
        }

        public byte[] sha384(byte[] data)
        {
            using (SHA384 sha384Hash = SHA384.Create())
            {
                byte[] bytes = sha384Hash.ComputeHash(data);
                return bytes;
            }
        }

        public byte[] sha512(byte[] data)
        {
            using (SHA512 sha512Hash = SHA512.Create())
            {
                byte[] bytes = sha512Hash.ComputeHash(data);
                return bytes;
            }
        }
    }
}