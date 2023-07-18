using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevationCheck
{
    public static List<double[]> elevationTimes (Timeline sat, geographic geo, double alt, double minEl, double duration, double jd)
    {
        double startEpoch = 0;
        double step = 60 * 2.5;
        const double muE = 398600.44;

        return Contacts.runContacts(sat, geo, alt, minEl, step, duration, jd);
    }
}

