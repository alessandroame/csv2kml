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
        public int TimestampIndex { get; set; }
        public int LatitudeIndex {get;set;}
        public int LongitudeIndex {get;set;}
        public int AltitudeIndex {get;set;}
        public int ValueToColorizeIndex { get; set; }
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
}
