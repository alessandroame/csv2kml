// See https://aka.ms/new-console-template for more information
using SharpKml.Dom.GX;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;

public class Data
{
    public Data(DateTime time, double lat, double lon, double alt, double valueToColorize, double motor)
    {
        Time = time;
        Latitude = lat;
        Longitude = lon;
        Altitude = alt;
        ValueToColorize = valueToColorize;
        Motor = motor;
    }

    public DateTime Time { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double ValueToColorize { get; set; }
    public double Motor { get; set; }
}
