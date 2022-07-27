using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class visibility
{

    [Flags]
    public enum raycastParameters
    {
        planet = 1,
        satellite = 2,
        facility = 4,
        all = 8
    }

    public struct raycastInfo
    {

        public bool hit;
        public List<planet> hitPlanets;
        public List<facility> hitFacs;
        public List<satellite> hitSats;

        public raycastInfo(bool hit, List<planet> HitPlanets, List<satellite> HitSats, List<facility> HitFacs)
        {
            this.hit = hit;
            this.hitPlanets = HitPlanets;
            this.hitSats = HitSats;
            this.hitFacs = HitFacs;
        }
    }

    public struct link
    {
        public bool hit;
        public double time;
        public double distance;

        public link(bool hit, double time, double distance)
        {
            this.hit = hit;
            this.time = time;
            this.distance = distance;
        }
    }    
            

    private static double raycastMath(position position1, position position2, position obj)
    {
        position vector2 = new position(
          position1.x * -1 + position2.x,
          position1.y * -1 + position2.y,
          position1.z * -1 + position2.z
        );

        position vectorOBJ = new position(
          position1.x * -1 + obj.x,
          position1.y * -1 + obj.y,
          position1.z * -1 + obj.z
        );

        return Math.Sin(position.dotProductTheta(vectorOBJ, vector2)) * position.distance(position1, obj);
    }

    /// <summary> This is the main raycast function for our program and it takes in 5 arguments. The final argument if set true will return a list of all the hit objects, but if it is set to false the program will break after the first object is hit </summary>
    public static link dynamicLinkVisibility(position p1, position p2, Time t, int defaultRadius)
    {
        bool hit = false;

        double p1p2Distance = position.distance(p1, p2);

        foreach (planet p in master.allPlanets) 
        {
            if (position.distance(p1, p.requestPosition(t)) < p1p2Distance) 
            {
                double distanceFromObj = raycastMath(p1, p2, p.requestPosition(t));
                if (distanceFromObj <= p.radius)
                {
                    hit = true;
                    break;
                }
            }
        }
        
        
        if (hit == false)
        {
            link dLink;
            dLink.hit = false;
            dLink.time = t.julian;
            dLink.distance = p1p2Distance;
            return dLink;
        }
        else
        {
            link dLink;
            dLink.hit = true;
            dLink.time = 0;
            dLink.distance = 0;
            return dLink;
        }
    }


    /// <summary> This is the main raycast function for our program and it takes in 5 arguments. The final argument if set true will return a list of all the hit objects, but if it is set to false the program will break after the first object is hit </summary>
    public static raycastInfo raycast(position p1, position p2, raycastParameters flags, Time t, int defaultRadius, bool returnAllHit)
    {
        List<planet> hitPlanets = new List<planet>();
        List<facility> hitFacs = new List<facility>();
        List<satellite> hitSats = new List<satellite>();
        bool hit = false;

        double p1p2Distance = position.distance(p1, p2);

        if ((flags & raycastParameters.planet) == raycastParameters.planet) {
            foreach (planet p in master.allPlanets) {
                if (position.distance(p1, p.requestPosition(t)) < p1p2Distance) {
                    double distanceFromObj = raycastMath(p1, p2, p.requestPosition(t));
                    if (distanceFromObj <= p.radius)
                    {
                        if (returnAllHit)
                        {
                            hit = true;
                            hitPlanets.Add(p);
                        }
                        else
                        {
                            hitPlanets.Add(p);
                            hit = true;
                            break;
                        }
                    }
                }
            }
        }

        if ((flags & raycastParameters.satellite) == raycastParameters.satellite)
        {
            foreach (satellite sat in master.allSatellites)
            {
                if (position.distance(p1, sat.requestPosition(t)) < p1p2Distance)
                {
                    double distanceFromObj = raycastMath(p1, p2, sat.requestPosition(t));
                    if (distanceFromObj <= defaultRadius)
                    {
                        if (returnAllHit)
                        {
                            hit = true;
                            hitSats.Add(sat);
                        }
                        else
                        {
                            hitSats.Add(sat);
                            hit = true;
                            break;
                        }
                    }
                }
            }
        }


        if ((flags & raycastParameters.facility) == raycastParameters.facility)
        {
            foreach (facility f in master.allFacilites)
            {
                double alt = 0;
                position fac = f.facParent.geoOnPlanet(f.geo, alt) + f.facParent.requestPosition(t);
                if (position.distance(p1, fac) < p1p2Distance)
                {
                    double distanceFromObj = raycastMath(p1, p2, fac);
                    if (distanceFromObj <= defaultRadius)
                    {
                        if (returnAllHit)
                        {
                            hit = true;
                            hitFacs.Add(f);
                        }
                        else
                        {
                            hitFacs.Add(f);
                            hit = true;
                            break;
                        }
                    }
                }
            }
        }
        raycastInfo raycastInfo = new raycastInfo(hit, hitPlanets, hitSats, hitFacs);
        return raycastInfo;
    }

    /// <summary> This is the like the above raycast function but the second argument is the direction of the desired vector </summary>
    public static raycastInfo raycastVector(position p1, position vector, raycastParameters flags, Time t, int defaultRadius, bool returnAllHit)
    {
        List<planet> hitPlanets = new List<planet>();
        List<facility> hitFacs = new List<facility>();
        List<satellite> hitSats = new List<satellite>();
        bool hit = false;

        position p2 = new position(
        double.MaxValue / 4.0 + vector.x,
         double.MaxValue / 4.0 + vector.y,
          double.MaxValue / 4.0 + vector.z);

        double p1p2Distance = position.distance(p1, p2);

        if ((flags & raycastParameters.planet) == raycastParameters.planet)
        {
            foreach (planet p in master.allPlanets)
            {
                if (position.distance(p1, p.requestPosition(t)) > p1p2Distance)
                {
                    double distanceFromObj = raycastMath(p1, p2, p.requestPosition(t));
                    if (distanceFromObj <= p.radius)
                    {
                        if (returnAllHit)
                        {
                            hit = true;
                            hitPlanets.Add(p);
                        }
                        else
                        {
                            hitPlanets.Add(p);
                            hit = true;
                            break;
                        }
                    }
                }
            }
        }

        if ((flags & raycastParameters.satellite) == raycastParameters.satellite)
        {
            foreach (satellite sat in master.allSatellites)
            {
                if (position.distance(p1, sat.requestPosition(t)) > p1p2Distance)
                {
                    double distanceFromObj = raycastMath(p1, p2, sat.requestPosition(t));
                    if (distanceFromObj <= defaultRadius)
                    {
                        if (returnAllHit)
                        {
                            hit = true;
                            hitSats.Add(sat);
                        }
                        else
                        {
                            hitSats.Add(sat);
                            hit = true;
                            break;
                        }
                    }
                }
            }
        }

        if ((flags & raycastParameters.facility) == raycastParameters.facility)
        {
            foreach (facility f in master.allFacilites)
            {
                double alt = 0;
                position fac = f.facParent.geoOnPlanet(f.geo, alt) + f.facParent.requestPosition(t);
                if (position.distance(p1, fac) > p1p2Distance)
                {
                    double distanceFromObj = raycastMath(p1, p2, fac);
                    if (distanceFromObj <= defaultRadius)
                    {
                        if (returnAllHit)
                        {
                            hit = true;
                            hitFacs.Add(f);
                        }
                        else
                        {
                            hitFacs.Add(f);
                            hit = true;
                            break;
                        }
                    }
                }
            }
        }
        raycastInfo raycastInfo = new raycastInfo(hit, hitPlanets, hitSats, hitFacs);
        return raycastInfo;
    }
}
