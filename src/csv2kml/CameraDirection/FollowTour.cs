using csv2kml.CameraDirection;
using SharpKml.Dom.GX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataExtensions;

namespace csv2kml.CameraDirection
{
    public class FollowTour : TourBuilder
    {

        public FollowTour(Context context) : base(context)
        {
        }
        
        public override Tour Build()
        {
            throw new NotImplementedException();
        }
    }
}
