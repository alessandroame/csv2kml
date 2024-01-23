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
    public class TourSettings
    {
        public int VisibleHistorySeconds { get; set; }
        [JsonProperty("lookAtCamera")] 
        public LookAtCameraSettings LookAtCameraSettings { get;set;}

        public static TourSettings? FromFile(string filename)
        {
            var json = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<TourSettings>(json);
        }
    }

    public class LookAtCameraSettings
    {
        public int MetersFromTrackPoint { get; set; }
        public int LookBackSeconds { get; set; }
        public int? Tilt { get; set; }
        public int PanOffset { get; set; }
        public int UpdatePositionFrameInterval { get; set; }
    }
}
