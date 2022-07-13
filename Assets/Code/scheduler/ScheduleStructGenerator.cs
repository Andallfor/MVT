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
using Mono.Data.Sqlite;



public static class ScheduleStructGenerator
{

    public static void genDB()
    {
        if (File.Exists("Assets/Code/scheduler/windows.db")) return;
        var connection = new SqliteConnection("URI=file:Assets/Code/scheduler/windows.db;New=False");
        connection.Open();
        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
        CREATE TABLE ""Windows"" (
            ""ID""	INTEGER,
            ""Source""	TEXT,
            ""Destination""	BLOB,
            ""Start""	NUMERIC,
            ""Stop""	NUMERIC,
            ""Frequency""	TEXT,
            PRIMARY KEY(""ID"")
        );";
        createCommand.ExecuteNonQuery();
        Debug.Log("Created DB");

        string filePath = "Assets/Resources/SchedulingJSONS/LunarWindows-ArtemisIII_06_30_22.json";
        JObject json = JObject.Parse(File.ReadAllText(filePath)); 
        Scenario scenario = new Scenario();
        scenario.epochTime = (string) json["epochTime"];
        scenario.fileGenDate = (string) json["fileGenDate"];
        List<Window> windList = new List<Window>();
        int count = 0;
        foreach (var window in json["windows"])
        {
            
            Window wind = new Window();
            foreach (var block in window["windows"])
            {                
                wind.ID = count;
                wind.frequency = (string)window["frequency"];
                wind.frequency = wind.frequency.Replace("\"", "");
                wind.source = (string)window["source"];
                wind.destination = (string)window["destination"];
                wind.source = wind.source.Replace("\"", "");
                wind.destination = wind.destination.Replace("\"", "");
                wind.rate = (double)window["rate"];
                wind.start = (double)block[0];
                wind.stop = (double)block[1];
                wind.latency = (double)block[2];
                var command = connection.CreateCommand();
                command.CommandText = $"INSERT INTO Windows (ID, Source, Destination, Start, Stop, Frequency) VALUES ({wind.ID},\"{wind.source}\",\"{wind.destination}\",{wind.start},{wind.stop},\"{wind.frequency}\")";
                command.ExecuteNonQuery();
                count +=1;
            }
            windList.Add(wind);
        }
        scenario.windows = windList;
        /*foreach (var printWindow in scenario.windows)
        {
            string print = JsonUtility.ToJson(printWindow, true);
            Debug.Log(print);
        }*/
        connection.Close();
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
    public int ID;
    public string frequency;
    public string source; //user
    public string destination;
    public double rate;
    public double start;
    public double stop;
    public double latency;
    public List<Window> conflicts;
}
