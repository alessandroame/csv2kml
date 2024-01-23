// See https://aka.ms/new-console-template for more information
using Csv;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.ComponentModel.Design.Serialization;
using Csv2KML;
using csv2kml;
using MathNet.Numerics.Interpolation;
using CommandLine.Text;
using CommandLine;
using Newtonsoft.Json;
using System.Drawing.Printing;

internal class Program
{

    private static void Main(string[] args)
    {
        try
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                if (o.Verbose)
                    Console.WriteLine($"Verbose output enabled.");
                if (string.IsNullOrEmpty(o.TelemetryFN) && string.IsNullOrEmpty(o.TelemetryFolder))
                    throw new Exception("inputFile or inputFolder required");
                if (o.Verbose) Console.WriteLine($"CsvSettings.FromFile {o.CsvSettingsFN}");
                var csvSettings = CsvSettings.FromFile(o.CsvSettingsFN);
                if (o.Verbose) Console.WriteLine($"TourSettings.FromFile {o.TourSettingsFN}");
                var tourSettings = TourSettings.FromFile(o.TourSettingsFN);

                var files = new List<string>();
                if (!string.IsNullOrEmpty(o.TelemetryFN)) files.Add(o.TelemetryFN);
                foreach (var file in Directory.GetFiles(o.TelemetryFolder, "*.csv"))
                {
                    files.Add(file);
                }

                for (var i = 0; i < files.Count(); i++)
                {
                    var fn = files[i];
                    Convert(fn, csvSettings, tourSettings, o.AltitudeOffset, o.KMLFolder);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static void Convert(string fn, CsvSettings csvSettings, TourSettings tourSettings,int altitudeOffset, string kMLFolder)
    {
        Console.WriteLine("----------------------------");
        Console.WriteLine($"Converting {fn}...");
        
        var data = LoadData(fn, csvSettings);
        PrintStats(data);
        string outFn;
        if (string.IsNullOrEmpty(kMLFolder))
        {
            outFn = Path.ChangeExtension(fn,"kml"); 
        }
        else
        {
            outFn = Path.Combine(kMLFolder, Path.GetFileNameWithoutExtension(fn) + ".kml");
        }
        var generator = new KmlGenerator(data, Path.GetFileNameWithoutExtension(fn), SharpKml.Dom.AltitudeMode.Absolute, altitudeOffset, tourSettings);
        if (!generator.SaveTo(outFn, out var errors))
        {
            Console.WriteLine(errors);
        }
    }

    private static void PrintStats(Data[] data)
    {
        Console.WriteLine($"Max altitude min {data.Max(d => d.Altitude)}m max { data.Max(d => d.Altitude)}m");
        Console.WriteLine($"Vertical speed max  {data.Max(d => d.VSpeed)} m/s  min  {data.Min(d => d.VSpeed)} m/s avg {Math.Round(data.Average(d => d.VSpeed))}m/s");
        Console.WriteLine($"Speed max {data.Max(d => d.GPSSpeed)}km/h  min {data.Min(d => d.GPSSpeed)}km/h avg{Math.Round(data.Average(d => d.GPSSpeed))}km/h ");
    }

    private static Data[] LoadData(string path, CsvSettings csvSettings)
    {
        var res = new List<Data>();
        var fs = new FileStream(path, FileMode.Open);

        var lastTime = DateTime.MinValue;
        double lastLat = 0;
        double lastLon = 0;
        foreach (var line in CsvReader.ReadFromStream(fs, new CsvOptions { HeaderMode = HeaderMode.HeaderPresent }))
        {
            try
            {
                if (!line[csvSettings.LatitudeIndex].TryParseDouble(out var lat)) continue;
                if (!line[csvSettings.LongitudeIndex].TryParseDouble(out var lon)) continue;
                if (!line[csvSettings.AltitudeIndex].TryParseDouble(out var alt)) continue;
                if (!line[csvSettings.SpeedIndex].TryParseDouble(out var gpsSpeed)) continue;
                if (!line[csvSettings.VerticalSpeedIndex].TryParseDouble(out var vSpeed)) continue;
                var timestamp = DateTime.Parse(line[csvSettings.TimestampIndex]);
                if (lastTime == timestamp || lastLat == lat && lastLon == lon) continue;
                //import
                var data = new Data(timestamp, lat, lon, alt, gpsSpeed, vSpeed);
                res.Add(data);
                lastTime = timestamp;
                lastLat = lat;
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
}
