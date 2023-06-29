using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.IO;

public class WNRs
{
  public struct WNR
  {
    public double start;
    public double stop;
  }

  public struct WNRInner
	{
		public string source;
		public string destination;
		public List<double[]> windows;
	}

	public struct WNRWrapper
	{
		public string epochTime;
		public string fileGenDate;
		public List<WNRInner> windows;
	}

  public static void jsonWindows(Dictionary<(string, string), (List<double>, List<double>)> linkResults)
  {
    List<WNRInner> innerWindowList = new List<WNRInner>();

    foreach (KeyValuePair <string, (bool, double, double)> provider in linkBudgeting.providers)
    {
      foreach (KeyValuePair <string, (bool, double, double)> user in linkBudgeting.users)
      {
        if (!linkResults.ContainsKey((user.Key, provider.Key))) continue;
        List<double> time = linkResults[(user.Key, provider.Key)].Item1;
        List<double> distance = linkResults[(user.Key, provider.Key)].Item2;

        if (time.Count > 0)
        {
          WNRInner inner;
          inner.source = user.Key;
          inner.destination = provider.Key;
          inner.windows = generateWindows(time, distance);

          innerWindowList.Add(inner);
        }
      }
    }

    WNRWrapper json;
    json.epochTime = "12-Dec-2025";
    json.fileGenDate = DateTime.Now.ToString("MM-dd_hhmm"); ;
    json.windows = innerWindowList;

    string jsonReturn = JsonConvert.SerializeObject(json, Formatting.Indented);
    //File.WriteAllText(Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), "windows.json"), jsonReturn);
    File.WriteAllText("/Users/arya/Downloads/windows.json", jsonReturn);
    Debug.Log("Finished Writing File");
  }

  public static List<double[]> generateWindows(List<double> time, List<double> distance)
  {

  		for(int k = 0; k < time.Count; k++)
  		{
  			time[k] = time[k]- 2461021.5;
  		}

    List<WNR> windows = new List<WNR>();
    int st = -1;

    for(int x = 1; x < time.Count - 1; x++)
    {
      if (st == -1 && time[x] - time[x-1] < .0035)
      {
        st = x - 1;
      }
      else if (st != -1 && time[x] - time[x-1] > .0035)
      {
        WNR window;
        window.start = time[st];
        window.stop = time[x-1];
        windows.Add(window);
        st = -1;
      }
    }

    return format(windows, time, distance);
  }

  public static List<double[]> format(List<WNR> windows, List<double> time, List<double> distance)
	{
		List<double[]> returnList = new List<double[]>();

		if (windows.Count > 0)
		{
			for (int l = 0; l < windows.Count; l++)
			{
				double[] inner = new double[] {windows[l].start, windows[l].stop};
				returnList.Add(inner);
			}
		}

		return returnList;
	}
}
