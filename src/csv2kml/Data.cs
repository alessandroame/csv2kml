// See https://aka.ms/new-console-template for more information
using csv2kml;
using SharpKml.Dom.Atom;
using SharpKml.Dom.GX;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;

public class Data
{
    public Data(int index,DateTime time, double lat, double lon, double alt, double vspeed, double speed, bool motorActive)
    {
        Index = index;
        Time = time;
        Latitude = lat;
        Longitude = lon;
        Altitude = alt;
        VerticalSpeed = vspeed;
        Speed = speed;
        MotorActive = motorActive;
    }

    public override string ToString()
    {
        return $"#{Index} alt {Altitude}m";
    }

    public int Index { get; private set; }
    public DateTime Time { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double VerticalSpeed { get; set; }
    public double Speed { get; set; }
    public bool MotorActive { get; set; }
    public FlightPhase FlightPhase { get; set; }
    public double TotalEnergy { get; set; }
    public double DeltaEnergy { get; set; }
    public double EnergyVerticalSpeed { get; set; }
}
