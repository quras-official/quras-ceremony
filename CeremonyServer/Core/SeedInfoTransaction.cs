using Ceremony.Cryptography.ECC;
using Ceremony.IO.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ceremony.Core
{
    public class SeedInfoTransaction
    {
        public ECPoint Signature1;
        public ECPoint Signature2;
        public ECPoint SenderPubKey;

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["signature1"] = Signature1.ToString();
            json["signature2"] = Signature2.ToString();
            json["pubkey"] = SenderPubKey.ToString();
            return json;
        }

        public bool FromJson(JObject json)
        {
            try
            {
                Signature1 = ECPoint.Parse(json["signature1"].AsString(), ECCurve.Secp256r1);
                Signature2 = ECPoint.Parse(json["signature2"].AsString(), ECCurve.Secp256r1);
                SenderPubKey = ECPoint.Parse(json["pubkey"].AsString(), ECCurve.Secp256r1);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void CreateSeedSignature(byte[] privateKey, ECPoint serverPubkey, ECPoint XORSeed, ECPoint MySeed)
        {
            Signature1 = serverPubkey * privateKey + XORSeed;
            Signature2 = serverPubkey * privateKey + MySeed;
        }

        public ECPoint GetXORSeed(byte[] privateKey, ECPoint senderPubKey)
        {
            return Signature1 - senderPubKey * privateKey;
        }

        public ECPoint GetTxSeed(byte[] privateKey, ECPoint senderPubKey)
        {
            return Signature2 - senderPubKey * privateKey;
        }
    }
}
