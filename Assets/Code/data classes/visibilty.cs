using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.IO;

public struct dynamicLinkOptions {
	public Action<Dictionary<(string user, string provider), (List<double> time, List<double> dist)>> callback;
	public bool debug;
	public string outputPath;
}

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

public static class visibility
{
	public static bool currentlyRunningTerrainRaycast = false;
	public static IEnumerator raycastTerrain(List<object> users, List<object> providers, double start, double end, double increment, dynamicLinkOptions options) {
		general.blockMainLoop = true;
		currentlyRunningTerrainRaycast = true;
		// please just use a struct or something
		Dictionary<(string user, string provider), (List<double> time, List<double> dist)> output = new Dictionary<(string user, string provider), (List<double> time, List<double> dist)>();
		Dictionary<string, Transform> u = new Dictionary<string, Transform>();
		Dictionary<string, Transform> p = new Dictionary<string, Transform>();
        Dictionary<string, Func<Time, bool>> existance = new Dictionary<string, Func<Time, bool>>();

		Dictionary<(string user, string provider), LineRenderer> lrs = new Dictionary<(string user, string provider), LineRenderer>();
		GameObject lrPrefab = Resources.Load("Prefabs/simpleLine") as GameObject;

		formatObjects(users, ref u, ref existance);
		formatObjects(providers, ref p, ref existance);

		foreach (string us in u.Keys) {
			foreach (var ps in p.Keys) {
				List<double> time = new List<double>();
				List<double> dist = new List<double>();
				output[(us, ps)] = (time, dist);
				if (options.debug) {
					LineRenderer lr = GameObject.Instantiate(lrPrefab).GetComponent<LineRenderer>();
					lr.positionCount = 0;
					lr.positionCount = 2;
					lrs[(us, ps)] = lr;
				}
			}
		}

		double checkpoint = master.time.julian;

		// reset time
		master.time.addJulianTime(start - master.time.julian - increment);

		StringBuilder sb = new StringBuilder();

		while (master.time.julian < end) {
			master.time.addJulianTime(increment);
			master.requestPositionUpdate();

			foreach (var ukvp in u) {
				foreach (var pkvp in p) {
					if (ukvp.Key == pkvp.Key) continue;
                    if (!existance[ukvp.Key](master.time) || !existance[pkvp.Key](master.time)) continue;
					Ray r = new Ray(ukvp.Value.position, pkvp.Value.position - ukvp.Value.position);
					bool hit = true;
					if (!Physics.Raycast(r, 100000, LayerMask.GetMask("terrain", "planet"))) {
						var results = output[(ukvp.Key, pkvp.Key)];
						results.time.Add(master.time.julian);
						results.dist.Add(Vector3.Distance(ukvp.Value.position, pkvp.Value.position) * master.scale);

						hit = false;

						if (options.outputPath != default(string)) sb.AppendLine($"{master.time.ToString()}: {ukvp.Key} to {pkvp.Key}");
					}

					if (options.debug) {
						LineRenderer lr = lrs[(ukvp.Key, pkvp.Key)];
						lr.SetPositions(new Vector3[2] {ukvp.Value.position, pkvp.Value.position});
						Color c = hit ? Color.red : Color.green;
						lr.startColor = c;
						lr.endColor = c;
					}
				}
			}

			yield return null;
		}

		master.time.addJulianTime(checkpoint - master.time.julian);
		master.requestPositionUpdate();

		if (options.debug) {
			foreach (LineRenderer lr in lrs.Values) GameObject.Destroy(lr.gameObject);
		}

		if (options.outputPath != default(string)) File.WriteAllText(options.outputPath, sb.ToString());

		if (options.callback != null) options.callback(output);

		general.blockMainLoop = false;
		currentlyRunningTerrainRaycast = false;
	}

	private static void formatObjects(List<object> objs, ref Dictionary<string, Transform> dict, ref Dictionary<string, Func<Time, bool>> dict2) {
		foreach (object obj in objs) {
			if (obj is planet) {
				planet p = (planet) obj;
				dict[p.name] = p.representation.gameObject.transform;
                dict2[p.name] = p.positions.exists;
			}
			else if (obj is satellite) {
				satellite s = (satellite) obj;
				dict[s.name] = s.representation.gameObject.transform;
                dict2[s.name] = s.positions.exists;
            }
			else if (obj is facility) {
				facility f = (facility) obj;
				dict[f.name] = f.representation.gameObject.transform;
                dict2[f.name] = f.exists;
            } else {
                Debug.LogWarning($"Warning: Unable to find {obj}");
            }
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

        return Math.Tan(position.dotProductTheta(vectorOBJ, vector2)) * position.distance(position1, obj);
    }

    /// <summary> This is the main raycast function for our program and it takes in 5 arguments. The final argument if set true will return a list of all the hit objects, but if it is set to false the program will break after the first object is hit </summary>
    public static (bool, double) dynamicLinkVisibility(position p1, position p2, Time t, int defaultRadius)
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
        
        
        if (hit == false) { return (false, p1p2Distance); }
        else { return (true, 0); }
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
                position fac = f.facParent.rotateLocalGeo(f.geo, alt) + f.facParent.requestPosition(t);
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
                position fac = f.facParent.rotateLocalGeo(f.geo, alt) + f.facParent.requestPosition(t);
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
