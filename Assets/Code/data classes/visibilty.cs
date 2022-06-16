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

  public static double distance(position p1, position p2)
  {
     double distance = Math.Sqrt(Math.Pow((p1.x - p2.x) ,2) + Math.Pow((p1.y - p2.y) ,2) + Math.Pow((p1.z - p2.z), 2));
     return distance;
  }

  public static double raycastMath(position position1, position position2, position obj)
  {
    double distance1 = distance(position1, obj);
    double distancePercent = distance1 / distance(position1, position2);

    position pointOnLine = new position((position1.x + position2.x) * distancePercent,
    (position1.y + position2.y) * distancePercent,
    (position1.z + position2.z) * distancePercent);

    double distanceFromObj = distance(pointOnLine, obj);

    return distanceFromObj;

  }

  //double.max

  public static raycastInfo raycastHit(position p1, position p2, raycastParameters flags, int defaultRadius)
  {
    // 100 indicates planet search
    // 10 indicates a satellite search
    // 1 indicates a facility search

    List<planet> hitPlanets = new List<planet>();
    List<facility> hitFacs = new List<facility>();
    List<satellite> hitSats = new List<satellite>();
    bool hit = false;

    if ((flags & raycastParameters.planet) == raycastParameters.planet)
    {
      foreach(planet p in master.allPlanets)
      {
        if (distance(p1, p.pos) > distance(p1,p2))
        {
          double distanceFromObj = raycastMath(p1, p2, p.pos);
          if (distanceFromObj <= p.radius)
          {
            hit = true;
            hitPlanets.Add(p);
          }
        }
      }
    }

    if ((flags & raycastParameters.satellite) == raycastParameters.satellite)
    {
      foreach(satellite sat in master.allSatellites)
      {
        if (distance(p1, sat.pos) > distance(p1,p2))
        {
          double distanceFromObj = raycastMath(p1, p2, sat.pos);
          if (distanceFromObj <= defaultRadius)
          {
            hit = true;
            hitSats.Add(sat);
          }
        }
      }
    }

    if ((flags & raycastParameters.facility) == raycastParameters.facility)
    {
      foreach(facility f in master.allFacilites)
      {
        //alt is set to 0 now but eventually when altitudes are implemented in geographic positions they will be added here too
        double alt = 0;
        position fac = f.facParent.geoOnPlanet(f.geo, alt);
        if (distance(p1, fac) > distance(p1,p2))
        {
          double distanceFromObj = raycastMath(p1, p2, fac);
          if (distanceFromObj <= defaultRadius)
          {
            hit = true;
            hitFacs.Add(f);
          }
        }
      }
    }

    raycastInfo raycastInfo = new raycastInfo(hit, hitPlanets, hitSats, hitFacs);
    return raycastInfo;
  }

  public static raycastInfo raycast(position p1, position p2, raycastParameters flags, int defaultRadius)
  {
    // 100 indicates planet search
    // 10 indicates a satellite search
    // 1 indicates a facility search

    List<planet> hitPlanets = new List<planet>();
    List<facility> hitFacs = new List<facility>();
    List<satellite> hitSats = new List<satellite>();
    bool hit = false;

    if ((flags & raycastParameters.planet) == raycastParameters.planet)
    {
      foreach(planet p in master.allPlanets)
      {
        if (distance(p1, p.pos) > distance(p1,p2))
        {
          double distanceFromObj = raycastMath(p1, p2, p.pos);
          if (distanceFromObj <= p.radius)
          {
            hitPlanets.Add(p);
            hit = true;
            break;
          }
        }
      }
    }

    if ((flags & raycastParameters.satellite) == raycastParameters.satellite)
    {
      foreach(satellite sat in master.allSatellites)
      {
        if (distance(p1, sat.pos) > distance(p1,p2))
        {
          double distanceFromObj = raycastMath(p1, p2, sat.pos);
          if (distanceFromObj <= defaultRadius)
          {
            hitSats.Add(sat);
            hit = true;
            break;
          }
        }
      }
    }

    if ((flags & raycastParameters.facility) == raycastParameters.facility)
    {
      foreach(facility f in master.allFacilites)
      {
        //alt is set to 0 now but eventually when altitudes are implemented in geographic positions they will be added here too
        double alt = 0;
        position fac = f.facParent.geoOnPlanet(f.geo, alt);
        if (distance(p1, fac) > distance(p1,p2))
        {
          double distanceFromObj = raycastMath(p1, p2, fac);
          if (distanceFromObj <= defaultRadius)
          {
            hitFacs.Add(f);
            hit = true;
            break;
          }
        }
      }
    }

    raycastInfo raycastInfo = new raycastInfo(hit, hitPlanets, hitSats, hitFacs);
    return raycastInfo;
  }

//vector functions, the second number is the vector
public static raycastInfo raycastvector(position p1, position vector, raycastParameters flags, int defaultRadius)
{
  // 100 indicates planet search
  // 10 indicates a satellite search
  // 1 indicates a facility search

  List<planet> hitPlanets = new List<planet>();
  List<facility> hitFacs = new List<facility>();
  List<satellite> hitSats = new List<satellite>();
  bool hit = false;

  position p2 = new position(
  double.MaxValue/4 + vector.x,
   double.MaxValue/4 + vector.y,
    double.MaxValue/4 + vector.z);

  if ((flags & raycastParameters.planet) == raycastParameters.planet)
  {
    foreach(planet p in master.allPlanets)
    {
      if (distance(p1, p.pos) > distance(p1,p2))
      {
        double distanceFromObj = raycastMath(p1, p2, p.pos);
        if (distanceFromObj <= p.radius)
        {
          hitPlanets.Add(p);
          hit = true;
          break;
        }
      }
    }
  }

  if ((flags & raycastParameters.satellite) == raycastParameters.satellite)
  {
    foreach(satellite sat in master.allSatellites)
    {
      if (distance(p1, sat.pos) > distance(p1,p2))
      {
        double distanceFromObj = raycastMath(p1, p2, sat.pos);
        if (distanceFromObj <= defaultRadius)
        {
          hitSats.Add(sat);
          hit = true;
          break;
        }
      }
    }
  }

  if ((flags & raycastParameters.facility) == raycastParameters.facility)
  {
    foreach(facility f in master.allFacilites)
    {
      //alt is set to 0 now but eventually when altitudes are implemented in geographic positions they will be added here too
      double alt = 0;
      position fac = f.facParent.geoOnPlanet(f.geo, alt);
      if (distance(p1, fac) > distance(p1,p2))
      {
        double distanceFromObj = raycastMath(p1, p2, fac);
        if (distanceFromObj <= defaultRadius)
        {
          hitFacs.Add(f);
          hit = true;
          break;
        }
      }
    }
  }

  raycastInfo raycastInfo = new raycastInfo(hit, hitPlanets, hitSats, hitFacs);
  return raycastInfo;
}

public static raycastInfo raycastvectorHit(position p1, position vector, raycastParameters flags, int defaultRadius)
{
  // 100 indicates planet search
  // 10 indicates a satellite search
  // 1 indicates a facility search

  List<planet> hitPlanets = new List<planet>();
  List<facility> hitFacs = new List<facility>();
  List<satellite> hitSats = new List<satellite>();
  bool hit = false;

  position p2 = new position(
  double.MaxValue/4 + vector.x,
   double.MaxValue/4 + vector.y,
    double.MaxValue/4 + vector.z);

  if ((flags & raycastParameters.planet) == raycastParameters.planet)
  {
    foreach(planet p in master.allPlanets)
    {
      if (distance(p1, p.pos) > distance(p1,p2))
      {
        double distanceFromObj = raycastMath(p1, p2, p.pos);
        if (distanceFromObj <= p.radius)
        {
          hit = true;
          hitPlanets.Add(p);
        }
      }
    }
  }

  if ((flags & raycastParameters.satellite) == raycastParameters.satellite)
  {
    foreach(satellite sat in master.allSatellites)
    {
      if (distance(p1, sat.pos) > distance(p1,p2))
      {
        double distanceFromObj = raycastMath(p1, p2, sat.pos);
        if (distanceFromObj <= defaultRadius)
        {
          hit = true;
          hitSats.Add(sat);
        }
      }
    }
  }

  if ((flags & raycastParameters.facility) == raycastParameters.facility)
  {
    foreach(facility f in master.allFacilites)
    {
      //alt is set to 0 now but eventually when altitudes are implemented in geographic positions they will be added here too
      double alt = 0;
      position fac = f.facParent.geoOnPlanet(f.geo, alt);
      if (distance(p1, fac) > distance(p1,p2))
      {
        double distanceFromObj = raycastMath(p1, p2, fac);
        if (distanceFromObj <= defaultRadius)
        {
          hit = true;
          hitFacs.Add(f);
        }
      }
    }
  }

  raycastInfo raycastInfo = new raycastInfo(hit, hitPlanets, hitSats, hitFacs);
  return raycastInfo;
  }
}
