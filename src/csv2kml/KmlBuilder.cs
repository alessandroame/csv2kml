using Csv;
using Csv2KML;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Xml.Linq;

namespace csv2kml
{
    public partial class KmlBuilder
    {
        private Context _ctx;
        Folder _rootFolder = new Folder
        {
            Open = true
        };

        public KmlBuilder()
        {
            _ctx = new Context
            {
                Subdivision = 120
            };
        }
        public KmlBuilder UseCsvConfig(string configFilename)
        {
            _ctx.CsvConfig = CsvConfig.FromFile(configFilename);
            return this;
        }

        public KmlBuilder UseTourConfig(string configFilename)
        {
            _ctx.TourConfig = TourConfig.FromFile(configFilename);
            return this;
        }
        public KmlBuilder Build(string csvFilename, double altitudeOffset)
        {
            _ctx.AltitudeOffset = altitudeOffset;
            _ctx.Data = new DataBuilder().UseCtx(_ctx).Build(csvFilename);
            _rootFolder.Name = $"{Path.GetFileNameWithoutExtension(csvFilename)}";
            _rootFolder.AddFeature(new TrackBuilder().UseCtx(_ctx).Build());
            _rootFolder.AddFeature(new SegmentBuilder().UseCtx(_ctx).Build());
            _rootFolder.AddFeature(new OverviewBuilder().UseCtx(_ctx).Build());

            return this;
        }

        public void Save(string fn)
        {
            try
            {
                var kml = new Kml();
                var document = new Document
                {
                    Open = true
                };
                document.AddFeature(_rootFolder);
                kml.Feature = document;
                //_kml.(document);
                var serializer = new Serializer();
                serializer.Serialize(kml);
                var outStream = new FileStream(fn, FileMode.Create);
                var sw = new StreamWriter(outStream);
                sw.Write(serializer.Xml);
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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



    }
}