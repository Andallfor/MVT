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

  private static double raycastMath(position position1, position position2, position obj)
  {
    double distance1 = position.distance(position1, obj);
    double distancePercent = distance1 / position.distance(position1, position2);

    position pointOnLine = new position((position1.x + position2.x) * distancePercent,
    (position1.y + position2.y) * distancePercent,
    (position1.z + position2.z) * distancePercent);

    double distanceFromObj = position.distance(pointOnLine, obj);

    return distanceFromObj;

  }
  /// <summary> This is the main raycast function for our program and it takes in 5 arguments. The final argument if set true will return a list of all the hit objects, but if it is set to false the program will break after the first object is hit <summary> ///
  public static raycastInfo raycast(position p1, position p2, raycastParameters flags, int defaultRadius, bool returnAllHit)
  {

    List<planet> hitPlanets = new List<planet>();
    List<facility> hitFacs = new List<facility>();
    List<satellite> hitSats = new List<satellite>();
    bool hit = false;

    double p1p2Distance = position.distance(p1,p2);

    if ((flags & raycastParameters.planet) == raycastParameters.planet)
    {
      foreach(planet p in master.allPlanets)
      {
        if (position.distance(p1, p.pos) > p1p2Distance)
        {
          double distanceFromObj = raycastMath(p1, p2, p.pos);
          if (distanceFromObj <= p.radius)
          {
            if (returnAllHit == true)
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
      foreach(satellite sat in master.allSatellites)
      {
        if (position.distance(p1, sat.pos) > p1p2Distance)
        {
          double distanceFromObj = raycastMath(p1, p2, sat.pos);
          if (distanceFromObj <= defaultRadius)
          {
            if (returnAllHit == true)
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
      foreach(facility f in master.allFacilites)
      {
        double alt = 0;
        position fac = f.facParent.geoOnPlanet(f.geo, alt);
        if (position.distance(p1, fac) > p1p2Distance)
        {
          double distanceFromObj = raycastMath(p1, p2, fac);
          if (distanceFromObj <= defaultRadius)
          {
            if (returnAllHit == true)
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

/// <summary> This is the like the above raycast function but the second argument is the direction of the desired vector <summary> ///
  public static raycastInfo raycastVector(position p1, position vector, raycastParameters flags, int defaultRadius, bool returnAllHit)
  {

    List<planet> hitPlanets = new List<planet>();
    List<facility> hitFacs = new List<facility>();
    List<satellite> hitSats = new List<satellite>();
    bool hit = false;

    position p2 = new position(
    double.MaxValue/4.0 + vector.x,
     double.MaxValue/4.0 + vector.y,
      double.MaxValue/4.0 + vector.z);

    double p1p2Distance = position.distance(p1,p2);

    if ((flags & raycastParameters.planet) == raycastParameters.planet)
    {
      foreach(planet p in master.allPlanets)
      {
        if (position.distance(p1, p.pos) > p1p2Distance)
        {
          double distanceFromObj = raycastMath(p1, p2, p.pos);
          if (distanceFromObj <= p.radius)
          {
            if (returnAllHit == true)
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
      foreach(satellite sat in master.allSatellites)
      {
        if (position.distance(p1, sat.pos) > p1p2Distance)
        {
          double distanceFromObj = raycastMath(p1, p2, sat.pos);
          if (distanceFromObj <= defaultRadius)
          {
            if (returnAllHit == true)
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
      foreach(facility f in master.allFacilites)
      {
        double alt = 0;
        position fac = f.facParent.geoOnPlanet(f.geo, alt);
        if (position.distance(p1, fac) > p1p2Distance)
        {
          double distanceFromObj = raycastMath(p1, p2, fac);
          if (distanceFromObj <= defaultRadius)
          {
            if (returnAllHit == true)
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
