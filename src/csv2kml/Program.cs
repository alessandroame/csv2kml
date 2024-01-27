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

           
            var files = new List<string>();
                if (!string.IsNullOrEmpty(o.TelemetryFN)) files.Add(o.TelemetryFN);
                foreach (var csv in Directory.GetFiles(o.TelemetryFolder, "*.csv"))
                {
                    string outFn;
                    if (string.IsNullOrEmpty(o.KMLFolder))
                    {
                        outFn = Path.ChangeExtension(csv, "kml");
                    }
                    else
                    {
                        outFn = Path.Combine(o.KMLFolder, Path.GetFileNameWithoutExtension(csv) + ".kml");
                    }

                    new KmlBuilder()
                    .UseCsvConfig(o.CsvConfigFN)
                    .UseTourConfig(o.TourConfigFN)
                    .Build(csv)
                    .Save(outFn);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

}
