using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// all values are centered on the sun
public readonly struct position
{
    public readonly double x, z, y;

    public position(double x, double y, double z)
    {
        this.x = x;
        this.z = z;
        this.y = y;
    }

    public position swapAxis() => new position(x, z, y);

    public static double distance(position p1, position p2) => Math.Sqrt(
        Math.Pow(p1.x - p2.x, 2) +
        Math.Pow(p1.y - p2.y, 2) +
        Math.Pow(p1.z - p2.z, 2));
    
    public static position interpLinear(position p1, position p2, double t) => new position(
        p1.x + ((p2.x - p1.x) * t),
        p1.y + ((p2.y - p1.y) * t),
        p1.z + ((p2.z - p1.z) * t));
    
    // https://stackoverflow.com/questions/34050929/3d-point-rotation-algorithm
    public position rotate(double pitch, double roll, double yaw)
    {
        double Axx = Math.Cos(yaw) * Math.Cos(pitch);
        double Axy = Math.Cos(yaw) * Math.Sin(pitch) * Math.Sin(roll) - Math.Sin(yaw) * Math.Cos(roll);
        double Axz = Math.Cos(yaw) * Math.Sin(pitch) * Math.Cos(roll) + Math.Sin(yaw) * Math.Sin(roll);

        double Ayx = Math.Sin(yaw) * Math.Cos(pitch);
        double Ayy = Math.Sin(yaw) * Math.Sin(pitch) * Math.Sin(roll) + Math.Cos(yaw) * Math.Cos(roll);
        double Ayz = Math.Sin(yaw) * Math.Sin(pitch) * Math.Cos(roll) - Math.Cos(yaw) * Math.Sin(roll);

        double Azx = -Math.Sin(pitch);
        double Azy = Math.Cos(pitch) * Math.Sin(roll);
        double Azz = Math.Cos(pitch) * Math.Cos(roll);

        position p = new position(
            Axx * x + Axy * y + Axz * z,
            Ayx * x + Ayy * y + Ayz * z,
            Azx * x + Azy * y + Azz * z);
        
        return p;
    }

    public position normalize()
    {
        double l = length();
        return new position(
            x / l,
            y / l,
            z / l);
    }

    public jsonPositionStruct requestJsonFile() => new jsonPositionStruct() {x=x, y=y, z=z};

    public double length() => Math.Sqrt(x * x + y * y + z * z);

    public override string ToString() => $"xyz: {x}, {y}, {z}";
    public static implicit operator position(Vector3 v) => new position(v.x, v.y, v.z);
    public static explicit operator Vector3(position p) => new Vector3((float) p.x, (float) p.y, (float) p.z);
    public static position operator+(position p1, position p2) => new position(
        p1.x + p2.x,
        p1.y + p2.y,
        p1.z + p2.z);
    public static position operator-(position p1, position p2) => new position(
        p1.x - p2.x,
        p1.y - p2.y,
        p1.z - p2.z);
    public static position operator*(position p1, double d) => new position(
        p1.x * d,
        p1.y * d,
        p1.z * d);
    public static position operator*(double d, position p1) => new position(
        p1.x * d,
        p1.y * d,
        p1.z * d);
    public static position operator/(position p1, double d) => new position(
        p1.x / d,
        p1.y / d,
        p1.z / d);
    public static position operator/(double d, position p1) => new position(
        d / p1.x,
        d / p1.y,
        d / p1.z);
    public static bool operator==(position p1, position p2) => (p1.x == p2.x && p1.y == p2.y && p1.z == p2.z);
    public static bool operator!=(position p1, position p2) => (p1.x != p2.x || p1.y != p2.y || p1.z != p2.z);

    public override bool Equals(object obj)
    {
        if (!(obj is position)) return false;
        position p = (position) obj;
        return this == p;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int) 2166136261;
            hash = (hash * 16777619) ^ x.GetHashCode();
            hash = (hash * 16777619) ^ y.GetHashCode();
            hash = (hash * 16777619) ^ z.GetHashCode();

            return hash;
        }
    }
}