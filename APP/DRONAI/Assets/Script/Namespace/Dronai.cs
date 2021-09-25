namespace Dronai
{
    namespace Data
    {
        public class DroneEvent
        {
            public string DroneId = string.Empty;
            public string Detail = string.Empty;
            public string ImgPath = string.Empty;


            public DroneEvent(string droneId, string detail, string imgPath)
            {
                this.DroneId = droneId;
                this.Detail = detail;
                this.ImgPath = imgPath;
            }
        }
    }
}