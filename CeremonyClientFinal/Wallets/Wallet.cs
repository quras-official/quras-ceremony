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

        public void SaveWallet(string walletName,string walletPassword)
        {
            //create output file name
            FileStream fsCrypt = new FileStream(walletName + ".aes", FileMode.Create);
            byte[] passwordBytes = { };
            string password_content = walletPassword;
            while (passwordBytes.Length < 32)
            {
                passwordBytes = System.Text.Encoding.UTF8.GetBytes(password_content);
                password_content = passwordBytes.ToHexString();
            }
            
            byte[] content = walletKey.PrivateKey.AES256Encrypt(passwordBytes.Take(16).ToArray());

            fsCrypt.Write(content, 0, content.Length);

            fsCrypt.Close();
        }
        
        public void ReadWallet(string walletName,string walletPassword)
        {
            byte[] passwordBytes = { };
            string password_content = walletPassword;
            while (passwordBytes.Length < 32)
            {
                passwordBytes = System.Text.Encoding.UTF8.GetBytes(password_content);
                password_content = passwordBytes.ToHexString();
            }

            FileStream fsCrypt = new FileStream(walletName+".aes", FileMode.Open);

            byte[] encryptedKey = new byte[fsCrypt.Length];
            fsCrypt.Read(encryptedKey, 0, (int)fsCrypt.Length);

            walletKey = new KeyPair(encryptedKey.AES256Decrypt(passwordBytes.Take(16).ToArray()));

            fsCrypt.Close();
        }

        public void SetKeyPair(KeyPair newKey)
        {
            walletKey = newKey;
        }

        public ECPoint GetPublicKey()
        {
            return walletKey.PublicKey;
        }

        public byte[] GetPrivateKey()
        {
            return walletKey.PrivateKey;
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
