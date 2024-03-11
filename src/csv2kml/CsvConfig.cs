using Newtonsoft.Json;

namespace csv2kml
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class CsvConfig
    {

        [JsonProperty("indexes")]
        public CSVFieldsByIndex FieldsByIndex { get; set; }
        [JsonProperty("titles")]
        public CSVFieldsByTitle FieldsByTitle { get; set; }
        public double ValueMin { get; set; }
        public double ValueMax { get; set; }
        public double ColorScaleExpo { get; set; }

        public static CsvConfig FromFile(string filename)
        {
            var json = File.ReadAllText(filename);
            var res = JsonConvert.DeserializeObject<CsvConfig>(json);
            if (res == null) throw new Exception($"Failed to read TourConfig from {filename}");
            return res;

        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public class CSVFieldsByIndex
    {
        public int Timestamp { get; set; }
        public int Latitude { get; set; }
        public int Longitude { get; set; }
        public int Altitude { get; set; }
        public int VerticalSpeed { get; set; }
        public string Speed { get; set; }
        public int Motor { get; set; }
    }
    public class CSVFieldsByTitle
    {
        public string Timestamp { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Altitude { get; set; }
        public string VerticalSpeed { get; set; }
        public string Speed { get; set; }
        public string Motor { get; set; }

    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

}
