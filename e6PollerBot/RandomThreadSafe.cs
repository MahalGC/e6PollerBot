using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace e6PollerBot
{
    public static class RandomThreadSafe
    {
        private static RNGCryptoServiceProvider _global =
            new RNGCryptoServiceProvider();
        [ThreadStatic]
        private static Random _local;

        public static int Next(int minValue, int maxValue)
        {
            Random inst = _local;
            if (inst == null)
            {
                byte[] buffer = new byte[4];
                _global.GetBytes(buffer);
                _local = inst = new Random(
                    BitConverter.ToInt32(buffer, 0));
            }
            return inst.Next(minValue, maxValue);
        }
    }
}
