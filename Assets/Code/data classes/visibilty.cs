using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;

public struct dynamicLinkOptions {
	public Action<Dictionary<(string user, string provider), (List<double> time, List<double> dist)>> callback;
	public bool debug, blocking;
	public string outputPath;
}

public static class visibility
{
    public static bool currentlyRunningTerrainRaycast = false;
    public static void raycastTerrain(List<object> users, List<object> providers, double start, double end, double increment, dynamicLinkOptions options, bool report)
    {
        if (controller.useTerrainVisibility) Debug.LogWarning("Warning! You are trying to use visiblity with terrain. This forces us to load everything- be careful when increasing resolution!");
        if (options.blocking) raycastTerrainBlock(users, providers, start, end, increment, options, report);
        else controller.self.StartCoroutine(raycastTerrainCoro(users, providers, start, end, increment, options, report));
    }

    public static async void raycastTerrainBlock(List<object> users, List<object> providers, double start, double end, double increment, dynamicLinkOptions options, bool report)
    {
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

        foreach (string us in u.Keys)
        {
            foreach (var ps in p.Keys)
            {
                List<double> time = new List<double>();
                List<double> dist = new List<double>();
                output[(us, ps)] = (time, dist);
                if (options.debug)
                {
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

        loadingController.start(new Dictionary<float, string>() { { 0, "Generating..." } });

        await Task.Delay(1000);

        int iterations = (int)Math.Ceiling((end - master.time.julian) / increment);
        int updateCount = iterations / 100;
        int index = 0;

        while (master.time.julian < end)
        {
            master.time.addJulianTime(increment);
            master.requestPositionUpdate();

						bool hit;

            foreach (var ukvp in u)
            {
                foreach (var pkvp in p)
                {
                    if (ukvp.Key == pkvp.Key) continue;
                    if (!existance[ukvp.Key](master.time) || !existance[pkvp.Key](master.time)) continue;
                    if (!Physics.Linecast(ukvp.Value.position, pkvp.Value.position, LayerMask.GetMask("terrain", "planet")))
                    {
                        var results = output[(ukvp.Key, pkvp.Key)];
                        results.time.Add(master.time.julian);
                        results.dist.Add(Vector3.Distance(ukvp.Value.position, pkvp.Value.position) * master.scale);

                        hit = false;
												Debug.Log("connected");
                    }
                    else
                    {
                        hit = true;
												Debug.Log("hit");
                    }


                    if (options.debug)
                    {
                        LineRenderer lr = lrs[(ukvp.Key, pkvp.Key)];
                        lr.SetPositions(new Vector3[2] { ukvp.Value.position, pkvp.Value.position });
                        Color c = hit ? Color.red : Color.green;
                        lr.startColor = c;
                        lr.endColor = c;
                    }
                }
            }

            if (index % updateCount == 0)
            {
                await Task.Delay(1);
                loadingController.addPercent(0.01f);
            }

            index++;
        }

        loadingController.end();

        master.time.addJulianTime(checkpoint - master.time.julian);
        master.requestPositionUpdate();

        if (options.debug)
        {
            foreach (LineRenderer lr in lrs.Values) GameObject.Destroy(lr.gameObject);
        }
        if (report)
        {
            if (options.outputPath != default(string)) File.WriteAllText(options.outputPath, sb.ToString());
        }

        if (options.callback != null) options.callback(output);

        general.blockMainLoop = false;
        currentlyRunningTerrainRaycast = false;
    }

    public static IEnumerator raycastTerrainCoro(List<object> users, List<object> providers, double start, double end, double increment, dynamicLinkOptions options, bool report)
    {
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

        foreach (string us in u.Keys)
        {
            foreach (var ps in p.Keys)
            {
                List<double> time = new List<double>();
                List<double> dist = new List<double>();
                output[(us, ps)] = (time, dist);
                if (options.debug)
                {
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

        while (master.time.julian < end)
        {
            master.time.addJulianTime(increment);
            master.requestPositionUpdate();

						bool hit;

            foreach (var ukvp in u)
            {
                foreach (var pkvp in p)
                {
                    if (ukvp.Key == pkvp.Key) continue;
                    if (!existance[ukvp.Key](master.time) || !existance[pkvp.Key](master.time)) continue;
                    if (!Physics.Linecast(ukvp.Value.position, pkvp.Value.position, LayerMask.GetMask("terrain", "planet")))
                    {
                        var results = output[(ukvp.Key, pkvp.Key)];
                        results.time.Add(master.time.julian);
                        results.dist.Add(Vector3.Distance(ukvp.Value.position, pkvp.Value.position) * master.scale);

                        hit = false;
                        Debug.Log("connected");

                        //if (options.outputPath != default(string)) sb.AppendLine($"{master.time.ToString()}: {ukvp.Key} to {pkvp.Key}");
                        if (options.outputPath != default(string)) sb.AppendLine($"{master.time.julian}: {ukvp.Key} to {pkvp.Key}");
                    }
                    else
                    {
                        hit = true;
                        Debug.Log("hit");
                    }

                    if (options.debug)
                    {
                        LineRenderer lr = lrs[(ukvp.Key, pkvp.Key)];
                        lr.SetPositions(new Vector3[2] { ukvp.Value.position, pkvp.Value.position });
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

        if (options.debug)
        {
            foreach (LineRenderer lr in lrs.Values) GameObject.Destroy(lr.gameObject);
        }

        if (options.outputPath != default(string)) File.WriteAllText(options.outputPath, sb.ToString());

        if (options.callback != null) options.callback(output);

        general.blockMainLoop = false;
        currentlyRunningTerrainRaycast = false;
    }

    private static void formatObjects(List<object> objs, ref Dictionary<string, Transform> dict, ref Dictionary<string, Func<Time, bool>> dict2)
    {
        foreach (object obj in objs)
        {
            if (obj is planet)
            {
                planet p = (planet)obj;
                dict[p.name] = p.representation.gameObject.transform;
                dict2[p.name] = p.positions.exists;
            }
            else if (obj is satellite)
            {
                satellite s = (satellite)obj;
                dict[s.name] = s.representation.gameObject.transform;
                dict2[s.name] = s.positions.exists;
            }
            else if (obj is facility)
            {
                facility f = (facility)obj;
                dict[f.name] = f.representation.gameObject.transform;
                dict2[f.name] = f.exists;
            }
            else
            {
                Debug.LogWarning($"Warning: Unable to find {obj}");
            }
        }
    }
}
