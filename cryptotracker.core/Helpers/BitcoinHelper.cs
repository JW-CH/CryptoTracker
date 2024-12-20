using NBitcoin.DataEncoders;

namespace cryptotracker.core.Helpers
{
    public static class BitcoinHelper
    {
        public static decimal GetBitcoinFromSats(decimal sats)
        {
            return sats / 100000000;
        }
        public static decimal GetSatsFromBitcoin(decimal bitcoin)
        {
            return bitcoin * 100000000;
        }

        // Methode zur Umwandlung von zpub zu xpub
        public static string ZpubToXpub(string zpub)
        {
            // Dekodieren der ZPUB in rohe Bytes
            var decoded = Encoders.Base58Check.DecodeData(zpub);

            // Erstellen des neuen XPUB-Arrays, wobei wir das Präfix von ZPUB (0x04B24746) durch das XPUB-Präfix (0x0488B21E) ersetzen
            byte[] xpubBytes = new byte[decoded.Length];

            // Kopieren der Daten
            Array.Copy(decoded, 0, xpubBytes, 0, decoded.Length);

            // Präfix für XPUB
            byte[] xpubPrefix = new byte[] { 0x04, 0x88, 0xB2, 0x1E }; // 0x0488B21E für XPUB

            // Ersetzen des Präfixes in den ersten 4 Bytes
            Array.Copy(xpubPrefix, 0, xpubBytes, 0, 4);

            // Zurückkodieren in Base58Check und Rückgabe der XPUB
            return Encoders.Base58Check.EncodeData(xpubBytes);
        }
    }
}
