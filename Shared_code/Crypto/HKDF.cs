using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace crypto
{
    public static class HKDF
    {
        static Func<byte[], byte[], byte[]> keyedHash;
        static HKDF()
        {
            var hmac = new HMACSHA256();
            keyedHash = (key, message) =>
            {
                hmac.Key = key;
                return hmac.ComputeHash(message);
            };
        }

        public static byte[] Extract(byte[] salt, byte[] inputKeyMaterial)
        {
            return keyedHash(salt, inputKeyMaterial);
        }

        public static byte[] Expand(byte[] prk, byte[] info, int outputLength)
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

        // Usar para derivar device session key
        public static byte[] DeriveKey(byte[] salt, byte[] inputKeyMaterial, byte[] info, int outputLength)
        {
            var prk = Extract(salt, inputKeyMaterial);
            var result = Expand(prk, info, outputLength);
            return result;
        }

        public static byte[] Add(this byte[] A, byte[] B)
        {
            List<byte> array = new List<byte>(A);
            for (int i = 0; i < B.Length; i++)
                array = _add_(array, B[i], i);

            return array.ToArray();
        }
        private static List<byte> _add_(List<byte> A, byte b, int idx = 0, byte rem = 0)
        {
            short sample = 0;
            if (idx < A.Count)
            {
                sample = (short)((short)A[idx] + (short)b);
                A[idx] = (byte)(sample % 256);
                rem = (byte)((sample - A[idx]) % 255);
                if (rem > 0)
                    return _add_(A, (byte)rem, idx + 1);
            }
            else A.Add(b);

            return A;
        }

        public static byte[] Multiply(this byte[] A, byte[] B)
        {
            List<byte> ans = new List<byte>();

            byte ov, res;
            int idx = 0;
            for (int i = 0; i < A.Length; i++)
            {
                ov = 0;
                for (int j = 0; j < B.Length; j++)
                {
                    short result = (short)(A[i] * B[j] + ov);

                    // get overflow (high order byte)
                    ov = (byte)(result >> 8);
                    res = (byte)result;
                    idx = i + j;

                    // apply result to answer array
                    if (idx < (ans.Count))
                        ans = _add_(ans, res, idx);
                    else ans.Add(res);
                }
                // apply remainder, if any
                if (ov > 0)
                    if (idx + 1 < (ans.Count))
                        ans = _add_(ans, ov, idx + 1);
                    else ans.Add(ov);
            }

            return ans.ToArray();
        }

        private static int IntPow(int x, uint pow)
        {
            int ret = 1;
            while (pow != 0)
            {
                if ((pow & 1) == 1)
                    ret *= x;
                x *= x;
                pow >>= 1;
            }
            return ret;
        }

        
    }
}