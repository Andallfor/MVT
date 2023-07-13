using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class Calc
{
    public static double tpSolve(double trueAnom, double e, double mu, double a, double start)
    {
        double cosE0 = (e + Math.Cos(trueAnom)) / (1 + e * Math.Cos(trueAnom));
        double sinE0 = (Math.Sqrt(1 - e * e) * Math.Sin(trueAnom)) / (1 + e * Math.Cos(trueAnom));
        double E0 = Math.Atan2(sinE0, cosE0);
        if (E0 < 0)
        {
            E0 = E0 + 2 * Math.PI;
        }

        return ((-E0 + e * Math.Sin(E0)) / Math.Sqrt(mu / (a * a * a)));
    }

    public static double meanAnomSolve(double trueAnom, double e, double mu, double a, double t, double start)
    {
        double tp = tpSolve(trueAnom, e, mu, a, start);
        return Math.Sqrt(mu / (a * a * a)) * (t - tp);
    }


    public static double kepSolve(double M, double e)
    {

        double EA = M;

        double k = 1;
        double error = .00000000001;

        double y = 0;

        while (k > error)
        {
            y = M + e * Math.Sin(EA);
            k = Math.Abs(Math.Abs(EA) - Math.Abs(y));
            EA = y;
        }

        return EA;
    }

    public static double topo(Kepler sat, double lat, double lon, double alt, double time, double jd)
    {
        double Deg2Rad = Math.PI / 180.0;
        position rSite = geographic.toCartesianWGS(new geographic(lat, lon), alt);
        position rSat = position.ECI2ECEF(sat.find(time), jd) - rSite;

        position rSEZ = position.mult1(position.mult2(position.R2(Math.PI / 2 - (lat * Deg2Rad)), position.R3(lon * Deg2Rad)), rSat);

        double r = position.norm(rSEZ);

        double sinEl = rSEZ.z / r;
        double cosEl = (position.norm(new position(rSEZ.x, rSEZ.y, 0))) / r;

        return Math.Atan2(sinEl, cosEl) * 180 / Math.PI;
    }


}
