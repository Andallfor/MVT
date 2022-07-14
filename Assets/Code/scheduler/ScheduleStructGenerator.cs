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
            CREATE TABLE IF NOT EXISTS ""Windows"" (
                ""ID""	INTEGER,
                ""Source""	TEXT,
                ""Destination""	TEXT,
                ""Start""	NUMERIC,
                ""Stop""	NUMERIC,
                ""Frequency""	TEXT,
                PRIMARY KEY(""ID"")
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
        foreach (var window in json["windows"])
        {
            
            foreach (var block in window["windows"])
            {                
                Window wind = new Window();
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
                wind.boxes = Enumerable.Range((int)Math.Floor(wind.start+1.0), (int)Math.Floor(wind.stop+1.0)-(int)Math.Floor(wind.start+1.0)+1).ToList();
                wind.timeSpentInBox = new List<double>();
                if (wind.boxes.Count>1)
                {
                    wind.timeSpentInBox.Add(wind.boxes[0]-wind.start);
                    foreach (int day in Enumerable.Range(wind.boxes[0], wind.boxes[wind.boxes.Count-1]-1-wind.boxes[0])) wind.timeSpentInBox.Add(1);
                    wind.timeSpentInBox.Add(wind.stop-wind.boxes[wind.boxes.Count-2]);
                }
                else wind.timeSpentInBox.Add(wind.stop-wind.start);
                Debug.Log($"Start: {wind.start}, Stop: {wind.stop}\t\t{string.Join(", ", wind.boxes)}\t{string.Join(", ", wind.timeSpentInBox)}");
                if (!fileExists)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = $"INSERT INTO Windows (ID, Source, Destination, Start, Stop, Frequency) VALUES ({wind.ID},\"{wind.source}\",\"{wind.destination}\",{wind.start},{wind.stop},\"{wind.frequency}\")";
                    command.ExecuteNonQuery();
                }
                count +=1;
                windList.Add(wind);
            }
            
        }
        scenario.windows = windList;
        /*foreach (var printWindow in scenario.windows)
        {
            string print = JsonUtility.ToJson(printWindow, true);
            Debug.Log(print);
        }*/
         if (!fileExists) connection.Close();
    }
    public static void createConflictList()
    {
        SqliteConnection connection = new SqliteConnection("URI=file:Assets/Code/scheduler/windows.db;New=False");
        connection.Open();
        //foreach (Window block in scenario.windows)
        for (int i = 1; i <= scenario.windows.Count;i++)
        {
            //Debug.Log(i);
            Window block = scenario.windows[i];
            var command = connection.CreateCommand();
            command.CommandText = "drop table if exists TimeFiltered;";
            command.ExecuteNonQuery();
            List<int> cons = new List<int>();
            int id = block.ID;
            command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT ID from Windows WHERE ID <> {id} AND
                (
                    (Start BETWEEN (Select Start from Windows where ID ={id}) AND (Select Stop from Windows where ID={id})) OR
                    (Stop BETWEEN (Select Start from Windows where ID ={id}) AND (Select Stop from Windows where ID={id})) OR
                    (Start <=  (Select Start from Windows where ID ={id}) AND Stop >= (Select Stop from Windows where ID={id}))
                )
                AND
                (
                    (
                        Source = (select Source from Windows where ID={id}) or 
                        Destination = (select Destination from Windows where ID={id})
                    )
                    AND NOT
                    (
                        Source = (select Source from Windows where ID={id}) and
                        Destination = (select Destination from Windows where ID={id})
                    )
                )
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
    public string frequency;
    public string source; //user
    public string destination;
    public double rate;
    public double start;
    public double stop;
    public double latency;
    public List<int> conflicts;
    public List<int> boxes;
    public List<double> timeSpentInBox;
}
