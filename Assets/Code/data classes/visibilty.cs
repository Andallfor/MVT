using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class visibility
{

  public bool raycast(position position1, position position2, int flags, int defaultRadius = 1)
  {
    // 100 indicates planet search
    // 10 indicates a satellite search
    // 1 indicates a facility search

    int c = 0;
    int x = flags;
    bool check = false;


    if (x - 100 >= 0)
    {
      x = x - 100;
      foreach (planet p in master.allPlanets)
      {
        double distance1 = Math.Sqrt(Math.Pow((position1.x - p.pos.x) ,2) + Math.Pow((position1.y - p.pos.y) ,2) + Math.Pow((position1.z - p.pos.z), 2));
        double distancePercent = distance1 / Math.Sqrt(Math.Pow((position1.x - position2.x), 2) + Math.Pow((position1.y - position2.y), 2) + Math.Pow((position1.z - position2.z), 2));

        double pointOnLineX = (position1.x + ((position2.x - position1.x) * distancePercent));
        double pointOnLineY = (position1.y + ((position2.y - position1.y) * distancePercent));
        double pointOnLineZ = (position1.z + ((position2.z - position1.z) * distancePercent));

        double inRadius = Math.Sqrt(Math.Pow((pointOnLineX - p.pos.x), 2) + Math.Pow((pointOnLineY - p.pos.y), 2) + Math.Pow((pointOnLineX - p.pos.z), 2));

        if (inRadius < p.radius)
        {
          c = c + 1;
        }
        else
        {
          c = c + 0;
        }
      }
    }

    if (x - 10 >= 0)
    {
      x = x - 10;

      foreach (satellite sat in master.allSatellites)
      {
        double distance1 = Math.Sqrt(Math.Pow((position1.x - sat.pos.x), 2) + Math.Pow((position1.y - sat.pos.y), 2) + Math.Pow((position1.z - sat.pos.z), 2));
        double distancePercent = distance1 / Math.Sqrt(Math.Pow((position1.x - position2.x), 2) + Math.Pow((position1.y - position2.y), 2) + Math.Pow((position1.z - position2.z), 2));

        double pointOnLineX = (position1.x + ((position2.x - position1.x) * distancePercent));
        double pointOnLineY = (position1.y + ((position2.y - position1.y) * distancePercent));
        double pointOnLineZ = (position1.z + ((position2.z - position1.z) * distancePercent));

        double inRadius = Math.Sqrt(Math.Pow((pointOnLineX - sat.pos.x), 2) + Math.Pow((pointOnLineY - sat.pos.y), 2) + Math.Pow((pointOnLineZ - sat.pos.z), 2));

        if (inRadius < 1)
        {
          c = c + 1;
        }
        else
        {
          c = c + 0;
        }
      }
    }

    if (x - 1 >= 0)
    {
      x = x - 1;

      foreach (facility f in master.allFacilites)
      {

        //for now ill leave this as zero but when alt is added into geographic or the system in general ill put it in.
        position fac = f.facParent.geoOnPlanet(f.geo, 0);
        double distance1 = Math.Sqrt(Math.Pow((position1.x - fac.x),2) + Math.Pow((position1.y - fac.y), 2) + Math.Pow((position1.z - fac.z), 2));
        double distancePercent = distance1 / Math.Sqrt(Math.Pow((position1.x - position2.x), 2) + Math.Pow((position1.y - position2.y), 2) + Math.Pow((position1.z - position2.z), 2));

        double pointOnLineX = (position1.x + ((position2.x - position1.x) * distancePercent));
        double pointOnLineY = (position1.y + ((position2.y - position1.y) * distancePercent));
        double pointOnLineZ = (position1.z + ((position2.z - position1.z) * distancePercent));

        double inRadius = Math.Sqrt(Math.Pow((pointOnLineX - fac.x), 2) + Math.Pow((pointOnLineY - fac.y), 2) + Math.Pow((pointOnLineZ - fac.z), 2));

        if (inRadius < 1)
        {
          c = c + 1;
        }
        else
        {
          c = c + 0;
        }
      }
    }


    if (c == 0)
    {
      check = false;
    }
    else
    {
      check = true;
    }

    //u
    return check;

  }
}
