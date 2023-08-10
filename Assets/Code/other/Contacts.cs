using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class Contacts
{
    public static List<double[]> runContacts(Timeline sat, Timeline centralBody, geographic geo, double alt, double minEl, double step, double duration, double jd)
    {
        List<double> t = new List<double>();

        double time = 0;

        double y1 = 0;
        double y2 = 0;
        double t1 = 0;
        double t2 = 0;

        double elstart = Calc.topo(sat, centralBody, geo, alt, jd) - minEl;

        if (elstart > 0)
        {
            t.Add(time);
        }

        //y1 = el1;
        //t1 = time;

        //time += step;

        double test1 = 0;
        double t3 = 0;
        double tc = 0;
        double yhold = elstart;
        double thold = time;
        double var = 0;
        double el = 0;

        while (time < duration - step)
        {
            el = Calc.topo(sat, centralBody, geo, alt, jd + ((time + step) / 86400)) - minEl;

            y1 = yhold;
            t1 = thold;
            y2 = el;
            t2 = time + step;
            yhold = y2;
            thold = t2;

            double norp = y2 * y1;
            //Console.WriteLine("norp: " + norp);
            if (norp < 0)
            {
                test1 = 1;

                while (test1 > 0.01)
                {
                    //Console.WriteLine("Time: " + time+"\ttest1: "+test1);
                    t3 = ((t2 - t1) / (y2 - y1) * -y1) + t1;

                    var = Calc.topo(sat, centralBody, geo, alt, jd + ((t3) / 86400));

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

                t.Add(tc);
            }

            //t1 = time;
            //y1 = el;
            time += step;
        }
        //Console.WriteLine("After exiting the time was: " + time);

        if (Calc.topo(sat, centralBody, geo, alt, jd + (time / 86400)) > 0)
        {
            t.Add(time);
        }


        if (t.Count % 2 != 0)
        {
            t.Add(Calc.topo(sat, centralBody, geo, alt, jd + ((t3) / 86400)));
        }
        

        if (t.Any())
        {
            List<double[]> windows = new List<double[]>();

            for (int x = 0; x < t.Count; x = x + 2)
            {
                if (t[x + 1] - t[x] / 60 > 5)
                {
                    double[] arr = new double[2];
                    arr[0] = t[x] / 86400 + jd;
                    arr[1] = t[x + 1] / 86400 + jd;
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

    public static List<double[]> runContacts(Timeline sat, geographic geo, double alt, double minEl, double step, double duration, double jd)
    {
        List<double> t = new List<double>();

        double time = 0;

        double y1 = 0;
        double y2 = 0;
        double t1 = 0;
        double t2 = 0;

        double elstart = Calc.topo(sat, geo, alt, jd) - minEl;
        
        if (elstart > 0)
        {
            t.Add(time);
        }

        //y1 = el1;
        //t1 = time;

        //time += step;

        double test1 = 0;
        double t3 = 0;
        double tc = 0;
        double yhold = elstart;
        double thold = time;
        double var = 0;
        double el = 0;

        while (time < duration - step)
        {
            el = Calc.topo(sat, geo, alt, jd + ((time + step) / 86400)) - minEl;
            
            y1 = yhold;
            t1 = thold;
            y2 = el;
            t2 = time + step;
            yhold = y2;
            thold = t2;

            double norp = y2 * y1;
            //Console.WriteLine("norp: " + norp);
            if (norp < 0)
            {
                test1 = 1;

                while (test1 > 0.01)
                {
                    //Console.WriteLine("Time: " + time+"\ttest1: "+test1);
                    t3 = ((t2 - t1) / (y2 - y1) * -y1) + t1;

                    var = Calc.topo(sat, geo, alt, jd + ((t3) / 86400));
                    
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

                t.Add(tc);
            }

            //t1 = time;
            //y1 = el;
            time += step;
        }
        //Console.WriteLine("After exiting the time was: " + time);

        if (Calc.topo(sat, geo, alt, jd + (time / 86400)) > 0)
        {
            t.Add(time);
        }


        if (t.Count % 2 != 0)
        {
            t.Add(Calc.topo(sat, geo, alt, jd + (time / 86400)));
        }


        if (t.Any())
        {
            List<double[]> windows = new List<double[]>();

            for (int x = 0; x < t.Count; x = x + 2)
            {
                if (t[x + 1] - t[x] / 60 > 5)
                {
                    double[] arr = new double[2];
                    arr[0] = t[x] / 86400 + jd;
                    arr[1] = t[x + 1] / 86400 + jd;
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

