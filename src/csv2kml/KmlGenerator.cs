using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace csv2kml
{
    public  class KmlGenerator
    {
        private Data[] _data;
        private Document _kml;
        private Folder _rootFolder;

        public KmlGenerator(Data[] data,string rootName)
        {
            _data = data;
            _kml = new Document();
            _rootFolder = new Folder
            {
                Name = rootName
            };
            _rootFolder.GenerateColoredTrack(data,"Coloured by climb",20);
            //_rootFolder.GenerateCameraPath(data,"Follow cam", 1);
            var name = "LookAt and Follow";
            _rootFolder.GenerateLookPath(data, $"{name} every 1 frame", 1, true);
            _rootFolder.GenerateLookPath(data, $"{name} every 10 frame", 10, true);
            _rootFolder.GenerateLookPath(data, $"{name} every 30 frame", 30, true);
            _rootFolder.GenerateLookPath(data, $"{name} every 50 frame", 50, true);

            name = "LookAt";
            _rootFolder.GenerateLookPath(data, $"{name} every 1 frame", 1);
            _rootFolder.GenerateLookPath(data, $"{name} every 10 frame", 10);
            _rootFolder.GenerateLookPath(data, $"{name} every 30 frame", 30);
            _rootFolder.GenerateLookPath(data, $"{name} every 50 frame", 50);
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
                _kml.AddFeature(_rootFolder);
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
