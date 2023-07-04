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
          inner.windows = generateWindows(time);

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

  public static List<double[]> generateWindows(List<double> time)
  {

  		for(int k = 0; k < time.Count; k++)
  		{
  			time[k] = time[k] - 2461021.5;
            Debug.Log(time[k]);
  		}

        List<double> temp = time;
        temp.Add(Double.MaxValue);

        List<WNR> windows = new List<WNR>();

        int st = -1;
        for (int x = 0; x < temp.Count - 1; x++)
        {
            if (st == -1 && temp[x + 1] - temp[x] < .0035)
            {
                st = x;
            }
            else if (st != -1 && temp[x] != Double.MaxValue && temp[x + 1] - temp[x] > .0035)
            {
                Debug.Log("start: " + temp[st] + ", end: " + temp[x]);
                WNR window;
                window.start = temp[st];
                window.stop = temp[x];
                windows.Add(window);
                st = -1;
            }
        }

        return format(windows, time);
  }

  public static List<double[]> format(List<WNR> windows, List<double> time)
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
