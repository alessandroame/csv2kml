using csv2kml.CameraDirection;
using SharpKml.Base;
using SharpKml.Dom;

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
            _ctx.Data = new DataBuilder(_ctx).Build(csvFilename);
            _rootFolder.Name = $"{Path.GetFileNameWithoutExtension(csvFilename)}";
            _rootFolder.AddFeature(new SegmentBuilder(_ctx).Build());
            _rootFolder.AddFeature(new TrackBuilder(_ctx).Build());
            //_rootFolder.AddFeature(new OverviewBuilder(_ctx).Build());
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

    }
}