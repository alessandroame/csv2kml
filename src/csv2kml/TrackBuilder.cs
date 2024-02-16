using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;

namespace csv2kml
{
    public class TrackBuilder
    {
        private Context _ctx;

        public TrackBuilder UseCtx (Context ctx)
        {
            _ctx = ctx;
            return this;
        }
        int trackIndex = 0;
        public Folder Build()
        {
            var trackFolder = new Folder
            {
                Name = "Track",
                Open = true
            };
            AddTrackStyles(trackFolder);
            trackFolder.AddFeature(BuildColoredTrack("3D track", "Value"));
            trackFolder.AddFeature(BuildColoredTrack( "Ground track", "groundValue"));
            trackFolder.AddFeature(BuildLineString( "extruded track", "extrudedValue"));
            foreach (var cameraSettings in _ctx.TourConfig.LookAtCameraSettings)
            {
                trackFolder.AddFeature(BuildTour( cameraSettings, true));
            }
            return trackFolder;
        }

        private void AddTrackStyles(Folder container)
        {
            container.AddStyle(new Style
            {
                Id = "hiddenChildren",
                List = new ListStyle
                {
                    ItemType = ListItemType.CheckHideChildren
                }
            });
            var trackWidth = 4;
            AddTrackStyle(container, $"extrudedValueMotor", $"33000000", $"00000000", trackWidth);
            AddTrackStyle(container, $"groundValueMotor", $"88000000", $"55000000", trackWidth / 2);
            AddTrackStyle(container, $"ValueMotor", $"FF000000", $"55000000", trackWidth);

            for (var i = 0; i <= _ctx.Subdivision; i++)
            {
                var value = i - _ctx.Subdivision / 2;
                var styleId = $"Value{value}";
                var normalizedValue = (double)value / _ctx.Subdivision * 2;
                var logValue = normalizedValue.ApplyExpo(_ctx.CsvConfig.ColorScaleExpo);
                var color = logValue.ToColor();
                //Console.WriteLine($"{i}-> {value} -> {normalizedValue} -> {logValue} -> {color}");

                var polygonColor = $"00{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                var lineColor = $"22{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, $"extruded{styleId}", lineColor, polygonColor, trackWidth / 2);

                lineColor = $"88{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, $"ground{styleId}", lineColor, polygonColor, trackWidth / 2);

                lineColor = $"FF{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, styleId, lineColor, polygonColor, trackWidth);
                //Console.WriteLine($"{styleId} -> {lineColor}");
            }
        }
        private void AddTrackStyle(Folder container, string name, string lineColor, string polygonColor, int width)
        {
            container.AddStyle(new Style
            {
                Id = name,
                Line = new LineStyle
                {
                    Color = Color32.Parse(lineColor),
                    Width = width
                },
                Icon = new IconStyle
                {
                    Scale = 0
                },
                Polygon = new PolygonStyle
                {
                    Color = Color32.Parse(polygonColor),

                }
            });
        }

        private Folder BuildColoredTrack(string name, string styleRadix)
        {
            return BuildTrack(name, styleRadix, BuildPlacemarkWithTrack);
        }
        private Folder BuildLineString(string name, string styleRadix)
        {
            return BuildTrack( name, styleRadix, BuildPlacemarkWithLineString);
        }
        
        private Placemark BuildPlacemarkWithTrack(List<Data> data, string style)
        {
            var track = new Track
            {
                AltitudeMode = SharpKml.Dom.AltitudeMode.Absolute,
            };
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = track,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                Description = new Description
                {
                    Text = $"#{trackIndex++} -> {style} " +
                    //$"\r\nMotor: {string.Join(" -> ", data.Select(d => d.MotorActive))}" +
                    $"\r\nPhase: {string.Join(" -> ", data.Select(d => d.FlightPhase))}" +
                    $"\r\nAlt: {string.Join(" -> ", data.Select(d => Math.Round(d.Altitude, 2)))}" +
                    $"\r\nVSpeed: {string.Join(" -> ", data.Select(d => Math.Round(d.VerticalSpeed, 2)))}"
                }
            };
            placemark.Time = new SharpKml.Dom.TimeSpan
            {
                Begin = data.First().Time,
                End = data.Last().Time,
            };
            foreach (var d in data)
            {
                track.AddWhen(d.Time);
                track.AddCoordinate(new Vector(d.Latitude, d.Longitude, d.Altitude + _ctx.AltitudeOffset));
            }
            return placemark;
        }

        private Placemark BuildPlacemarkWithLineString(List<Data> data, string style)
        {
            var lineString = new LineString
            {
                AltitudeMode = SharpKml.Dom.AltitudeMode.Absolute,
                Extrude = true,
                //Tessellate = true,
                Coordinates = new CoordinateCollection(),
            };
            var groundLineString = new LineString
            {
                AltitudeMode = SharpKml.Dom.AltitudeMode.ClampToGround,
                //Tessellate = true,
                Coordinates = new CoordinateCollection(),
            };
            var multipleGeometry = new MultipleGeometry();
            multipleGeometry.AddGeometry(lineString);
            multipleGeometry.AddGeometry(groundLineString);
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = multipleGeometry,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                //Description = new Description { Text = $"#{trackIndex++} -> {style} motor:{data.Any(d=>d.MotorActive)}" }
            };
            placemark.Time = new SharpKml.Dom.TimeSpan
            {
                Begin = data.First().Time,
                End = data.Last().Time,
            };
            //placemark.Viewpoint = data.First().CreateLookAt(data.Last(), true, altitudeMode, altitudeOffset);
            foreach (var d in data)
            {
                lineString.Coordinates.Add(new Vector(d.Latitude, d.Longitude, d.Altitude + _ctx.AltitudeOffset));
                groundLineString.Coordinates.Add(new Vector(d.Latitude, d.Longitude, d.Altitude + _ctx.AltitudeOffset));
            }
            return placemark;
        }

        private Folder BuildTrack( string name, string styleRadix,
            Func<List<Data>, string, Placemark> placemarkGenerator)
        {
            var res = new Folder
            {
                Name = name,
                StyleUrl = new Uri("#hiddenChildren", UriKind.Relative)
            };
            var coords = new List<Data>();
            var oldStyleIndex = 0;
            var oldMotorActive = _ctx.Data[0].MotorActive;
            var styleId = "";
            for (var i = 0; i < _ctx.Data.Length; i++)
            {
                var item = _ctx.Data[i];

                //TODO Normalize ( by config value )
                var nv = item.VerticalSpeed.Normalize(5);
                var styleIndex = (int)Math.Round(nv * _ctx.Subdivision / 2);

                if (oldMotorActive && !item.MotorActive)
                {
                    styleId = "Motor";
                    var p = placemarkGenerator(coords, $"{styleRadix}{styleId}");
                    res.AddFeature(p);
                    coords = new List<Data> { coords.Last() };
                    oldStyleIndex = styleIndex;
                    oldMotorActive = _ctx.Data[i].MotorActive;
                }
                else if (!oldMotorActive && oldStyleIndex != styleIndex)
                {
                    styleId = oldStyleIndex.ToString();
                    var p = placemarkGenerator(coords, $"{styleRadix}{styleId}");
                    res.AddFeature(p);
                    //if (coords.Count() > 3) Debugger.Break();
                    coords = new List<Data> { coords.Last() };
                    oldStyleIndex = styleIndex;
                    oldMotorActive = _ctx.Data[i].MotorActive;
                }
                coords.Add(item);
            }
            var lastPlacemark = placemarkGenerator(coords, $"{styleRadix}{oldStyleIndex}");

            res.AddFeature(lastPlacemark);
            return res;
        }

        private Tour BuildTour( LookAtCameraConfig cameraConfig, bool follow = false)
        {
            var tourplaylist = new Playlist();

            var oldHeading = 0d;
            for (int i = 0; i < _ctx.Data.Length - cameraConfig.UpdatePositionIntervalInSeconds; i += cameraConfig.UpdatePositionIntervalInSeconds)
            {
                var dataToShow = new List<Data>();
                for (var n = i; n >= 0; n--)
                {
                    var diff = _ctx.Data[i].Time.Subtract(_ctx.Data[n].Time).TotalSeconds;
                    if (diff > cameraConfig.VisibleHistorySeconds) break;
                    dataToShow.Insert(0, _ctx.Data[n]);
                }

                var m = 1;
                while (m < _ctx.Data.Length
                    && _ctx.Data[m].Time.Subtract(_ctx.Data[i].Time).TotalSeconds < cameraConfig.UpdatePositionIntervalInSeconds)
                {
                    m++;
                }
                var duration = _ctx.Data[m].Time.Subtract(_ctx.Data[i].Time).TotalMilliseconds / 1000;
                var lookAt = dataToShow.CreateLookAt(follow, _ctx.AltitudeOffset, cameraConfig);
                if (oldHeading != 0)
                {
                    if (oldHeading - lookAt.Heading > cameraConfig.MaxDeltaHeadingDegrees)
                        lookAt.Heading = oldHeading - cameraConfig.MaxDeltaHeadingDegrees;
                    if (lookAt.Heading - oldHeading > cameraConfig.MaxDeltaHeadingDegrees)
                        lookAt.Heading = oldHeading + cameraConfig.MaxDeltaHeadingDegrees;
                }
                var flyTo = new FlyTo
                {
                    Mode = FlyToMode.Smooth,
                    Duration = duration,
                    View = lookAt
                };
                tourplaylist.AddTourPrimitive(flyTo);
                oldHeading = lookAt.Heading.HasValue ? lookAt.Heading.Value : 0;
            }
            var tour = new Tour { Name = cameraConfig.Name };
            tour.Playlist = tourplaylist;
            return tour;
        }
    }
}
