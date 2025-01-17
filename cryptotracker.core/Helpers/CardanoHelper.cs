using System.Numerics;

namespace cryptotracker.core.Helpers
{
    public static class CardanoHelper
    {
        public static (byte[] version, byte depth, byte[] parentFinterprint, byte[] childNumber, byte[] chaincode, byte[] publicKey) GetByteStuff(string xpub)
        {
            var decodedXpub = Base58Decode(xpub);

            var versionBytes = decodedXpub.Take(4).ToArray();
            var depth = decodedXpub[4];
            var parentFingerprint = decodedXpub.Skip(5).Take(4).ToArray();
            var childNumber = decodedXpub.Skip(9).Take(4).ToArray();
            var chainCode = decodedXpub.Skip(13).Take(32).ToArray();
            var publicKey = decodedXpub.Skip(45).Take(33).ToArray();

            return (versionBytes, depth, parentFingerprint, childNumber, chainCode, publicKey);
        }
        public static byte[] Base58Decode(string input)
        {
            const string ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            var base58 = ALPHABET.ToCharArray();
            var base58Map = base58.Select((c, i) => new { c, i }).ToDictionary(x => x.c, x => x.i);

            BigInteger intData = 0;
            foreach (char c in input)
            {
                if (!base58Map.ContainsKey(c))
                    throw new FormatException($"Invalid Base58 character `{c}`.");
                intData = intData * 58 + base58Map[c];
            }

            // Convert to byte array
            byte[] bytes = intData.ToByteArray(isUnsigned: true, isBigEndian: true);

            // Remove leading zeroes
            int leadingZeroCount = input.TakeWhile(c => c == '1').Count();
            var result = new byte[leadingZeroCount + bytes.Length];
            Array.Copy(bytes, 0, result, leadingZeroCount, bytes.Length);

            return result;
        }
    }
}
