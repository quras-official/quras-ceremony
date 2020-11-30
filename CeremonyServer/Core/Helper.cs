using Ceremony.Cryptography;
using Ceremony.Wallets;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Ceremony.Core
{
    public static class Helper
    {
        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Default.Hash160(script));
        }

        public static byte[] FixLength(this byte[] org)
        {
            byte[] ret = new byte[32];

            for (int i = 0; i < 32; i++)
            {
                if (org.Length - 1 - i >= 0)
                {
                    ret[31 - i] = org[org.Length - 1 - i];
                }
            }

            return ret;
        }

        
    }
}