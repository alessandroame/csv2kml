using System.Globalization;

namespace Csv2KML
{
    public static class StringExtensions
    {
        public static bool TryParseDouble(this string stringValue, out double value)
        {
            if (!double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                //Console.WriteLine($"{value} is not a valid double");
                return false;
            }
            return true;
        }

        public static bool TryParseLatLon(this string stringValue, out double lat, out double lon)
        {
            var parts=stringValue.Split(" ");
            lat = double.NaN;
            lon = double.NaN;

            if (!parts[0].TryParseDouble(out lat)) return false;
            if (!parts[1].TryParseDouble(out lon)) return false;
            return true;
        }
    }
}
