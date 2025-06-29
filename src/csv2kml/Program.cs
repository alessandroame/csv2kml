// See https://aka.ms/new-console-template for more information
using CommandLine;
using csv2kml;
using System.Text.Unicode;

internal class Program
{

    private static void Main(string[] args)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;    
            CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                if (o.Verbose)
                    Console.WriteLine($"Verbose output enabled.");
                if (string.IsNullOrEmpty(o.TelemetryFN) && string.IsNullOrEmpty(o.TelemetryFolder))
                    throw new Exception("inputFile or inputFolder required");


                var files = new List<string>();
                if (!string.IsNullOrEmpty(o.TelemetryFN)) files.Add(o.TelemetryFN);
                if (!string.IsNullOrEmpty(o.TelemetryFolder)) files.AddRange(Directory.GetFiles(o.TelemetryFolder, "*.csv"));

                if (string.IsNullOrEmpty(o.CsvConfigFN)) throw new ArgumentException("CsvConfigFN");
                if (string.IsNullOrEmpty(o.TourConfigFN)) throw new ArgumentException("TourConfigFN");
                var success = new List<string>();
                var errors = new List<string>();
                foreach (var csv in files)
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"Importing {Path.GetFullPath(csv)}...");
                        Console.ForegroundColor = ConsoleColor.DarkGray;

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
                        .Build(csv, o.AltitudeOffset)
                        .Save(outFn);
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"Created {Path.GetFullPath(outFn)}\r\n");
                        success.Add($"{Path.GetFileName(csv)} >> {Path.GetFullPath(outFn)}");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"FAILED to convert {csv}\r\n{ex}\r\n\r\n");
                        errors.Add($"FAILED to convert {csv}\r\n\t--> {ex.Message}");
                    }
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(" SUMMARY ");
                Console.WriteLine("---------");
                foreach (var msg in success)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(msg);
                }
                if (errors.Count > 0) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\r\n ERRORS");
                    Console.WriteLine("---------");
                }
                foreach (var msg in errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(msg);
                }

                Console.WriteLine("\r\n\r\n\r\n\r\n");

            });
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
        }
        Console.ForegroundColor = ConsoleColor.Gray;
    }

}

