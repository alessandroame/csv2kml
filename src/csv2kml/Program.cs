// See https://aka.ms/new-console-template for more information
using Csv;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System.ComponentModel.Design.Serialization;
using Csv2KML;
using csv2kml;

//var csvPath = @"../../../../../samples/_PRESTIGE-2pK-2024-01-04-13-36-02.csv";
//var csvPath = @"D:\Github\csv2kml\samples\20240119\_PRESTIGE-2pK-2024-01-19-15-05-51.csv";
//var csvPath = @"../../../../../samples/_PRESTIGE-2pK-2024-01-04-14-20-00.csv";
var files = Directory.GetFiles(@"D:\Github\csv2kml\samples\20240120","*.csv");
foreach (var f in files)
{
    Console.WriteLine($"\r\n\r\nConverting {f}");
    var data = LoadFromFRSKYTelemetry(f);
    PrintStats(data);
    var generator = new KmlGenerator(data, Path.GetFileNameWithoutExtension(f));
    if (!generator.SaveTo(Path.ChangeExtension(f, "kml"), out var errors))
    {
        Console.WriteLine(errors);
    }
}
Data[] LoadFromFRSKYTelemetry(string path)
{
    var res = new List<Data>();
    var fs = new FileStream(path, FileMode.Open);

    var latIndex = 18;
    var lonIndex = 19;
    var altIndex = 20;
    var speedIndex = 21;
    var vSpeedIndex = 8;
    var gpsClockIndex = 22;

    var lastTime = DateTime.MinValue;
    double lastLat = 0;
    double lastLon = 0;
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

            if (lastTime == time || (lastLat== lat && lastLon==lon)) continue;

            var data = new Data(time, lat, lon, alt, gpsSpeed, vSpeed);
            res.Add(data);
            lastTime = time;
            lastLat=lat; 
            lastLon = lon;

            //if (res.Count > 100) break;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
