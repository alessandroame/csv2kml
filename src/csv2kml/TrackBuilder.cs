using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;

namespace csv2kml
{
    public class TrackBuilder
    {
        private Context _ctx;

        public TrackBuilder(Context ctx)
        {
            _ctx = ctx;
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
            trackFolder.AddFeature(BuildTrack1("3D track energy compensated", "Value", BuildPlacemarkWithTrack));
            trackFolder.AddFeature(BuildTrack("3D track", "Value", BuildPlacemarkWithTrack));
            var extrudedFolder = BuildTrack("Extruded track", "extrudedValue", BuildPlacemarkWithLineString);
            extrudedFolder.Visibility = false;
            trackFolder.AddFeature(extrudedFolder);
            trackFolder.AddFeature(BuildTrack("Ground track", "groundValue", BuildPlacemarkWithGroundLineString));
            foreach (var cameraSettings in _ctx.TourConfig.LookAtCameraSettings)
            {
                trackFolder.AddFeature(BuildTour(cameraSettings, true));
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
            var trackWidth = 3;
            AddTrackStyle(container, $"ValueMotor", $"FF000000", $"55000000", trackWidth);
            AddTrackStyle(container, $"extrudedValueMotor", $"33000000", $"00000000", trackWidth);
            AddTrackStyle(container, $"groundValueMotor", $"88000000", $"55000000", trackWidth / 2);

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
                AddTrackStyle(container, $"extruded{styleId}", lineColor, polygonColor, trackWidth / 3);

                lineColor = $"88{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, $"ground{styleId}", lineColor, polygonColor, trackWidth/2);

                lineColor = $"FF{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, styleId, lineColor, polygonColor, trackWidth);

                lineColor = $"88{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, "comp"+styleId, lineColor, polygonColor, trackWidth*3);
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
                    //Icon= new IconStyle.IconLink(new Uri("http://maps.google.com/mapfiles/kml/shapes/placemark_circle.png")),
                    Scale = 0
                },
                Polygon = new PolygonStyle
                {
                    Color = Color32.Parse(polygonColor),

                }
            });
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
            //placemark.Viewpoint = data.First().CreateLookAt(data.Last(), true, altitudeMode, altitudeOffset);
            foreach (var d in data)
            {
                lineString.Coordinates.Add(new Vector(d.Latitude, d.Longitude, d.Altitude + _ctx.AltitudeOffset));
            }
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = lineString,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                //Description = new Description { Text = $"#{trackIndex++} -> {style} motor:{data.Any(d=>d.MotorActive)}" }
            };
            placemark.Time = new SharpKml.Dom.TimeSpan
            {
                Begin = data.First().Time,
                End = data.Last().Time,
            };
            return placemark;
        }

        private Placemark BuildPlacemarkWithGroundLineString(List<Data> data, string style)
        {
            var lineString = new LineString
            {
                AltitudeMode = SharpKml.Dom.AltitudeMode.ClampToGround,
                Extrude = false,
                //Tessellate = true,
                Coordinates = new CoordinateCollection(),
            };
            //placemark.Viewpoint = data.First().CreateLookAt(data.Last(), true, altitudeMode, altitudeOffset);
            foreach (var d in data)
            {
                lineString.Coordinates.Add(new Vector(d.Latitude, d.Longitude));
            }
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = lineString,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                //Description = new Description { Text = $"#{trackIndex++} -> {style} motor:{data.Any(d=>d.MotorActive)}" }
            };
            placemark.Time = new SharpKml.Dom.TimeSpan
            {
                Begin = data.First().Time,
                End = data.Last().Time,
            };
            return placemark;
        }

        private Folder BuildTrack1(string name, string styleRadix,
            Func<List<Data>, string, Placemark> placemarkGenerator)
        {
            var res = new Folder
            {
                Name = name,
                StyleUrl = new Uri("#hiddenChildren", UriKind.Relative),
                Visibility = false
            };
            var coords = new List<Data>();
            var oldStyleIndex = 0;
            var oldMotorActive = _ctx.Data[0].MotorActive;
            var styleId = "";
            var createPlacemark = false;
            for (var i = 0; i < _ctx.Data.Length; i++)
            {
                var item = _ctx.Data[i];

                //TODO Normalize ( by config value )
                var compensatedVspeed = item.VerticalSpeed - item.EnergyVerticalSpeed;
                var nv = compensatedVspeed.Normalize(5);
                var styleIndex = (int)Math.Round(nv * _ctx.Subdivision / 2);

                if (coords.Count() > 2)
                {
                    if (oldMotorActive && !item.MotorActive)
                    {
                        styleId = "Motor";
                        createPlacemark = true;
                    }
                    else if (!oldMotorActive && oldStyleIndex != styleIndex)
                    {
                        styleId = oldStyleIndex.ToString();
                        createPlacemark = true;
                    }
                }
                if (createPlacemark)
                {
                    var p = placemarkGenerator(coords, $"comp{styleRadix}{styleId}");
                    res.AddFeature(p);
                    coords = new List<Data> { coords.Last() };
                    oldStyleIndex = styleIndex;
                    oldMotorActive = _ctx.Data[i].MotorActive;
                    createPlacemark = false;
                }
                coords.Add(item);
            }
            var lastPlacemark = placemarkGenerator(coords, $"{styleRadix}{oldStyleIndex}");

            res.AddFeature(lastPlacemark);
            return res;
        }
        private Folder BuildTrack(string name, string styleRadix,
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
            var createPlacemark = false;
            for (var i = 0; i < _ctx.Data.Length; i++)
            {
                var item = _ctx.Data[i];

                //TODO Normalize ( by config value )
                var nv = item.VerticalSpeed.Normalize(5);
                var styleIndex = (int)Math.Round(nv * _ctx.Subdivision / 2);

                if (coords.Count() > 2)
                {
                    if (oldMotorActive && !item.MotorActive)
                    {
                        styleId = "Motor";
                        createPlacemark = true;
                    }
                    else if (!oldMotorActive && oldStyleIndex != styleIndex)
                    {
                        styleId = oldStyleIndex.ToString();
                        createPlacemark = true;
                    }
                }
                if (createPlacemark)
                {
                    var p = placemarkGenerator(coords, $"{styleRadix}{styleId}");
                    res.AddFeature(p);
                    coords = new List<Data> { coords.Last() };
                    oldStyleIndex = styleIndex;
                    oldMotorActive = _ctx.Data[i].MotorActive;
                    createPlacemark = false;
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
                while (m < _ctx.Data.Length-1
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
