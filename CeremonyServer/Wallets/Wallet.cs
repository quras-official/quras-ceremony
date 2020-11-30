using Ceremony.Core;
using Ceremony.Cryptography;
using Ceremony.Cryptography.ECC;
using Ceremony.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ceremony.Wallets
{

    public class Wallet
    {
        private KeyPair walletKey;

        public static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                {
                    // Fille the buffer with the generated data
                    rng.GetBytes(data);
                }
            }

            return data;
        }

        public static KeyPair CreateKey()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = CreateKey(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return key;
        }
        public static KeyPair CreateKey(byte[] privateKey, KeyType nVersion = KeyType.Transparent)
        {
            KeyPair key = new KeyPair(privateKey, nVersion);
            return key;
        }
        public void SetKeyPair(KeyPair newKey)
        {
            walletKey = newKey;
        }

        public ECPoint GetPublicKey()
        {
            return walletKey.PublicKey;
        }

        public string GetAddress()
        {
            return ToAddress(walletKey.PublicKeyHash);
        }

        public static string ToAddress(UInt160 scriptHash)
        {
            byte[] data = new byte[21];
            data[0] = 31;
            Buffer.BlockCopy(scriptHash.ToArray(), 0, data, 1, 20);
            return data.Base58CheckEncode();
        }
    }
}
