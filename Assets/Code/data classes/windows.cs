#if (UNITY_EDITOR || UNITY_STANDALONE) && !UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.IO;

public class windows
{
	public struct window
	{
		public double start;
		public double stop;
	}

	public struct finalWindow
	{
		public double start;
		public double stop;
		public double rate;
	}

	public struct jsonWindowsInner
	{
		public string frequency;
		public string source;
		public string destination;
		public double rate;
		public List<double[]> windows;
	}

	public struct jsonWindowsWrapper
	{
		public string epochTime;
		public string fileGenDate;
		public List<jsonWindowsInner> windows;
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

	public static List<int> diff(List<(double, double)> toDiff)
	{
		List<int> returnList = new List<int>();
		int i = 0;

		for (int x = 0; x < toDiff.Count - 1; x++)
		{
			double Diff = toDiff[x+1].Item1  - toDiff[x].Item1;
			if (Diff >= 0.0035) { returnList.Add(i); }
			i = i + 1;
		}

		return returnList;
	}

	public static List<(double, double)> add(List<(double, double)> toAdd, List<int> index)
	{
		List<(double, double)> returnList = new List<(double, double)>();
		foreach (int x in index)
		{
			returnList.Add(toAdd[x]);
		}

		return returnList;
	}

	public static List<double> addTime(List<(double, double)> toAdd, List<int> index)
	{
		List<double> returnList = new List<double>();
		foreach (int x in index)
		{
			returnList.Add(toAdd[x].Item1);
		}

		return returnList;
	}

	public static List<double> addRate(List<(double, double)> toAdd, List<int> index)
	{
		List<double> returnList = new List<double>();
		foreach (int x in index)
		{
			returnList.Add(toAdd[x].Item2);
		}

		return returnList;
	}

	public static List<int> find(List<(double, double)> toSearch, double maxRate, double minRate)
	{
		List<int> returnList = new List<int>();
		for (int i = 0; i < toSearch.Count; i++)
		{
			if (toSearch[i].Item2 > minRate && toSearch[i].Item2 <= maxRate)
			{
				returnList.Add(i);
			}
		}

		return returnList;
	}

	public static List<finalWindow> windowsDefiner(List<double> start, List<double> stop, List<double> rate)
	{
		List<finalWindow> returnList = new List<finalWindow>();

		for (int x = 0; x < stop.Count; x++)
		{
			finalWindow window;
			window.start = start[x];
			window.stop = stop[x];
			window.rate = rate[x];
			returnList.Add(window);
		}

		return returnList;
	}

	public static List<(double, double)> time_Rate(List<double> time, List<double> rate)
	{
		List<(double, double)> returnList = new List<(double, double)>();
		for(int k = 0; k < time.Count; k++)
		{
			returnList.Add((time[k]- 2460806.5, rate[k]));
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

	public static List<finalWindow> calculateWindows(List<(double, double)> timeRate, double maxRate, double minRate)
	{
		minRate = 0;
		List<int> c = find(timeRate, maxRate, minRate);
		List<(double, double)> temp_Vals = add(timeRate, c);
		temp_Vals.Add((double.MaxValue, double.MaxValue));
		List<int> cc = diff(temp_Vals);
		List<double> start = addTime(temp_Vals, findOperate(cc));
		List<double> stop = addTime(temp_Vals, cc);
		List<double> rate = addRate(temp_Vals, cc);
		return windowsDefiner(start, stop, rate);
	}

	public static List<double[]> format(List<finalWindow> windows, List<double> time, List<double> distance)
	{
		List<double[]> returnList = new List<double[]>();

		if (windows.Count > 0)
		{
			for (int l = 0; l < windows.Count; l++)
			{
				int windowStartIndex = minAndAbs(time, windows[l].start); //what time element is close to the start
				double delayDistance = distance[windowStartIndex];
				double[] inner = new double[] {windows[l].start, windows[l].stop, ((delayDistance * 1000) / 299792458)};
				returnList.Add(inner);
			}
		}

		return returnList;
	}

	// dict type is wrong, waiting on adi to get back to me
	public static List<double[]> generateWindows(double DataRate, double EIRP, double GT, string band, List<double> time, List<double> distance)
	{
		int freq = 0;
		if (band.Contains("KaBand")) { freq = 26; }
		else if(band.Contains("XBand")) { freq = 8; }
		else if(band.Contains("SBand")) { freq =2; }

		List<double> rate = new List<double>();

		for (int k=0; k < time.Count; k++)
		{
			double FSPL = 92.45 + 20 * Math.Log10(distance[k]) + 20 * Math.Log10(freq);
			double cNo = EIRP - FSPL + GT + 228.6; // 228.6 is the Boltzmann Constant
			double rxEbN0 = cNo - 10 * Math.Log10(DataRate);
			double linkMargin = rxEbN0 - 4.1;
			if(linkMargin >= 3) { rate.Add(DataRate/1000000); }
			else { rate.Add(0); }
		}

		List<finalWindow> windows = calculateWindows(time_Rate(time, rate), DataRate/1000000, 0);

		return format(windows, time, distance);
	}

	public static void jsonWindows(Dictionary<(string, string), (List<double>, List<double>)> linkResults)
	{
		var data = DBReader.getData();
		List<jsonWindowsInner> innerWindowList = new List<jsonWindowsInner>();

		foreach (KeyValuePair <string, (bool, double, double)> provider in linkBudgeting.providers)
		{
			foreach (KeyValuePair <string, (bool, double, double)> user in linkBudgeting.users)
			{
				if (!linkResults.ContainsKey((user.Key, provider.Key))) continue;
				List<double> time = linkResults[(user.Key, provider.Key)].Item1;
				List<double> distance = linkResults[(user.Key, provider.Key)].Item2;

				if (time.Count > 0)
				{
					string[] possibleBands = new string[] {"SBand", "XBand", "KaBand"};
					foreach(string posband in possibleBands)
					{

						var user1Data = data["Artemis_III"].satellites[user.Key];
						var user2Data = data["Artemis_III"].satellites[provider.Key];

						string _band = "None";

						if (user1Data["CentralBody"] == "Moon" && user2Data["CentralBody"] == "Earth")
						{
							_band = posband + "DTE";
						}
						else if (user1Data["CentralBody"] == "Moon" && user2Data["CentralBody"] == "Moon")
						{
							_band = posband + "Proximity";
						}
						else if (user1Data["CentralBody"] == "Earth" && user2Data["CentralBody"] == "Moon")
						{
							_band = posband + "DTE";
						}


						if (user1Data.ContainsKey(_band) && user2Data.ContainsKey(_band))
						{
							if(user2Data[_band].ContainsKey("GT"))
                    	    {
								if (user1Data[_band].ContainsKey("EIRP"))
								{
									double DataRate = 0;
									double EIRP = user1Data[_band]["EIRP"];
									double GT = user2Data[_band]["GT"];
									if (user.Key.Contains("LCN") && provider.Key.Contains("CLPS"))
									{
										DataRate = user2Data[_band]["DataRate"];
									}
									else
									{
										DataRate = user1Data[_band]["DataRate"];
									}

									jsonWindowsInner inner;
									inner.frequency = posband;
									inner.source = user.Key;
									inner.destination = provider.Key;
									inner.rate = (double)user1Data[_band]["DataRate"] / 1000000;
									inner.windows = generateWindows(DataRate, EIRP, GT, _band, time, distance);

									innerWindowList.Add(inner);
								}
							}
						}
					}
				}
			}
		}

		jsonWindowsWrapper json;
		json.epochTime = "11-May-2025";
		json.fileGenDate = DateTime.Now.ToString("MM-dd_hhmm"); ;
		json.windows = innerWindowList;

		string jsonReturn = JsonConvert.SerializeObject(json, Formatting.Indented);
		File.WriteAllText(Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), "windows.json"), jsonReturn);
		Debug.Log("Finished Writing File");

	}
}
#endif