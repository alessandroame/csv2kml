using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csv2kml
{
    public static class NumberExtensions
    {

        public static Color ToColor(this float normalizedValue)
        {
            var hue = (1-normalizedValue) * 240 ;
            return HueToRGB(hue);
        }
        public static Color HueToRGB(float hue)
        {
            while (hue < 0) hue += 360;
            while (hue > 360) hue -= 360;
            float red, green, blue;

            if (hue < 60)
            {
                red = 1;
                green = hue / 60f;
                blue = 0;
            }
            else if (hue < 120)
            {
                red = 1 - (hue - 60) / 60f;
                green = 1;
                blue = 0;
            }
            else if (hue < 180)
            {
                red = 0;
                green = 1;
                blue = (hue - 120) / 60f;
            }
            else if (hue < 240)
            {
                red = 0;
                green = 1 - (hue - 180) / 60f;
                blue = 1;
            }
            else if (hue < 300)
            {
                red = (hue - 240) / 60f;
                green = 0;
                blue = 1;
            }
            else
            {
                red = 1;
                green = 0;
                blue = 1 - (hue - 300) / 60f;
            }

            return Color.FromArgb(255, (int)(red * 255), (int)(green * 255), (int)(blue * 255));
        }

        public static float Normalize(this double value, double max)
        {
            var res = (float)(value / max);
            if (res > 1) res = 1;
            else if (res < -1) res = -1;
            return res;
        }
    }
}

