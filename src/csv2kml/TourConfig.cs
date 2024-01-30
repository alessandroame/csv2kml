using Newtonsoft.Json;
using SharpKml.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace csv2kml
{
    public class TourConfig
    {
        [JsonProperty("lookAtCamera")]
        public LookAtCameraSettings[] LookAtCameraSettings { get; set; }
        public AltitudeMode AltitudeMode { get; set; }
        public int AltitudeOffset { get; set; }

        public static TourConfig? FromFile(string filename)
        {
            var json = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<TourConfig>(json);
        }
    }

    public enum PointReference{
        CurrentPoint,
        PreviousPoint,
        LastVisiblePoint,
        BoundingBoxCenter
    }

    public class LookAtCameraSettings
    {
        public string Name { get; set; }
        public int VisibleHistorySeconds { get; set; }
        public int? Tilt { get; set; }
        public int PanOffset { get; set; }
        public int UpdatePositionFrameInterval { get; set; }

        public int MinimumRangeInMeters { get; set; }

        public PointReference LookAt { get; set; }
        public PointReference AlignTo { get; set; }

    }
}
