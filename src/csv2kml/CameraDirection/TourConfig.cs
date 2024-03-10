using Newtonsoft.Json;
using SharpKml.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace csv2kml.CameraDirection
{
    public class TourConfig
    {
        [JsonProperty("lookAtCamera")]
        public LookAtCameraConfig[]? LookAtCameraSettings { get; set; }

        public static TourConfig FromFile(string filename)
        {
            var json = File.ReadAllText(filename);
            var res = JsonConvert.DeserializeObject<TourConfig>(json);
            if (res == null) throw new Exception($"Failed to read TourConfig from {filename}");
            return res;
        }
    }

    public enum PointReference
    {
        CurrentPoint,
        PreviousPoint,
        LastVisiblePoint,
        BoundingBoxCenter,
        CurrentBoundingBoxCenter,
        PilotPosition
    }

    public class LookAtCameraConfig
    {
        public string Name { get; set; } = "Tour";
        public int VisibleHistorySeconds { get; set; }
        public int? Tilt { get; set; }
        public int PanOffset { get; set; }
        public int UpdatePositionIntervalInSeconds { get; set; }
        public int MinimumRangeInMeters { get; set; }
        public int MaxDeltaHeadingDegrees { get; set; }
        public PointReference LookAt { get; set; }
        public PointReference AlignTo { get; set; }

    }
}
