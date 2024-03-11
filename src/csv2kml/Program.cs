// See https://aka.ms/new-console-template for more information
using CommandLine;
using csv2kml;

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
                if (!string.IsNullOrEmpty(o.TelemetryFolder)) files.AddRange(Directory.GetFiles(o.TelemetryFolder, "*.csv"));

                if (string.IsNullOrEmpty(o.CsvConfigFN)) throw new ArgumentException("CsvConfigFN");
                if (string.IsNullOrEmpty(o.TourConfigFN)) throw new ArgumentException("TourConfigFN");
                foreach (var csv in files)
                {
                    Console.WriteLine($"Importing {csv}...");
                    string outFn;
                    if (string.IsNullOrEmpty(o.KMLFolder))
                    {
                        outFn = Path.ChangeExtension(csv, "kml");
                    }
                    else
                    {
                        outFn = Path.Combine(o.KMLFolder, Path.GetFileNameWithoutExtension(csv) + ".kml");
                    }

                    //for (int i =-10;i<=10;i++)
                    //{
                    //    var n = ((double)i).Normalize(10);
                    //    Console.WriteLine($"{i}\t{n}" +
                    //        $"\t{n.ApplyExpo(1)}" +
                    //        $"\t{n.ApplyExpo(.5)}" +
                    //        $"\t{n.ApplyExpo(-1)}");
                    //    //var v = n.ToNonLinearScale();
                    //    //var color = v.ToColor();
                    //    //Console.WriteLine($"{i}\t{n}\t{v}\t{color}");
                    //}

                    new KmlBuilder()
                    .UseCsvConfig(o.CsvConfigFN)
                    .UseTourConfig(o.TourConfigFN)
                    .Build(csv, o.AltitudeOffset)
                    .Save(outFn);
                    var oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"Created {outFn}\r\n");
                    Console.ForegroundColor = oldColor;

                }
            });
        }
        catch (Exception ex)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ForegroundColor = oldColor;
        }
    }

}
