using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct geographic
{
    public double lat, lon;
    public geographic(double lat, double lon)
    {
        this.lat = Math.Min(Math.Max(-90, lat), 90);
        this.lon = Math.Min(Math.Max(-180, lon), 180);
    }

    /// <summary> Parse DMS format. DO NOT USE WITH DECIMAL FORMAT </summary>
    public geographic(string lat, string lon) {
        double latSign = lat.Last() == 'S' ? -1 : 1;
        double lonSign = lon.Last() == 'W' ? -1 : 1;

        this.lat = latSign * parseDmsStringToDecimal(general.combineCharArray(lat.Take(lat.Length - 1).ToArray()));
        this.lon = lonSign * parseDmsStringToDecimal(general.combineCharArray(lon.Take(lon.Length - 1).ToArray()));
    }

    // degree minute second
    public static double parseDmsStringToDecimal(string dms) {
        int sIndex = dms[dms.Length - 2] == '.' ? -4 : -2;
        int mIndex = sIndex - 2;
        double degree = double.Parse(general.combineCharArray(dms.Take(dms.Length + mIndex).ToArray()));
        double minute = double.Parse(dms.Substring(dms.Length + mIndex, (dms.Length + sIndex) - (dms.Length + mIndex)));
        double second = double.Parse(dms.Substring(dms.Length + sIndex));

        return degree + ((minute + (second / 60.0)) / 60.0);
    }

    /// <summary> Converts lat, lon into a cartesian point centered on (0, 0) with length radius </summary>
    public position toCartesian(double radius) => toCartesian(this, radius);

    public static position toCartesian(geographic g, double radius) {
        g.lat = Math.Min(Math.Max(-90, g.lat), 90);
        g.lon = Math.Min(Math.Max(-180, g.lon), 180);

        double lt = g.lat * (Math.PI / 180.0);
        double ln = g.lon * (Math.PI / 180.0);

        return new position(
            radius * Math.Cos(lt) * Math.Cos(ln),
            radius * Math.Cos(lt) * Math.Sin(ln),
            radius * Math.Sin(lt));
    }

    public position toCartesianWGS(double alt) => toCartesianWGS(this, alt);

    public static position toCartesianWGS(geographic g, double alt) {
        double Deg2Rad = Math.PI / 180.0;
        geographic geo = new geographic(g.lat * Deg2Rad, g.lon * Deg2Rad);
        
        double rEq = 6378.14; // equatorial radius
        double rPol = 6356.75; // polar radius

        double a = Math.Pow(Math.Pow(rEq, 2) * Math.Cos(geo.lat), 2) + Math.Pow(Math.Pow(rPol, 2) * Math.Sin(geo.lat), 2);
        double b = Math.Pow(rEq * Math.Cos(geo.lat), 2) + Math.Pow(rPol * Math.Sin(geo.lat), 2);

        double rGs = Math.Sqrt(a / b);

        double aE = 6378.137;
        double eEsq = 6.69437888014 * .001;

        double nTheta = aE / Math.Sqrt(1 - eEsq * Math.Sin(geo.lat));

        double xGS_geo = (nTheta + alt) * Math.Cos(geo.lat) * Math.Cos(geo.lon);
        double yGS_geo = (nTheta + alt) * Math.Cos(geo.lat) * Math.Sin(geo.lon);
        double zGS_geo = (nTheta * (1 - eEsq) + alt) * Math.Sin(geo.lat);

        position locGeo = new position(xGS_geo, yGS_geo, zGS_geo);
        position n = locGeo.normalize();
        locGeo = new position(locGeo.x / n.x, locGeo.y / n.y, locGeo.z / n.z);

        return locGeo * (rGs + alt);
    }

    /// <summary> Takes a point centered on (0, 0) with unknown length, and converts it into geo </summary>
    public static geographic toGeographic(position point, double radius)
    {
        // draw point onto planet
        double dist = position.distance(new position(0, 0, 0), point);
        double div = radius / dist;

        position p = new position(
            point.x * div,
            point.y * div,
            point.z * div);

        return new geographic(
            Math.Asin(p.y / radius) * (180.0 / Math.PI),
            Math.Atan2(p.z, p.x) * (180.0 / Math.PI));
    }

    /// <summary> Gets the distance between two geographic points, assuming the shortest path is on the sphere with radius </summary>
    public double distanceKm(geographic g, double radius) {
        double lt1 = this.lat * (Math.PI / 180.0);
        double ln1 = this.lon * (Math.PI / 180.0);
        double lt2 = g.lat * (Math.PI / 180.0);
        double ln2 = g.lon * (Math.PI / 180.0);

        // haversine formula
        return 2.0 * radius * Math.Asin(Math.Sqrt(
            (Math.Sin((lt2 - lt1) / 2.0) * Math.Sin((lt2 - lt1) / 2.0)) +
            Math.Cos(lt1) * Math.Cos(lt2) * 
            (Math.Sin((ln2 - ln1) / 2.0) * Math.Sin((ln2 - ln1) / 2.0))));
    }

    public double distAs2DVector(geographic g) => Math.Sqrt(
        (g.lat - this.lat) * (g.lat - this.lat) + (g.lon - this.lon) * (g.lon - this.lon));
    
    public double magnitude() => Math.Sqrt(lat * lat + lon * lon);

    public static geographic operator+(geographic g1, geographic g2) => new geographic(g1.lat + g2.lat, g1.lon + g2.lon);
    public static geographic operator-(geographic g1, geographic g2) => new geographic(g1.lat - g2.lat, g1.lon - g2.lon);
    public static geographic operator*(geographic g1, double d) => new geographic(g1.lat * d, g1.lon * d);
    public static geographic operator*(double d, geographic g1) => g1 * d;
    public static geographic operator/(geographic g1, double d) => new geographic(g1.lat / d, g1.lon / d);
    public static geographic operator/(double d, geographic g1) => new geographic(d / g1.lat, d / g1.lon);
    public static bool operator==(geographic g1, geographic g2) => g1.lat == g2.lat && g1.lon == g2.lon;
    public static bool operator!=(geographic g1, geographic g2) => g1.lat != g2.lat || g1.lon != g2.lon;

    public override bool Equals(object obj)
    {
        if (!(obj is geographic)) return false;
        geographic p = (geographic) obj;
        return this == p;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int) 2166136261;
            hash = (hash * 16777619) ^ lat.GetHashCode();
            hash = (hash * 16777619) ^ lon.GetHashCode();

            return hash;
        }
    }
    public override string ToString() => $"Latitude: {lat} | Longitude {lon}";
}
