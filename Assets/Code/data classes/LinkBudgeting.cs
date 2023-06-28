using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public static class linkBudgeting
{
    public static Dictionary<string, (bool, double, double)> users = new Dictionary<string, (bool, double, double)>();
    public static Dictionary<string, (bool, double, double)> providers = new Dictionary<string, (bool, double, double)>();

    public static void accessCalls(string path, double start, double stop)
    {
		Debug.Log("Starting access calls");
		Time time = new Time(2460810.5);
		List<string> connections = new List<string>();
        //satellite = false
        //facility = True

		Task t = new Task(() => {
			while (time.julian < 2460836.5) {
				foreach (KeyValuePair<string, (bool t, double start, double end)> provider in providers) {
					foreach (KeyValuePair<string, (bool t, double start, double end)> user in users) {
						if (user.Key == provider.Key) continue;

						if (((time.julian > provider.Value.Item2 & time.julian < provider.Value.Item3) & (time.julian > user.Value.Item2 & time.julian < user.Value.Item3))) {
							position pp = new position(0, 0, 0);
							position up = new position(0, 0, 0);

							if (!provider.Value.t) pp = master.allSatellites.Find(x => x.name == provider.Key).requestPosition(time);
							else {
								facility _provider = master.allFacilities.Find(x => x.name == provider.Key);
								pp = _provider.facParent.rotateLocalGeo(_provider.geo, 0) + _provider.facParent.requestPosition(time);
							}

							if (!user.Value.t) up = master.allSatellites.Find(x => x.name == user.Key).requestPosition(time);
							else {
								facility _user = master.allFacilities.Find(x => x.name == user.Key);
								up = _user.facParent.rotateLocalGeo(_user.geo, 0) + _user.facParent.pos;
							}

							if (pp != new position(0, 0, 0) && up != new position(0, 0, 0)) {
								if (!visibility.raycast(pp, up, raycastParameters.planet, time, 1, false).hit) connections.Add(time + ": " + provider.Key + " to " + user.Key);
							}
						}
					}
				}

				time.addJulianTime(0.0006944444);
			}

			File.WriteAllLines(path, connections);

			Debug.Log("Access calls finished");
		});

		t.Start();
    }

	public static Dictionary<(string, string), (List<double>, List<double>)> dynamicLink()
	{
		Dictionary<(string, string), (List<double>, List<double>)> Dictionary = new Dictionary<(string, string), (List<double>, List<double>)>();

		foreach (KeyValuePair<string, (bool t, double start, double end)> provider in providers)
		{
			foreach (KeyValuePair<string, (bool t, double start, double end)> user in users)
			{
				Time time = new Time(2460806.5);
				List<double> Time = new List<double>();
				List<double> distance = new List<double>();

				while (time.julian < 2460836.5)
				{
					if (user.Key == provider.Key) continue;

					if (((time.julian > provider.Value.Item2 & time.julian < provider.Value.Item3) & (time.julian > user.Value.Item2 & time.julian < user.Value.Item3)))
					{
						position pp = new position(0, 0, 0);
						position up = new position(0, 0, 0);

						if (!provider.Value.t) pp = master.allSatellites.Find(x => x.name == provider.Key).requestPosition(time);
						else
						{
							facility _provider = master.allFacilities.Find(x => x.name == provider.Key);
							pp = _provider.facParent.rotateLocalGeo(_provider.geo, 0) + _provider.facParent.requestPosition(time);
						}

						if (!user.Value.t) up = master.allSatellites.Find(x => x.name == user.Key).requestPosition(time);
						else {
							facility _user = master.allFacilities.Find(x => x.name == user.Key);
							up = _user.facParent.rotateLocalGeo(_user.geo, 0) + _user.facParent.pos;
						}

						if (pp != new position(0, 0, 0) && up != new position(0, 0, 0))
						{
							(bool, double) link = visibility.dynamicLinkVisibility(pp, up, time, 1);

							if (link.Item1 == false)
							{
								Time.Add(time.julian);
								distance.Add(link.Item2);
							}
						}
					}

					time.addJulianTime(0.0006944444);
				}

				Dictionary.Add((user.Key, provider.Key), (Time, distance));
			}
		}
		return Dictionary;
	}
}
