namespace Shared.Geo;

public static class GeoDistance
{
    private const double EarthRadiusKm = 6371.0088;

    public static double KilometersBetween(GeoPoint origin, GeoPoint destination)
    {
        var dLat = ToRadians(destination.Latitude - origin.Latitude);
        var dLon = ToRadians(destination.Longitude - origin.Longitude);
        var lat1 = ToRadians(origin.Latitude);
        var lat2 = ToRadians(destination.Latitude);

        var a = Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        return EarthRadiusKm * 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}