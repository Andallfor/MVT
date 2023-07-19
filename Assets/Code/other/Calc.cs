using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class Calc
{
    public static double topo(Timeline sat, geographic geo, double alt, double jd)
    {
        double Deg2Rad = Math.PI / 180.0;
        position rSite = geo.toCartesianWGS(alt);
        position satSwitch = sat.find(new Time(jd));
        position rSat = position.ECI2ECEF(new position(satSwitch.x, satSwitch.z, satSwitch.y), jd) - rSite;

        position rSEZ = position.mult1(position.mult2(position.R2(Math.PI / 2 - (geo.lat * Deg2Rad)), position.R3(geo.lon * Deg2Rad)), rSat);

        double r = position.norm(rSEZ);

        double sinEl = rSEZ.z / r;
        double cosEl = (position.norm(new position(rSEZ.x, rSEZ.y, 0))) / r;

        return Math.Atan2(sinEl, cosEl) * 180 / Math.PI;
    }
}
