using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csv2kml
{
    public static class NumberExtensions
    {

        public static Color ToColor(this double normalizedValue)
        {
            //0 must be green
            //-1 must be blue
            //1 must be red
            var hue = normalizedValue*-1 *120 + 120;
            return hue.HueToRGB();
        }
        public static Color HueToRGB(this double hue)
        {
            //if (hue > 400) Debugger.Break();

            if (hue < 0) hue = hue % 360 + 360;
            if (hue > 360) hue = hue % 360;
            double red, green, blue;

            if (hue < 60)
            {
                red = 1;
                green = hue / 60d;
                blue = 0;
            }
            else if (hue < 120)
            {
                red = 1 - (hue - 60) / 60d;
                green = 1;
                blue = 0;
            }
            else if (hue < 180)
            {
                red = 0;
                green = 1;
                blue = (hue - 120) / 60d;
            }
            else if (hue < 240)
            {
                red = 0;
                green = 1 - (hue - 180) / 60d;
                blue = 1;
            }
            else if (hue < 300)
            {
                red = (hue - 240) / 60d;
                green = 0;
                blue = 1;
            }
            else
            {
                red = 1;
                green = 0;
                blue = 1 - (hue - 300) / 60d;
            }

            return Color.FromArgb(255, (int)(red * 255), (int)(green * 255), (int)(blue * 255));
        }

        public static double Normalize(this double value, double max)
        {
            if (value==0) return 0;
            var res = value / max;
            if (res > 1) res = 1;
            else if (res < -1) res = -1;
            return res;
        }
        public static double ApplyExpo(this double v,double k)
        {
            double res = v;
            if (k >= 0)
            {
                res = k * Math.Pow(v, 3) + (1 - k) * v;
            }
            else
            {
                res = (-k) * (Math.Pow(Math.Abs(v), 1d / 3) * Math.Sign(v)) + (1 + k) * v;
            }
            return res;
        }
    }
}

