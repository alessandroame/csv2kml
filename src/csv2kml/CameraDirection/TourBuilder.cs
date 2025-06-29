using SharpKml.Dom.GX;

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
