// See https://aka.ms/new-console-template for more information
using Csv;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System.ComponentModel.Design.Serialization;
using Csv2KML;
using csv2kml;

var csvPath = @"../../../../../samples/_PRESTIGE-2pK-2024-01-04-13-36-02.csv";
var data = LoadFromFRSKYTelemetry(csvPath);

PrintStats(data);
var generator=new KmlGenerator(data,Path.GetFileNameWithoutExtension(csvPath));
generator.GenerateColoredTrack("By climb");
if (!generator.SaveTo(Path.ChangeExtension(csvPath,"kml"),out var errors))
{
    Console.WriteLine(errors);
}

Data[] LoadFromFRSKYTelemetry(string path)
{
    var res = new List<Data>();
    var fs = new FileStream(csvPath, FileMode.Open);

    var latIndex = 18;
    var lonIndex = 19;
    var altIndex = 20;
    var speedIndex = 21;
    var vSpeedIndex = 8;
    var gpsClockIndex = 22;

    foreach (var line in CsvReader.ReadFromStream(fs, new CsvOptions { HeaderMode = HeaderMode.HeaderPresent }))
    {
        try
        {
            if (!line[latIndex].TryParseDouble(out var lat)) continue;
            if (!line[lonIndex].TryParseDouble(out var lon)) continue;
            if (!line[altIndex].TryParseDouble(out var alt)) continue;
            if (!line[speedIndex].TryParseDouble(out var gpsSpeed)) continue;
            if (!line[vSpeedIndex].TryParseDouble(out var vSpeed)) continue;
            var time = DateTime.Parse(line[gpsClockIndex]);

            var data = new Data(time, lat, lon, alt, gpsSpeed, vSpeed);
            res.Add(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
    return res.ToArray();
}

void PrintStats(Data[] data)
{
    Console.WriteLine($"Max altitude {data.Max(d => d.Altitude)} m");
    Console.WriteLine($"Speed max  {data.Max(d => d.GPSSpeed)}  m/s  min  {data.Min(d => d.VSpeed)}  m/s avg {data.Average(d => d.VSpeed)} m/s");
    Console.WriteLine($"Speed max {data.Max(d => d.GPSSpeed)} km/h  min {data.Min(d => d.GPSSpeed)} km/h avg{data.Average(d => d.GPSSpeed)} km/h ");
}
