using csv2kml.CameraDirection;

namespace csv2kml
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class Context
    {
        public int Subdivision { get; set; }
        public CsvConfig CsvConfig { get; set; }
        public TourConfig TourConfig { get; set; }
        public Data[] Data { get; set; }
        public Segment[] Segments { get; set; }
        public double AltitudeOffset { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
