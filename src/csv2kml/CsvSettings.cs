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
    public class CsvSettings
    {
        public int TimestampIndex { get; set; }
        public int LatitudeIndex {get;set;}
        public int LongitudeIndex {get;set;}
        public int AltitudeIndex {get;set;}
        public int SpeedIndex {get;set;}
        public int VerticalSpeedIndex{get;set;}

        public static CsvSettings? FromFile(string filename)
        {
            var json= File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<CsvSettings>(json);
        }
    }
}
