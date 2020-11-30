using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace Ceremony.Cryptography.ECC
{
    /// <summary>
    /// Provide the function of Elliptic Curve Digital Signature Algorithms (ECDSA)
    /// </summary>
    public class ECDsa
    {
        private readonly byte[] privateKey;
        private readonly ECPoint publicKey;
        private readonly ECCurve curve;

        /// <summary>
        /// Create a new ECDsa object based on the specified private key and curve parameters, which can be used for signature
        /// </summary>
        /// <param name="privateKey">private key</param>
        /// <param name="curve">Elliptic Curve Parameters</param>
        public ECDsa(byte[] privateKey, ECCurve curve)
            : this(curve.G * privateKey)
        {
            this.privateKey = privateKey;
        }

        /// <summary>
        /// Create a new ECDsa object based on the specified public key, which can be used to verify signatures
        /// </summary>
        /// <param name="publicKey">public key</param>
        public ECDsa(ECPoint publicKey)
        {
            this.publicKey = publicKey;
            this.curve = publicKey.Curve;
        }

        private BigInteger CalculateE(BigInteger n, byte[] message)
        {
            int messageBitLength = message.Length * 8;
            BigInteger trunc = new BigInteger(message.Reverse().Concat(new byte[1]).ToArray());
            if (n.GetBitLength() < messageBitLength)
            {
                trunc >>= messageBitLength - n.GetBitLength();
            }
            return trunc;
        }

        /// <summary>
        /// Generating Elliptic Curve Digital Signature
        /// </summary>
        /// <param name="message">Messages to Sign</param>
        /// <returns>Digital encoding for returning signatures（r,s）</returns>

        private static ECPoint SumOfTwoMultiplies(ECPoint P, BigInteger k, ECPoint Q, BigInteger l)
        {
            int m = Math.Max(k.GetBitLength(), l.GetBitLength());
            ECPoint Z = P + Q;
            ECPoint R = P.Curve.Infinity;
            for (int i = m - 1; i >= 0; --i)
            {
                R = R.Twice();
                if (k.TestBit(i))
                {
                    if (l.TestBit(i))
                        R = R + Z;
                    else
                        R = R + P;
                }
                else
                {
                    if (l.TestBit(i))
                        R = R + Q;
                }
            }
            return R;
        }

        /// <summary>
        /// Verify the validity of signature
        /// </summary>
        /// <param name="message">Messages to verify</param>
        /// <param name="r">Digital Encoding of Signature</param>
        /// <param name="s">Digital Encoding of Signature</param>
        /// <returns>Returns the result of validation</returns>
        public bool VerifySignature(byte[] message, BigInteger r, BigInteger s)
        {
            if (r.Sign < 1 || s.Sign < 1 || r.CompareTo(curve.N) >= 0 || s.CompareTo(curve.N) >= 0)
                return false;
            BigInteger e = CalculateE(curve.N, message);
            BigInteger c = s.ModInverse(curve.N);
            BigInteger u1 = (e * c).Mod(curve.N);
            BigInteger u2 = (r * c).Mod(curve.N);
            ECPoint point = SumOfTwoMultiplies(curve.G, u1, publicKey, u2);
            BigInteger v = point.X.Value.Mod(curve.N);
            return v.Equals(r);
        }
    }
}
