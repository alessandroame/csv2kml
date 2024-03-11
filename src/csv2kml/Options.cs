// See https://aka.ms/new-console-template for more information
using CommandLine;
using SharpKml.Dom.GX;

public class Options
{
    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
    public bool Verbose { get; set; }

    [Option('c', "csv", Required = true, HelpText = "csv config file path")]
    public string CsvConfigFN { get; set; }

    [Option('t', "tour", Required = true, HelpText = "tour config file path")]
    public string TourConfigFN { get; set; }

    [Option('i', "inputFile", Required = false, HelpText = "telemetry file path")]
    public string? TelemetryFN { get; set; }

    [Option('f', "inputFolder", Required = false, HelpText = "telemetry file folder")]
    public string? TelemetryFolder { get; set; }

    [Option('o', "output", Required = false, HelpText = "kml file folder")]
    public string? KMLFolder { get; set; }

    [Option('a', "altOffset", Required = true, HelpText = "Altitude offset")]
    public double AltitudeOffset { get; set; }


}
