using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class linkBudgeting
{

  public static void accessCalls()
  {
    //satellite = false
    //facility = True

    foreach(KeyValuePair<string, (bool, double, double)> provider in master.providers)
    {
      foreach(KeyValuePair<string, (bool, double, double)> user in master.users)
      {
        if (((master.time.julian > provider.Value.Item2 & master.time.julian < provider.Value.Item3) & (master.time.julian > user.Value.Item2 & master.time.julian < user.Value.Item3)) == true)
        {
          if (user.Key != provider.Key)
          {

            switch (provider.Value.Item1)
            {
              case false:

                if (user.Value.Item1 == false)
                {
                  satellite _provider = master.allSatellites.Find(x => x.name == provider.Key);
                  satellite _user = master.allSatellites.Find(x => x.name == user.Key);
                  if (visibility.raycast(_provider.pos, _user.pos, visibility.raycastParameters.planet, 1, false).hit == false)
                  {
                    master.connections.Add(master.time + ": " + provider.Key + " to " + user.Key);
                    Debug.Log(master.time + ": " + provider.Key + " to " + user.Key);
                  }
                }

                if (user.Value.Item1)
                {
                  satellite _provider = master.allSatellites.Find(x => x.name == provider.Key);
                  facility _user = master.allFacilites.Find(x => x.name == user.Key);
                  //inset altitude here later
                  position fac = _user.facParent.geoOnPlanet(_user.geo, 0);
                  if (visibility.raycast(_provider.pos, fac, visibility.raycastParameters.planet, 1, false).hit == false)
                  {
                    master.connections.Add(master.time + ": " + provider.Key + " to " + user.Key);
                  }
                }
              break;


              case true:

                if (user.Value.Item1 == false)
                {
                  facility _provider = master.allFacilites.Find(x => x.name == provider.Key);
                  satellite _user = master.allSatellites.Find(x => x.name == user.Key);
                  //inset altitude here later
                  position fac = _provider.facParent.geoOnPlanet(_provider.geo, 0);
                  if (visibility.raycast(fac, _user.pos, visibility.raycastParameters.planet, 1, false).hit == false)
                  {
                    master.connections.Add(master.time + ": " + provider.Key + " to " + user.Key);
                    Debug.Log(master.time + ": " + provider.Key + " to " + user.Key);
                  }
                }

                if (user.Value.Item1)
                {
                  facility _provider = master.allFacilites.Find(x => x.name == provider.Key);
                  facility _user = master.allFacilites.Find(x => x.name == user.Key);
                  //inset altitude here later
                  position fac2 = _user.facParent.geoOnPlanet(_user.geo, 0);
                  position fac1 = _provider.facParent.geoOnPlanet(_provider.geo, 0);
                  if (visibility.raycast(fac1, fac2, visibility.raycastParameters.planet, 1, false).hit == false)
                  {
                    master.connections.Add(master.time + ": " + provider.Key + " to " + user.Key);
                  }
                }
              break;
            }
          }
        }
      }
    }
    return;
  }
}
