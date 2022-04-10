using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct geographic
{
    public double lat, lon;
    public geographic(double lat, double lon)
    {
        this.lat = Math.Min(Math.Max(-90, lat), 90);
        this.lon = Math.Min(Math.Max(-180, lon), 180);
    }

    /// <summary> Converts lat, lon into a cartesian point centered on (0, 0) with length radius </summary>
    public position toCartesian(double radius)
    {
        this.lat = Math.Min(Math.Max(-90, lat), 90);
        this.lon = Math.Min(Math.Max(-180, lon), 180);

        double lt = this.lat * (Math.PI / 180.0);
        double ln = this.lon * (Math.PI / 180.0);

        return new position(
            radius * Math.Cos(lt) * Math.Cos(ln),
            radius * Math.Cos(lt) * Math.Sin(ln),
            radius * Math.Sin(lt));
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

    public jsonGeographicStruct requestJsonFile()
    {
        return new jsonGeographicStruct() {
            lat = lat,
            lon = lon};
    }

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
