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
    }
}
