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

    public position(position p) {
        this.x = p.x;
        this.y = p.y;
        this.z = p.z;
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

    public static double dotProductTheta(position vector1, position vector2)
    {
        double dot = vector2.x * vector1.x + vector2.y * vector1.y + vector2.z * vector1.z;
        double abs1 = Math.Sqrt(vector2.x * vector2.x + vector2.y * vector2.y + vector2.z * vector2.z);
        double abs2 = Math.Sqrt(vector1.x * vector1.x + vector1.y * vector1.y + vector1.z * vector1.z);

        double theta = Math.Acos(dot/(abs1 * abs2)); //* 180 / Math.PI;

        return theta;
    }

    public static double dotProduct(position vector1, position vector2)
    {
        return vector2.x * vector1.x + vector2.y * vector1.y + vector2.z * vector1.z;
    }

    public static double norm(position p)
    {
        return Math.Sqrt(p.x * p.x + p.y * p.y + p.z * p.z);
    }

    public static position cross(position v1, position v2)
    {
        position p1 = new position(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v1.x);
        return p1;
    }

    public static position J2000(position moon, position velocity, position sat)
    {
        position j2000_x = new position(1, 0, 0);
        position j2000_y = new position(0, 1, 0);
        position Earth_j2000 = new position(0, 0, 0);

        //calculate moonfixed_x in j2000
        position M_x_j2000 = Earth_j2000 - moon;

        //calculate angle and rotation
        double angle = dotProductTheta(M_x_j2000, j2000_x);
 
        position k = cross(j2000_x, M_x_j2000);


        position yprime=velocity*Math.Cos(angle)+cross(k,velocity)*Math.Sin(angle)+k*(dotProduct(k,velocity))*(1-Math.Cos(angle));
        //  need to caluclate y offset and rotate around x
        double angle2 = dotProductTheta(yprime, j2000_y);
        position k2 = cross(j2000_y, velocity);


        position output1 = sat * (Math.Cos(angle2)) + cross(k2,sat) * Math.Sin(angle2) + k2 * (dotProduct(k2,sat)) * (1-Math.Cos(angle2));
        position output2 = output1 *Math.Cos(angle)+cross(k,output1)*Math.Sin(angle)+k*(dotProduct(k,output1))*(1-Math.Cos(angle));

        position T_j2000 = output2 + moon;
        return T_j2000 + sat;
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