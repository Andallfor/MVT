using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class Contacts
{
    public static List<double[]> runContacts(Kepler sat, geographic geo, double alt, double minEl, double step, double start, double duration, double tp, string gName, string sName, double jd)
    {
        List<double> t = new List<double>();

        double lat = geo.lat;
        double lon = geo.lon;
        double time = 0;

        double y1 = 0;
        double y2 = 0;
        double t1 = 0;
        double t2 = 0;

        double elstart = Calc.topo(sat, lat, lon, alt, time, jd) - minEl;

        if (elstart > 0)
        {
            t.Add(time);
        }

        double test1 = 0;
        double t3 = 0;
        double tc = 0;

        while (time < duration - step)
        {

            double el1 = Calc.topo(sat, lat, lon, alt, time, jd + (time / 86400)) - minEl;
            double el2 = Calc.topo(sat, lat, lon, alt, time + step, jd + ((time + step) / 86400)) - minEl;

            y1 = el1;
            t1 = time;
            y2 = el2;
            t2 = time + step;

            double norp = y2 * y1;
            if (norp < 0)
            {
                test1 = 1;

                while (test1 > 0.0001)
                {
                    t3 = ((t2 - t1) / (y2 - y1) * -y1) + t1;

                    double var = Calc.topo(sat, lat, lon, alt, t3, jd + ((t3) / 86400));

                    if (var < minEl)
                    {
                        y1 = var - minEl;
                        t1 = t3;
                    }
                    else if (var > minEl)
                    {
                        y2 = var - minEl;
                        t2 = t3;
                    }

                    test1 = Math.Min(Math.Abs(y1), Math.Abs(y2));
                }

                if (Math.Abs(y1) > Math.Abs(y2))
                {
                    tc = t2;
                }
                else
                {
                    tc = t1;
                }

                t.Add(tc + start);
            }


            time += step;
        }

        if (Calc.topo(sat, lat, lon, alt, time, jd + (time / 86400)) > 0)
        {
            t.Add(time + start);
        }

        if (t.Count % 2 != 0)
        {
            t.Add(Calc.topo(sat, lat, lon, alt, time, jd + (time / 86400)));
        }

        if (t.Any())
        {
            List<double[]> windows = new List<double[]>();

            for (int x = 0; x < t.Count; x = x + 2)
            {
                if (t[x + 1] - t[x] / 60 > 5)
                {
                    double[] arr = new double[2];
                    arr[0] = t[x] / 86400;
                    arr[1] = t[x + 1] / 86400;
                    windows.Add(arr);
                }
            }

            return windows;
        }
        else
        {
            return null;
        }
    }
}

