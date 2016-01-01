using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Projeny.Internal
{
    public static class MiscUtil
    {
        public static string ConvertByteSizeToDisplayValue(long bytesLong)
        {
            Decimal kilobytes = Convert.ToDecimal(bytesLong) / 1024.0m;

            if (kilobytes < 1024.0m)
            {
                return string.Format("{0:0} kB", kilobytes);
            }

            Decimal megabytes = Convert.ToDecimal(kilobytes) / 1024.0m;

            if (megabytes < 1024.0m)
            {
                return string.Format("{0:0} MB", megabytes);
            }

            Decimal gigabytes = Convert.ToDecimal(megabytes) / 1024.0m;

            return string.Format("{0:0} GB", gigabytes);
        }

        public static string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }
    }
}

