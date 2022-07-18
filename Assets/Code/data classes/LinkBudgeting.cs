using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class linkBudgeting
{

  public static Dictionary<string, (bool, double, double)> users = new Dictionary<string, (bool, double, double)>();
  public static Dictionary<string, (bool, double, double)> providers = new Dictionary<string, (bool, double, double)>();


  public static void accessCalls(string path)
   {
     Debug.Log("Starting access calls");
		Time time = new Time(2460810.5);
		List<string> connections = new List<string>();
        //satellite = false
        //facility = True

		Task t = new Task(() =>
    {
			while (time.julian < 2460836.5)
      {
				foreach (KeyValuePair<string, (bool t, double start, double end)> provider in providers)
        {
					foreach (KeyValuePair<string, (bool t, double start, double end)> user in users)
          {
						if (user.Key == provider.Key) continue;

						if (((time.julian > provider.Value.Item2 & time.julian < provider.Value.Item3) & (time.julian > user.Value.Item2 & time.julian < user.Value.Item3)))
            {
							position pp = new position(0, 0, 0);
							position up = new position(0, 0, 0);

							if (!provider.Value.t) pp = master.allSatellites.Find(x => x.name == provider.Key).requestPosition(time);
							else
              {
								facility _provider = master.allFacilites.Find(x => x.name == provider.Key);
								pp = _provider.facParent.geoOnPlanet(_provider.geo, 0) + _provider.facParent.requestPosition(time);
							}

							if (!user.Value.t) up = master.allSatellites.Find(x => x.name == user.Key).requestPosition(time);
							else
              {
								facility _user = master.allFacilites.Find(x => x.name == user.Key);
								up = _user.facParent.geoOnPlanet(_user.geo, 0) + _user.facParent.pos;
							}

							if (pp != new position(0, 0, 0) && up != new position(0, 0, 0))
              {
								if (!visibility.raycast(pp, up, visibility.raycastParameters.planet, time, 1, false).hit) connections.Add(time + ": " + provider.Key + " to " + user.Key);
							}
						}
					}
				}
		t.Start();
    }
  }
}
