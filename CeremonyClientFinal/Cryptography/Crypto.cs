using System;
using System.Linq;
using System.Security.Cryptography;

namespace Ceremony.Cryptography
{
    public class Crypto
    {
        public static readonly Crypto Default = new Crypto();

        public byte[] Hash160(byte[] message)
        {
            return message.Sha256().RIPEMD160();
        }

        public byte[] Hash256(byte[] message)
        {
            return message.Sha256().Sha256();
        }

    }
}
