using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;

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
		public List<finalWindow> windows;
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
		int i = 0;

		foreach((double, double) x in toSearch)
		{
			if (x.Item2 > minRate && x.Item2 < maxRate) { returnList.Add(i); }
			i = i + 1;
		}

		return returnList;
	}

	public static List<finalWindow> windowsDefiner(List<double> start, List<double> stop, List<double> rate)
	{
		List<finalWindow> returnList = new List<finalWindow>();

		for (int x = 0; x < start.Count; x++)
		{
			finalWindow window;
			window.start = start[x];
			window.stop = stop[x];
			window.rate = rate[x];
			returnList.Add(window);
		}
		Debug.Log("pass windowsDefiner");
		return returnList;
	}

	public static List<(double, double)> time_Rate(List<double> time, List<double> rate)
	{
		List<(double, double)> returnList = new List<(double, double)>();
		for(int k = 0; k < time.Count; k++)
		{
			returnList.Add((time[k], rate[k]));
		}

		return returnList;
	}

	public static int minAndAbs(List<double> time, double rate)
	{
		time.Add(double.MaxValue);
		
		for (int x = 0; x < time.Count; x++)
		{
			double val1 = Math.Abs(time[x] - rate);
			double val2 = Math.Abs(time[x + 1] - rate);
			if (val1 < val2) { return x; }
		}

		return 0;
	}

	public static List<finalWindow> calculateWindows(List<(double, double)> timeRate, double maxRate, double minRate)
	{
		if(minRate > maxRate) { maxRate = minRate; }
		List<int> c = find(timeRate, maxRate, minRate);
		List<(double, double)> temp_Vals = add(timeRate, c);
		temp_Vals.Add((double.MaxValue, double.MaxValue));
		List<int> cc = diff(temp_Vals);
		List<double> start = addTime(temp_Vals, findOperate(cc));
		List<double> stop = addTime(temp_Vals, cc);
		List<double> rate = addRate(temp_Vals, findOperate(cc));
		return windowsDefiner(start, stop, rate);
	}

	// dict type is wrong, waiting on adi to get back to me
	public static List<finalWindow> generateWindows(dynamic user1Data, dynamic user2Data, string band, List<double> time, List<double> distance)
	{
		int freq = 0;
		if (band.Contains("KaBand")) { freq = 26; }
		else if(band.Contains("XBand")) { freq = 8; }
		else if(band.Contains("SBand")) { freq =2; }

		double DataRate = user1Data[band]["DataRate"];

		List<double> rate = new List<double>();

		for (int k=0; k < time.Count; k++)
		{
			double FSPL = 92.45 + 20 * Math.Log10(distance[k]) + 20 * Math.Log10(freq);
			double cNo = user1Data[band]["EIRP"] - FSPL + user2Data[band]["GT"] + 228.6; // 228.6 is the Boltzmann Constant
			double rxEbN0 = cNo - 10 * Math.Log10(DataRate);
			double linkMargin = rxEbN0 - 4.1; 
			if(linkMargin >= 3) { rate.Add(DataRate/1000000); }
			else { rate.Add(0); }
		}

		List<finalWindow> windows = calculateWindows(time_Rate(time, rate), DataRate/1000000, 0);
		
		List<finalWindow> returnList = new List<finalWindow>();

		for (int l = 0; l < windows.Count; l++)
		{
			int windowStartIndex = minAndAbs(time, windows[l].start); //what time element is close to the start 
			double delayDistance = distance[windowStartIndex];

			finalWindow newWindow;
			newWindow.start = windows[l].start;
			newWindow.stop = windows[l].stop;
			newWindow.rate = (delayDistance * 1000) / 299792458;
			returnList.Add(newWindow);
		}

		return returnList;
	}

	public static void jsonWindows()
	{
		var data = DBReader.getData();
		var linkResults = linkBudgeting.dynamicLink();
		List<jsonWindowsInner> innerWindowList = new List<jsonWindowsInner>();

		foreach (KeyValuePair <string, (bool, double, double)> provider in linkBudgeting.providers)
		{
			foreach (KeyValuePair <string, (bool, double, double)> user in linkBudgeting.users) 
			{
				List<double> time = linkResults[(user.Key, provider.Key)].Item1;
				List<double> distance = linkResults[(user.Key, provider.Key)].Item2;

				string[] possibleBands = new string[] {"SBand", "XBand", "KaBand"};
				foreach(string posband in possibleBands)
				{

					var user2Data = data["Artemis_III"].satellites[user.Key];
					var user1Data = data["Artemis_III"].satellites[provider.Key];

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
						if (user.Key.Contains("LCN") && provider.Key.Contains("CLPS"))
						{
							user1Data[_band]["DataRate"] = user2Data[_band]["DataRate"];
						}
						
						jsonWindowsInner inner;
						inner.frequency = posband;
						inner.source = provider.Key;
						inner.destination = user.Key;
						inner.rate = user1Data[_band]["DataRate"]/1000000;
						inner.windows = generateWindows(user1Data, user2Data, _band, time, distance);

						innerWindowList.Add(inner);
					}
				}
			}
		}

		jsonWindowsWrapper json;
		json.epochTime = "11-May-2025";
		json.fileGenDate = "26-Jul-2022 6:30:00";
		json.windows = innerWindowList;
		
		string jsonReturn = JsonConvert.SerializeObject(json);
		Debug.Log(jsonReturn);
	}
}
