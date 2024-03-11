// See https://aka.ms/new-console-template for more information
using csv2kml;
using SharpKml.Base;

public static partial class DataExtensions
{
    public class BoundingBoxEx
    {
        public double West { get; }
        public double East { get; }
        public double North { get; }
        public double South { get; }
        public double Bottom { get; }
        public double Top { get; }

        public Vector Center
        {
            get
            {
                double latitude = (North + South) / 2.0;
                double longitude = (East + West) / 2.0;
                double altitude = (Top + Bottom) / 2.0;
                return new Vector(latitude, longitude, altitude);
            }
        }

        public double DiagonalSize { get; internal set; }
        public double GroundDiagonalSize { get; }

        public BoundingBoxEx(IEnumerable<Data> data)
        {
            West = data.Min(d => d.Longitude);
            East = data.Max(d => d.Longitude);
            North = data.Min(d => d.Latitude);
            South = data.Max(d => d.Latitude);
            Bottom = data.Min(d => d.Altitude);
            Top = data.Max(d => d.Altitude);

            var edge1 = new Vector(North, West, Bottom);
            var edge2 = new Vector(South, East, Top);
            DiagonalSize = edge1.Distance(edge2);

            edge1.Altitude = edge2.Altitude = 0;
            GroundDiagonalSize = edge1.Distance(edge2);
        }

    }
}