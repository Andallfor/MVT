using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class linkBudgeting
{
  public linkBudgeting(Dictionary<string, bool> users, Dictionary<string, bool> providers)
  {

    List<string> connections = new List<string>();
    //satellite = false
    //facility = True

    foreach(KeyValuePair<string, bool> provider in providers)
    {
      foreach(KeyValuePair<string, bool> user in users)
      {
        if (provider.Value == false)
        {
          if(user.Value == false)
          {
            satellite _provider = master.allSatellites.Find(x => x.name == provider.Key);
            satellite _user = master.allSatellites.Find(x => x.name == user.Key);
            if (visibility.raycast(_provider.pos, _user.pos, visibility.raycastParameters.all, 1, false).hit == false)
            {
              connections.Add(master.time + ": " + provider.Key + " to " + user.Key);
            }
          }
          else
          {
            satellite _provider = master.allSatellites.Find(x => x.name == provider.Key);
            facility _user = master.allFacilites.Find(x => x.name == user.Key);
            //inset altitude here later
            position fac = _user.facParent.geoOnPlanet(_user.geo, 0);
            if (visibility.raycast(_provider.pos, fac, visibility.raycastParameters.all, 1, false).hit == false)
            {
              connections.Add(master.time + ": " + provider.Key + " to " + user.Key);
            }
          }
        }
        else
        {
          if(user.Value == false)
          {
            facility _provider = master.allFacilites.Find(x => x.name == provider.Key);
            satellite _user = master.allSatellites.Find(x => x.name == user.Key);
            //inset altitude here later
            position fac = _provider.facParent.geoOnPlanet(_provider.geo, 0);
            if (visibility.raycast(fac, _user.pos, visibility.raycastParameters.all, 1, false).hit == false)
            {
              connections.Add(master.time + ": " + provider.Key + " to " + user.Key);
            }
          }
          else
          {
            facility _provider = master.allFacilites.Find(x => x.name == provider.Key);
            facility _user = master.allFacilites.Find(x => x.name == user.Key);
            //inset altitude here later
            position fac2 = _user.facParent.geoOnPlanet(_user.geo, 0);
            position fac1 = _provider.facParent.geoOnPlanet(_provider.geo, 0);
            if (visibility.raycast(fac1, fac2, visibility.raycastParameters.all, 1, false).hit == false)
            {
              connections.Add(master.time + ": " + provider.Key + " to " + user.Key);
            }
          }
        }
      }
    }
    System.IO.File.WriteAllLines("Connections.txt", connections);
    return;
  }
}
