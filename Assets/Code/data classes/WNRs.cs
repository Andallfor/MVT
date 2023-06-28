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

    List<WNR> windows = calculateWindows(time);

    return format(windows, time, distance);
  }

  public static List<WNR> calculateWindows(List<double> time)
	{

		List<double> temp_Vals = time;
		temp_Vals.Add(double.MaxValue);
		List<int> cc = diff(temp_Vals);
		List<double> start = addTime(temp_Vals, findOperate(cc));
		List<double> stop = addTime(temp_Vals, cc);
		return windowsDefiner(start, stop);
	}

  public static List<int> diff(List<double> toDiff)
	{
		List<int> returnList = new List<int>();
		int i = 0;

		for (int x = 0; x < toDiff.Count - 1; x++)
		{
			double Diff = toDiff[x+1]  - toDiff[x];
			if (Diff >= 0.0007) { returnList.Add(i); }
			i = i + 1;
		}

		return returnList;
	}

  public static List<int> findOperate(List<int> index)
	{
		List<int> returnList = new List<int>();
		returnList.Add(0);
		for (int x = 0; x < index.Count-1; x++)
		{
			returnList.Add(index[x] + 1);
		}

		return returnList;
	}

  public static List<double> addTime(List<double> toAdd, List<int> index)
	{
		List<double> returnList = new List<double>();
		foreach (int x in index)
		{
			returnList.Add(toAdd[x]);
		}

		return returnList;
	}

  public static List<WNR> windowsDefiner(List<double> start, List<double> stop)
	{
		List<WNR> returnList = new List<WNR>();

		for (int x = 0; x < stop.Count; x++)
		{
			WNR window;
			window.start = start[x];
			window.stop = stop[x];
			returnList.Add(window);
		}

		return returnList;
	}

  public static List<double[]> format(List<WNR> windows, List<double> time, List<double> distance)
	{
		List<double[]> returnList = new List<double[]>();

		if (windows.Count > 0)
		{
			for (int l = 0; l < windows.Count; l++)
			{
				int windowStartIndex = minAndAbs(time, windows[l].start); //what time element is close to the start
				double[] inner = new double[] {windows[l].start, windows[l].stop};
				returnList.Add(inner);
			}
		}

		return returnList;
	}

  public static int minAndAbs(List<double> time, double start)
	{
		int returnInt = 0;

		for (int x = 0; x < time.Count; x++)
		{
			if (x + 1 < time.Count)
			{
				double val1 = Math.Abs(time[x] - start);
				double val2 = Math.Abs(time[x + 1] - start);
				if (val1 < val2) { returnInt = x; }
			}
			else { returnInt = time.Count - 1; }
		}

		return returnInt;
	}

}
