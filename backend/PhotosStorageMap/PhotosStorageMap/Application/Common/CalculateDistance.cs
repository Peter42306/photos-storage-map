namespace PhotosStorageMap.Application.Common
{
    public static class Calculator
    {
        public static double DistanceBetweenLocations(
            double latitude1, double longitude1,
            double latitude2, double longitude2)
        {
            const double R = 6371000;

            var latitude1Rad = ToRad(latitude1);
            var latitude2Rad = ToRad(latitude2);

            var dLatitude = ToRad(latitude2 - latitude1);
            var dLongitude = ToRad(longitude2 - longitude1);

            var a = 
                Math.Sin(dLatitude / 2) * Math.Sin(dLatitude / 2) + 
                Math.Cos(latitude1Rad) * Math.Cos(latitude2Rad) * 
                Math.Sin(dLongitude / 2) * Math.Sin(dLongitude / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        public static double ToRad(double deg)
        {
            return deg * Math.PI / 180;
        }
    }
}
