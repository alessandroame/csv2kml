
using SharpKml.Dom;

namespace csv2kml
{
    internal class OverviewBuilder
    {
        private Context _ctx;

        public OverviewBuilder UseCtx(Context ctx)
        {
            _ctx = ctx;
            return this;
        }

        public Feature Build(){
            var res = new Folder
            {
                Name = "OverView",
                Open = true
            };

            return res;
        }


    }
}