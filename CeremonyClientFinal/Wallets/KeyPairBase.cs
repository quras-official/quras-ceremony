﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Ceremony.Wallets
{
    public abstract class KeyPairBase
    {
        public KeyType nVersion;
        public UInt160 PublicKeyHash;
    }
}
