using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
