using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevationCheck
{
    public List<double[]> elevationTimes (Kepler sat, geographic geo, double alt, double minEl, double duration, string gName, string sName, double jd)
    {
        double startEpoch = 0;
        double step = 60 * 2.5;
        const double muE = 398600.44;

        double a = sat.semiMajorAxis;
        double v0 = sat.v0;
        double e = sat.eccentricity;

        double tp = Calc.tpSolve(v0, e, muE, a, startEpoch);

        return Contacts.runContacts(sat, geo, alt, minEl, step, startEpoch, duration, tp, gName, sName, jd);
    }
}

