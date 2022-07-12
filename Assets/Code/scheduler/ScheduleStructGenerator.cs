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

public static class ScheduleStructGenerator
{

    public static void genStruct()
    {
        string filePath = "Assets/Resources/SchedulingJSONS/LunarWindows-ArtemisIII_06_30_22.json";
        JObject json = JObject.Parse(File.ReadAllText(filePath)); 
        Scenario scenario = new Scenario();
        
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
    public string freqency;
    public string source;
    public string destination;
    public double rate;
    public double start;
    public double stop;
    public double duration;
}
