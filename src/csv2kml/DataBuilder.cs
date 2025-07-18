﻿using Csv;
using Csv2KML;
using System.Diagnostics;

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
            var res = LoadFromCsv(csvFilename);
            //            CalculateFlightPhase(res);
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
                var index = fieldByName[fieldTitle.Trim()];
                return line[index];
            }
            var index = 0;
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
                    double lat, lon;
                    if (string.IsNullOrEmpty(_ctx.CsvConfig.FieldsByTitle.LatLon))
                    {
                        if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Latitude).TryParseDouble(out lat)) continue;
                        if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Longitude).TryParseDouble(out lon)) continue;
                    }
                    else
                    {
                        if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.LatLon).TryParseLatLon(out lat,out lon)) continue;
                    }
                    if (lat==0 || lon==0)
                    {
                        Console.WriteLine($"Error in gps coords: {lat},{lon}");
                        continue;
                    }
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Altitude).TryParseDouble(out var alt)) continue;
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.VerticalSpeed).TryParseDouble(out var verticalSpeed)) continue;
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Motor).TryParseDouble(out var motor)) continue;
                    if (!getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Speed).TryParseDouble(out var speed)) continue;
                    if (!DateTime.TryParse(getLineValue(line, _ctx.CsvConfig.FieldsByTitle.Timestamp), out var timestamp)) continue;

                    /*var s=getLineValue(line, "Date")+" "+ getLineValue(line, "Time");
                    var timestamp = DateTime.Parse(s);*/
                    if (lastTime == timestamp || lastLat == lat && lastLon == lon) continue;
                    //Console.WriteLine($"{timestamp} {motor}");

                    //import
                    var data = new Data(index, timestamp, lat, lon, alt, verticalSpeed, speed, motor == 1);
                    res.Add(data);
                    index++;
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

        private void Interpolate(List<Data> data)
        {
            for (int i = 0; i < data.Count(); i++)
            {
                var currentD = data[i];
                var count = 1;
                while (currentD.Latitude == data[i + (count)].Latitude
                    && currentD.Longitude == data[i + (count)].Longitude
                    && (i + count) < data.Count() - 1) { count++; }
                if ((i + count) > data.Count() - 1) break;
                var nextD = data[i + count];
                var dt = nextD.Time.Subtract(currentD.Time).TotalMilliseconds;
                var dLat = currentD.Latitude - nextD.Latitude;
                var dLon = currentD.Longitude - nextD.Longitude;
                //                Console.WriteLine($"----------------------------");
                for (var n = 0; n < count; n++)
                {
                    var d = data[i + n];
                    var k = d.Time.Subtract(currentD.Time).TotalMilliseconds / dt;
                    //                  Console.WriteLine(k);
                    //Console.WriteLine($"{d.Time}.{d.Time.Millisecond}------ {d.Altitude} ----------------------------");
                    //Console.WriteLine($"{d.Latitude}, {d.Longitude}");
                    d.Latitude -= k * dLat;
                    d.Longitude -= k * dLon;
                    //Console.WriteLine($"{d.Latitude}, {d.Longitude}");
                }
                i += count;
            }

        }


        //private void Interpolate(List<Data> data)
        //{
        //    InterpolatField(data, (d) => d.Time.Ticks,
        //        (data, delta) => { data.Time = data.Time.AddTicks((long)delta); });
        //    InterpolatField(data, (d) => d.Altitude,
        //        (data, delta) => { data.Altitude = data.Altitude + delta; });
        //    InterpolatField(data, (d) => d.Latitude,
        //        (data, delta) => { data.Latitude = data.Latitude + delta; });
        //    InterpolatField(data, (d) => d.Longitude,
        //           (data, delta) => { data.Longitude = data.Longitude + delta; });
        //}

        //private void InterpolatField(List<Data> data, Func<Data, double> valueGetter, Action<Data, double> valueSetter) 
        //{
        //    var segment = new List<Data>();
        //    var lastData = data[0];
        //    foreach (var d in data)
        //    {
        //        if (valueGetter(lastData)==(valueGetter(d)))
        //        {
        //            segment.Add(d);
        //        }
        //        else
        //        {
        //            if (segment.Count() > 1)
        //            {
        //                var v = segment.Count() + 1;
        //                var delta = (valueGetter(d)-valueGetter(lastData)) / v;
        //                var zz0 = segment.Select(s => $"{s.Time.Ticks}-{s.Altitude}").ToArray();
        //                for (var i = 1; i < segment.Count(); i++)
        //                {
        //                    var s = segment[i];
        //                    valueSetter(s,delta * i);
        //                }
        //                var zz1 = segment.Select(s => $"{s.Time.Ticks}-{s.Altitude}").ToArray();
        //            }
        //            lastData = d;
        //            segment = new List<Data>() { d };
        //        }
        //    }
        //}


        private void CalculateFlightPhase(IEnumerable<Data> data)
        {
            for (var i = 0; i < data.Count(); i++)
            {
                var current = data.ElementAt(i);
                if (current.MotorActive)
                {
                    current.FlightPhase = FlightPhase.MotorClimb;
                }
                else
                {
                    current.FlightPhase = current.VerticalSpeed.ToFlightPhase();
                }
            }
        }

        private void CalculateCompensatedVario(IEnumerable<Data> data)
        {
            //with gps speed instead of wind speed
            //wind gusts and against the wind will increase vario error
            //Wait to have a air speed sensor to continue
            //Or.. average tot energy for last n seconds > than turn time
            //var oldAltitude = data.FirstOrDefault()?.Altitude ?? 0;
            var mass = 1200;
            foreach (var d in data)
            {
                d.TotalEnergy = CalculateTotalEnergy(d, mass);
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

        private double CalculateTotalEnergy(Data d, double mass)
        {
            var ke = .5 * mass * Math.Pow(d.Speed * 1000 / 3600, 2);
            var ge = mass * 9.81 * d.Altitude;
            var totEnergy = ke + ge;
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
