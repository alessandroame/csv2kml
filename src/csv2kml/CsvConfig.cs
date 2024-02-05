using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace csv2kml
{
    public class CsvConfig
    {

        [JsonProperty("indexes")]
        public CSVFieldsByIndex FieldsByIndex {  get; set; }
        [JsonProperty("titles")]
        public CSVFieldsByTitle FieldsByTitle {  get; set; }
        public double ValueMin { get; set; }
        public double ValueMax { get; set; }
        public double ColorScaleExpo { get; set;}

        public static CsvConfig? FromFile(string filename)
        {
            var json = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<CsvConfig>(json);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this,Formatting.Indented);
        }
    }

    public class CSVFieldsByIndex
    {
        public int Timestamp { get; set; }
        public int Latitude { get; set; }
        public int Longitude { get; set; }
        public int Altitude { get; set; }
        public int ValueToColorize { get; set; }
        public int Motor { get; set; }
    }
    public class CSVFieldsByTitle
    {
        public string Timestamp { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Altitude { get; set; }
        public string ValueToColorize { get; set; }
        public string Motor { get; set; }
    }
}
