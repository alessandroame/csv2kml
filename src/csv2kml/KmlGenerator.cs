using SharpKml.Base;
using SharpKml.Dom;
using System;
using System.Drawing;
using System.Xml.Linq;
using AltitudeMode = SharpKml.Dom.AltitudeMode;

namespace csv2kml
{
    public  class KmlGenerator
    {
        private Data[] _data;
        private Kml _kml;
        private Folder _rootFolder;
        private SharpKml.Dom.AltitudeMode _altitudeMode;
        private int _altitudeOffset;

        public KmlGenerator(Data[] data,string rootName, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset, TourSettings tourSettings)
        {
            _altitudeMode = altitudeMode;
            _altitudeOffset = altitudeOffset;
            _data = data;
            _kml = new Kml();
            _rootFolder = new Folder
            {
                Name = rootName,
                Open = true
            };
            var min = data.Min(d => d.VSpeed);
            var max = data.Max(d => d.VSpeed);
            var colorsSubdivision = 20;
            _rootFolder.BuildStyles("vspeed", colorsSubdivision);

            var dataFolder=new Folder
            {
                Name = "Coloured by climb"
            };
            _rootFolder.AddFeature(dataFolder);
            dataFolder.GenerateColoredTrack(data, "3D track", colorsSubdivision, _altitudeMode, _altitudeOffset);
            dataFolder.GenerateColoredTrack(data, "Ground track", colorsSubdivision, AltitudeMode.ClampToGround, _altitudeOffset, "ground");
            dataFolder.GenerateLineString(data, "extruded track", colorsSubdivision, _altitudeMode, _altitudeOffset);


            var animationFolder = new Folder
            {
                Name = "Animations", Open = true
            };
            _rootFolder.GenerateLookBackPath(data, "Animation", _altitudeMode, _altitudeOffset, 
                    tourSettings.LookAtCameraSettings.UpdatePositionFrameInterval,
                    tourSettings.LookAtCameraSettings.LookBackSeconds,
                    tourSettings.LookAtCameraSettings.MetersFromTrackPoint,
                    tourSettings.LookAtCameraSettings.Tilt,
                    tourSettings.LookAtCameraSettings.PanOffset, true);
        }

        private Bitmap GenerateLegend(double k,int subdivisions)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            var w = 200;
            var h = 400;
            var bitmap = new Bitmap(w,h);
            Graphics graphics = Graphics.FromImage(bitmap);

            for (var i = 0; i <= subdivisions; i++)
            {
                var normalizedValue = (float)i / subdivisions;
                var color = normalizedValue.ToColor();
                var value = i / subdivisions * k;

                var brush = new SolidBrush(color);
                graphics.FillRectangle(
                    brush,
                    new Rectangle {
                        X=0,
                        Y=h-h/subdivisions*i,
                        Width=w/2, 
                        Height=h/subdivisions
                    });
                bitmap.Save("legend.bmp");
#pragma warning restore CA1416 // Validate platform compatibility
            }
            return bitmap;
        }
       
        public bool SaveTo(string fn,out string errors)
        {
            var res = false;
            errors = string.Empty;
            try
            {
                var document = new Document
                {
                    Name = $"imported from {Path.GetFileName(fn)}",
                    Open = true
                };
                document.AddFeature(_rootFolder);
                _kml.Feature = document;
                //_kml.(document);
                var serializer = new Serializer();
                serializer.Serialize(_kml);
                var outStream = new FileStream(fn, FileMode.Create);
                var sw = new StreamWriter(outStream);
                sw.Write(serializer.Xml);
                sw.Close();
                res = true;
            }
            catch (Exception ex)
            {
                errors= ex.ToString();
            }
            return res;
        }
    }
}
