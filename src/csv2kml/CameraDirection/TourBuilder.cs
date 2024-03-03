using SharpKml.Dom;
using SharpKml.Dom.GX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csv2kml.CameraDirection
{
    public abstract class TourBuilder
    {
        protected Context _ctx;

        public TourBuilder(Context context)
        {
            _ctx = context;
        }

        public abstract Tour Build();
    }
}
