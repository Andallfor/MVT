using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Data;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public static class ScheduleStructGenerator
{

    public static void genStruct()
    {
        string filePath = "Assets/Resources/SchedulingJSONS/LunarWindows-ArtemisIII_06_30_22.json";
        JObject json = JObject.Parse(File.ReadAllText(filePath)); 
        Scenario scenario = new Scenario();
        scenario.epochTime = (string) json["epochTime"];
        scenario.fileGenDate = (string) json["fileGenDate"];
        List<Window> windList = new List<Window>();
        foreach (var window in json["windows"])
        {
            Window wind = new Window();
            foreach (var block in window["windows"])
            {                
                wind.frequency = (string)window["frequency"];
                wind.source = (string)window["source"];
                wind.destination = (string)window["destination"];
                wind.rate = (double)window["rate"];
                wind.start = (double)block[0];
                wind.stop = (double)block[1];
                wind.duration = (double)block[2];
            }
            windList.Add(wind);
        }
        scenario.windows = windList;
        string print = JsonConvert.SerializeObject(scenario);
        Debug.Log(print);
    }
}


public struct Scenario
{
    public string epochTime;
    public string fileGenDate;
    public List<Window> windows;
}

public struct Window
{
    public string frequency;
    public string source;
    public string destination;
    public double rate;
    public double start;
    public double stop;
    public double duration;
}
