using Csv;
using Csv2KML;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csv2kml
{
    public class DataBuilder
    {
        private Context _ctx;
        public DataBuilder(Context ctx)
        {
            _ctx = ctx;
        }

        public Data[] Build(string csvFilename)
        {
            var res=LoadFromCsv(csvFilename);
            CalculateFlightPhase(res);
            //CalculateCompensatedVario(res);
            return res;
        }

        private Data[] LoadFromCsv(string csvFilename)
        {
            var res = new List<Data>();
            var fs = new FileStream(csvFilename, FileMode.Open);

            var lastTime = DateTime.MinValue;
            double lastLat = 0;
            double lastLon = 0;
            double? lastAlt = null;


            Dictionary<string, int>? fieldByName = null;
            string getLineValue(ICsvLine line, string fieldTitle)
            {
                return line[fieldByName[fieldTitle.Trim()]];
            }
            foreach (var line in CsvReader.ReadFromStream(fs, new CsvOptions { HeaderMode = HeaderMode.HeaderPresent }))
            {
                if (fieldByName == null)
                {
                    fieldByName = new Dictionary<string, int>();
                    var headers = line.Headers;
                    for (var i = 0; i < headers.Length; i++)
                    {
                        try
                        {
                            fieldByName.Add(headers[i].Trim(), i);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
                try
                {
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Latitude).TryParseDouble(out var lat)) continue;
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Longitude).TryParseDouble(out var lon)) continue;
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Altitude).TryParseDouble(out var alt)) continue;
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.VerticalSpeed).TryParseDouble(out var verticalSpeed)) continue;
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Motor).TryParseDouble(out var motor)) continue;
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Speed).TryParseDouble(out var speed)) continue;
                    var timestamp = DateTime.Parse(getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Timestamp));
                    //if (timestamp.Subtract(lastTime).TotalSeconds<1) continue;
                    if (lastTime == timestamp || lastLat == lat && lastLon == lon) continue;
                    //Console.WriteLine($"{timestamp} {motor}");
                    //import
                    //var data = new Data(timestamp, lat, lon, alt, lastAlt, speed, motor == 1);
                    var data = new Data(timestamp, lat, lon, alt, verticalSpeed, speed, motor == 1);

                    res.Add(data);
                    lastTime = timestamp;
                    lastLat = lat;
                    lastLon = lon;
                    lastAlt = alt;
                    //if (res.Count > 100) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            //Interpolate(res);
            return res.ToArray();
        }

        private void CalculateFlightPhase(IEnumerable<Data> data)
        {
            var amountInSeconds = 10;
            var buffer = new List<AltGain>();
            var index = 0;
            var lastAltitude = data.First().Altitude;
            foreach (var d in data)
            {
                if (d.MotorActive)
                {
                    buffer.Clear();
                    d.FlightPhase = FlightPhase.MotorClimb;
                    //Console.WriteLine($"#{i} phase:{d.FlightPhase}");
                }
                else
                {
                    while (buffer.Count() > 1 && d.Time.Subtract(buffer.First().Time).TotalSeconds > amountInSeconds) buffer.RemoveAt(0);
                    buffer.Add(new AltGain(d.Time, d.Altitude - lastAltitude));
                    var weigth = buffer.Last().Time.Subtract(buffer.First().Time).TotalSeconds / amountInSeconds;
                    var acc = 0D;
                    if (buffer.Count() > 1)
                    {
                        acc = buffer.Average(b => b.Gain) * weigth;
                    }

                    if (acc > 0)
                        d.FlightPhase = FlightPhase.Climb;
                    else if (acc > -0.8)
                        d.FlightPhase = FlightPhase.Glide;
                    else
                        d.FlightPhase = FlightPhase.Sink;
                    //Console.WriteLine($"#{i} phase:{d.FlightPhase} acc:{acc} ");
                }
                lastAltitude = d.Altitude;
                index++;
            }
        }

        private void CalculateCompensatedVario(IEnumerable<Data> data)
        {
            //with gps speed instead of wind speed
            //wind gusts and against the wind will increase vario error
            //Wait to have a air speed sensor to continue
            //Or.. average tot energy for last n seconds > than turn time
            var oldAltitude = data.FirstOrDefault()?.Altitude ?? 0;
            var mass = 1;
            foreach (var d in data)
            {
                d.TotalEnergy=CalculateTotalEnergy(d,oldAltitude,mass);
            }

            Data oldD = null;
            foreach (var d in data)
            {
                if (oldD != null)
                {
                    d.DeltaEnergy = oldD.TotalEnergy - d.TotalEnergy;
                    var deltaAlt = d.DeltaEnergy / (mass * 9.81);
                    var deltaVertSpeed = deltaAlt / (d.Time.Subtract(oldD.Time).TotalMilliseconds / 1000);
                    d.EnergyVerticalSpeed = deltaVertSpeed;
                }
                oldD = d;
            }

        }

        private double CalculateTotalEnergy(Data d,double prevAltitude,double mass)
        {            
            var ke = .5 * mass * Math.Pow(d.Speed*1000/3600, 2);
            var ge = mass * 9.81 * (prevAltitude-d.Altitude);
            var totEnergy=ke + ge;
            return totEnergy;
        }
        private class AltGain
        {
            public DateTime Time { get; set; }
            public double Gain { get; set; }

            public AltGain(DateTime time, double gain)
            {
                Time = time;
                Gain = gain;
            }
        }
    }
}
