
using SharpKml.Dom;
using static csv2kml.KmlExtensions;
using static DataExtensions;
using SharpKml.Base;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualBasic;
using SharpKml.Dom.GX;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using MathNet.Numerics;
using System.Drawing.Text;


namespace csv2kml
{
    public class OverviewBuilder
    {
        private Context _ctx;

        public OverviewBuilder(Context ctx)
        {
            _ctx = ctx;
        }
        class TimelineKey
        {
            public Vector Position{ get; set; }
            public double Distance { get; set; }
            public double Duration { get; set; }

            public TimelineKey(Vector position, double distance, double duration)
            {
                Position = position;
                Distance = distance;
                Duration = duration;
            }
        }
        public Feature Build(){
            var res = new Folder
            {
                Name = "OverView",
                Open = true
            };
            var fromTime = _ctx.Data.First().Time;
            var toTime = _ctx.Data.Last().Time;
            var bb = new BoundingBoxEx(_ctx.Data);
            const double EarthRadius = 6371 * 1000;

            var timeline = new TimelineKey[]
            {
                new TimelineKey(new Vector(0,0,0), EarthRadius*4,1),
                new TimelineKey(bb.Center.MoveTo(1000,0), 1000,1),
                new TimelineKey(bb.Center.MoveTo(100,0), 100,1),
            };

            var tourplaylist = new Playlist();

            foreach (var key in timeline)
            {
                var flyTo = new FlyTo
                {
                    Mode = FlyToMode.Smooth,
                    Duration = key.Duration,
                    View = CameraHelper.CreateLookAt(key.Position, key.Distance, 0, 0, fromTime, toTime, _ctx.AltitudeOffset)
                };
                tourplaylist.AddTourPrimitive(flyTo);
            }
            var tour = new Tour { Name = "Track Tour by flight phase" };
            tour.Playlist = tourplaylist;
            res.AddFeature(tour);
            return res;
        }


    }
}