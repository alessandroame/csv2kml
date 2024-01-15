// See https://aka.ms/new-console-template for more information
using Csv;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System.ComponentModel.Design.Serialization;
using Csv2KML;

var csvPath = @"C:\Users\aless\Downloads\logs\_PRESTIGE-2pK-2024-01-04-13-36-02.csv";
var data = LoadFromFRSKYTelemetry(csvPath);

PrintStats(data);
Folder root = new Folder
{
    Name = Path.GetFileNameWithoutExtension(csvPath)
};
var colouredFolder = new Folder
{
    Name = "Coloured"
};
root.AddFeature(colouredFolder);
var tourTrack = new Track
{
    AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround
};
var tourPlacemark = new Placemark
{
    Name="Tour",
    Geometry = tourTrack
};
root.AddFeature(tourPlacemark);
var coords = new CoordinateCollection();
var oldVSpeed = 0;
for (var i=0;i<data.Length-1;i++)
{
    var item = data[i];
    var nextItem = data[i+1];
    var vSpeed = (int) Math.Min(20,Math.Max(-20,Math.Round((nextItem.Altitude-item.Altitude)*2 )));

    var v = new Vector(item.Latitude, item.Longitude, item.Altitude);
    tourTrack.AddCoordinate(v);
    tourTrack.AddWhen(item.Time);
    coords.Add(v);
    if (oldVSpeed != vSpeed)
    {
        var p = CreatePlacemark(coords, $"vspeed{oldVSpeed}");
        colouredFolder.AddFeature(p);
        coords = new CoordinateCollection();
        coords.Add(v);
        oldVSpeed = vSpeed;
    }
}

var outPath = Path.ChangeExtension(csvPath, "kml");
var outStream = new FileStream(outPath, FileMode.Create);

int subdivision = 20;
for (var i = 1; i <= subdivision; i++)
{
    var color = 255-255 * i / subdivision;
    var hexColor = color.ToString("X2");
    root.AddStyle(new Style
    {
        Id = $"vspeed{i}",
        Line = new LineStyle
        {
            Color = Color32.Parse($"FF00FF{hexColor}"),
            Width = 4
        }
    });
}
root.AddStyle(new Style
{
    Id = $"vspeed0",
    Line = new LineStyle
    {
        Color = Color32.Parse($"FF00FFFF"),
        Width = 4
    }
});

for (var i = 1; i <= subdivision; i++)
{
    var color = 255-255 * i / subdivision;
    var hexColor = color.ToString("X2");
    root.AddStyle(new Style
    {
        Id = $"vspeed-{i}",
        Line = new LineStyle
        {
            Color = Color32.Parse($"FF00{hexColor}FF"),
            Width = 4
        }
    });
}

var kml = new Kml()
{
    Feature = root
};

Task.Delay(1000).Wait();
KmlFile.Create(kml, false).Save(outStream);


Placemark CreatePlacemark(CoordinateCollection coords, string style)
{
    return new Placemark
    {
        Name = "Track",
        Geometry = new LineString
        {
            AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround,
            Coordinates = coords
        },
        StyleUrl = new Uri($"#{style}", UriKind.Relative)
    };
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
public class Data
{
   

    public Data(DateTime time, double lat, double lon, double alt, double gpsSpeed, double vSpeed)
    {
        Time = time;
        Latitude = lat;
        Longitude = lon;
        Altitude = alt;
        GPSSpeed = gpsSpeed;
        VSpeed = vSpeed;
    }

    public DateTime Time { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double VSpeed { get; set; }
    public double GPSSpeed { get; set; }

}