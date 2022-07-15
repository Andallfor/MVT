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
    public static Scenario scenario = new Scenario();
    public static void genDB()
    {
        bool fileExists = File.Exists("Assets/Code/scheduler/windows.db");
        SqliteConnection connection = new SqliteConnection("URI=file:Assets/Code/scheduler/windows.db;New=False");
        if (!fileExists)
        {
            connection.Open();
            var createCommand = connection.CreateCommand();
            createCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""Windows_data"" (
                ""Block_ID""	INTEGER,
                ""Source""	TEXT,
                ""Destination""	TEXT,
                ""Start""	NUMERIC,
                ""Stop""	NUMERIC,
                ""Frequency""	TEXT,
                ""Rate"" NUMERIC,
                ""Latency"" Numeric,
                PRIMARY KEY(""Block_ID"")
            );";
            createCommand.ExecuteNonQuery();
            Debug.Log("Created DB");
        }
        string filePath = "Assets/Resources/SchedulingJSONS/LunarWindows-ArtemisIII_06_30_22.json";
        JObject json = JObject.Parse(File.ReadAllText(filePath)); 
        scenario.epochTime = (string) json["epochTime"];
        scenario.fileGenDate = (string) json["fileGenDate"];
        List<Window> windList = new List<Window>();
        int count = 1;
        if (!fileExists)
        {
            var command = connection.CreateCommand();
            command.CommandText = "BEGIN;";
            command.ExecuteNonQuery();
        }
        foreach (var window in json["windows"])
        {
            
            foreach (var block in window["windows"])
            {                
                Window wind = new Window();
                wind.ID = count;
                double start = (double)block[0];
                double stop = (double)block[1];
                //wind.frequency = (string)window["frequency"];
                //wind.frequency = wind.frequency.Replace("\"", "");
                //wind.source = (string)window["source"];
                //wind.destination = (string)window["destination"];
                wind.source = ((string)window["source"]).Replace("\"", "");
                wind.destination = ((string)window["source"]).Replace("\"", "");
                //wind.rate = (double)window["rate"];
                wind.start = (double)block[0];
                wind.stop = (double)block[1];
                //wind.latency = (double)block[2];
                wind.boxes = Enumerable.Range((int)Math.Floor(start+1.0), (int)Math.Floor(stop+1.0)-(int)Math.Floor(start+1.0)+1).ToList();
                wind.timeSpentInBox = new List<double>();
                if (wind.boxes.Count>1)
                {
                    wind.timeSpentInBox.Add(wind.boxes[0]-start);
                    foreach (int day in Enumerable.Range(wind.boxes[0], wind.boxes[wind.boxes.Count-1]-1-wind.boxes[0])) wind.timeSpentInBox.Add(1);
                    wind.timeSpentInBox.Add(stop-wind.boxes[wind.boxes.Count-2]);
                }
                else wind.timeSpentInBox.Add(stop-start);
                //Debug.Log($"Start: {start}, Stop: {stop}\t\t{string.Join(", ", wind.boxes)}\t{string.Join(", ", wind.timeSpentInBox)}");
                if (!fileExists)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = $"INSERT INTO Windows_data (Block_ID, Source, Destination, Start, Stop, Frequency) VALUES ({wind.ID},\"{wind.source}\",\"{wind.destination}\",{start},{stop},\"{(string)window["frequency"]}\")";
                    command.ExecuteNonQuery();
                }
                count +=1;
                windList.Add(wind);
            }
        }
        if(!fileExists)
        {
            var command = connection.CreateCommand();
            command.CommandText = "COMMIT;";
            command.ExecuteNonQuery();
            connection.Close();
        }
        scenario.windows = windList;
        /*foreach (var printWindow in scenario.windows)
        {
            string print = JsonUtility.ToJson(printWindow, true);
            Debug.Log(print);
        }*/
    }
    public static void createConflictList()
    {
        SqliteConnection connection = new SqliteConnection("URI=file:Assets/Code/scheduler/windows.db;New=False");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "BEGIN;";
        command.ExecuteNonQuery();
        for (int i = 1; i <= scenario.windows.Count-1;i++)
        {
            //Debug.Log(i);
            Window block = scenario.windows[i];
            List<int> cons = new List<int>();
            int id = block.ID;
            command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Block_ID from Windows_data WHERE Block_ID <> {id} AND
                (
                    (Start BETWEEN {block.start} AND {block.stop}) OR
                    (Stop BETWEEN {block.start} AND {block.stop}) OR
                    (Start <=  {block.start} AND Stop >= {block.stop})
                )
                AND
                (
                    (
                        Source = ""{block.source}"" or 
                        Destination = ""{block.destination}""
                    )
                    AND NOT
                    (
                        Source = ""{block.source}"" and
                        Destination = ""{block.destination}""
                    )
                );
            ";
            IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                cons.Add(reader.GetInt32(0));
                //Debug.Log(reader["ID"]);
            }
            block.conflicts = cons;
            /*using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    cons.Add(reader[0]);
                    //Debug.Log(reader["ID"]);
                }
            }*/
        }
        command = connection.CreateCommand();
        command.CommandText = "COMMIT;";
        command.ExecuteNonQuery();
        connection.Close();
    }

   /* public static void test()
    {
        Window finded = scenario.windows.Find(item => item.ID==6);
        foreach(var con in finded.conflicts)
            Debug.Log($"ID: {con.ID}, Source: {con.source}, Destination: {con.destination}, Start: {con.start}, Stop: {con.stop}");
    }*/
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
    //public string frequency;
    public string source; //user
    public string destination;
    //public double rate;
    public double start;
    public double stop;
    //public double latency;
    public List<int> conflicts;
    public List<int> boxes;
    public List<double> timeSpentInBox;
}

