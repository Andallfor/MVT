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
    public static void genDB(dynamic missionStructure, string misName, string JSONPath, string date, string dbName)
    {
        //Select * from Windows_data where Source="HLS-Surface" and Destination="LCN-12hourfrozen-1-LowPower" and Frequency="Ka Band" order by Start ASC
        scenario.users.Clear();
        if(File.Exists(@$"Assets/Code/scheduler/{date}/{dbName}_{date}.db"))
        {
            File.Delete(@$"Assets/Code/scheduler/{date}/{dbName}_{date}.db");
        }
        bool fileExists = false;
        SqliteConnection connection = new SqliteConnection($"URI=file:Assets/Code/scheduler/{date}/{dbName}_{date}.db;New=False");
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
                ""Freq_Priority"" INTEGER,
                ""Schedule_Priority"" NUMERIC,
                ""Ground_Priority"" NUMERIC,
                ""Service_Level"" NUMERIC,
                PRIMARY KEY(""Block_ID""));    
                ";
            createCommand.ExecuteNonQuery();
            Debug.Log("Created DB");
        }
        string restrictionPath = $"Assets/Code/scheduler/restrictions.json";
        JObject restrictionJson = JObject.Parse(File.ReadAllText(restrictionPath));
        string filePath = $"Assets/Resources/SchedulingJSONS/{JSONPath}";
        JObject json = JObject.Parse(File.ReadAllText(filePath)); 
        scenario.epochTime = (string) json["epochTime"];
        scenario.fileGenDate = (string) json["fileGenDate"];
        List<Window> windList = new List<Window>();
        int count = 0;
        if (!fileExists)
        {
            var command = connection.CreateCommand();
            command.CommandText = "BEGIN;";
            command.ExecuteNonQuery();
        }
        foreach (var window in json["windows"])
        {
            string source = ((string)window["source"]).Replace("\"", "");
            if (source.EndsWith("-Backup")) continue;
            string destination = ((string)window["destination"]).Replace("\"", "");
            double schedPrio = source.EndsWith("-Backup") ? 0.1 : 0;
            double service_Level = 0;
            double groundPrio = 0;
            try
            {
                    service_Level = (double)missionStructure[misName].Item2[source]["Service_Level"];
            }
            catch (KeyNotFoundException)
            {
                //Debug.Log($"Either satellite: {wind.source} or Service Level didn't exist");
            }
            try
            {
                    schedPrio += (double)missionStructure[misName].Item2[source]["Schedule_Priority"];
            }
            catch (KeyNotFoundException)
            {
                // Debug.Log($"Either satellite: {wind.source} or schedule priority didn't exist");
            }
            try
            {
                groundPrio = (double)missionStructure[misName].Item2[destination]["Ground_Priority"];
            }
            catch (KeyNotFoundException)
            {
                //Debug.Log($"Either destination: {wind.destination} or ground priority didn't exist");
            }
            int freqPrio = 0;
            switch((string)window["frequency"])
            {
                case "Ka Band":
                    freqPrio = 1;
                    break;
                case "X Band":
                    freqPrio = 2;
                    break;
                case "S Band":
                    freqPrio = 3;
                    break;
            }
            if (!scenario.users.ContainsKey(source))
            {
                User currentUser = new User();
                string stringServicePeriod = "1 Day";
                double servicePeriod = 1;
                try
                {
                    stringServicePeriod = missionStructure[misName].Item2[source]["Service_Period"];
                    servicePeriod += Double.Parse(stringServicePeriod.Remove(stringServicePeriod.LastIndexOf(" Day")))-1;
                }
                catch (KeyNotFoundException)
                {
                    //Debug.Log($"Either satellite: {wind.source} or service period didn't exist");
                }

                currentUser.numDays = (int)(30/servicePeriod);
                currentUser.serviceLevel = service_Level;
                currentUser.priority = (double)missionStructure[misName].Item2[source]["Schedule_Priority"];
                currentUser.timeIntervalStart = (double)missionStructure[misName].Item2[source]["TimeInterval_start"];
                currentUser.timeIntervalStop = (double)missionStructure[misName].Item2[source]["TimeInterval_stop"];
                for (double i = 0; i <= currentUser.numDays;i+=servicePeriod)
                {
                    
                    (double, double) curBox = (i,0);
                    if(i == Math.Floor(currentUser.timeIntervalStart) && currentUser.timeIntervalStart%1!=0)
                    {
                        curBox.Item2 = (1-(currentUser.timeIntervalStart-i))*currentUser.serviceLevel;
                    }
                    //else if (i == Math.Floor(currentUser.timeIntervalStop) && currentUser.timeIntervalStop%1!=0)
                    //{
                        //curBox.Item2 = (currentUser.timeIntervalStop-i)*currentUser.serviceLevel;
                    //}
                    else if (i == Math.Floor(currentUser.timeIntervalStop))
                    {
                        curBox.Item2 = (currentUser.timeIntervalStop-i)*currentUser.serviceLevel;
                    }
                    else if (currentUser.timeIntervalStart <=i && i <= currentUser.timeIntervalStop)
                    {
                        curBox.Item2 = currentUser.serviceLevel;
                    }
                    currentUser.boxes.Add(curBox.Item1, curBox.Item2);
                }
                try
                {
                foreach(var provider in restrictionJson[source]) currentUser.allowedProviders.Add((string) provider);
                }
                catch {}
                scenario.users.Add(source, currentUser);
            }
            if (!(scenario.users[source].allowedProviders.Contains(destination)))
            {
                //Debug.Log($"Window kicked out because {source} is not allowed to talk to {destination}");
                continue;
            }
            foreach (var block in window["windows"])
            {                
                Window wind = new Window();
                wind.ID = count;
                double start = (double)block[0];
                double stop = (double)block[1];
                wind.frequency = (string)window["frequency"];
                wind.frequency = wind.frequency.Replace("\"", "");
                wind.Freq_Priority = freqPrio;
                wind.Schedule_Priority = schedPrio;
                wind.Ground_Priority = groundPrio;
                //wind.source = (string)window["source"];
                //wind.destination = (string)window["destination"];
                wind.source = source;
                wind.destination = destination;
                //wind.rate = (double)window["rate"];
                wind.start = start;
                wind.stop = stop;
                wind.duration =stop-start;
                //wind.latency = (double)block[2];
                wind.days = Enumerable.Range((int)Math.Floor(start), (int)Math.Floor(stop)-(int)Math.Floor(start)+1).ToList();
                wind.timeSpentInDay = new List<double>();
                if (wind.days.Count>1)
                {
                    wind.timeSpentInDay.Add(1-(start-wind.days[0]));
                    foreach (int day in Enumerable.Range(wind.days[0], wind.days[wind.days.Count-1]-1-wind.days[0])) wind.timeSpentInDay.Add(1);
                    wind.timeSpentInDay.Add(stop-wind.days[wind.days.Count-1]);
                }
                else wind.timeSpentInDay.Add(stop-start);
                //Debug.Log($"Start: {start}, Stop: {stop}\t\t{string.Join(", ", wind.days)}\t{string.Join(", ", wind.timeSpentInDay)}");

                
                
                if (!fileExists)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                    INSERT INTO Windows_data (Block_ID, Source, Destination, Start, Stop, Frequency, Freq_Priority, Schedule_Priority, Ground_Priority, Service_Level) VALUES 
                    ({wind.ID},""{source}"",""{destination}"",{start},{stop},""{(string)window["frequency"]}"", {freqPrio}, {schedPrio}, {groundPrio}, {service_Level})";
                    command.ExecuteNonQuery();
                }
                count +=1;
                //if (!(scenario.windows==null) || !scenario.windows.Any(x=>x.ID==wind.ID))
                if (scenario.windows == null)
                    windList.Add(wind);
                else if(scenario.windows.Any(x=>x.ID==wind.ID))
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
        scenario.windows = windList.OrderBy(s => s.Schedule_Priority).ThenBy(s => s.Ground_Priority).ThenBy(s=>s.Freq_Priority).ThenByDescending(s=>s.duration).ToList();
        string debugJson = JsonConvert.SerializeObject(scenario.users, Formatting.Indented);
        System.IO.File.WriteAllText ($"PreDFSUsers.txt", debugJson);
        //Debug.Log($@"ID: {scenario.windows[0].ID}\tSource:{scenario.windows[0].source}\tDestination{scenario.windows[0].destination}
        //\tschedPrio: {scenario.windows[0].Schedule_Priority}\tGroundPrio: {scenario.windows[0].Ground_Priority}\tFreq_Prio: {scenario.windows[0].Freq_Priority}\tDuration: {scenario.windows[0].duration}");
        /*foreach (var printWindow in scenario.windows)
        {
            string print = JsonUtility.ToJson(printWindow, true);
            Debug.Log(print);
        }*/
        connection.Close();
    }
    //Ka - highest priority
    //X
    //s - lowest priority
    public static void createConflictList(string date)
    {
        SqliteConnection connection = new SqliteConnection($"URI=file:Assets/Code/scheduler/{date}/PreconWindows_{date}.db;New=False");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "BEGIN;";
        command.ExecuteNonQuery();
        for (int i = 0; i < scenario.windows.Count-1;i++)
        {
            //Debug.Log(i);
            Window block = scenario.windows[i];
            List<(int,int)> cons = new List<(int, int)>();
            int id = block.ID;
            command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Block_ID, Start, Stop from Windows_data WHERE Block_ID <> {id} AND
                (
                    (Start > {block.start} and Start < {block.stop}) OR
                    (Stop > {block.start} AND Stop < {block.stop}) OR
                    (Start <  {block.start} AND Stop > {block.stop})
                )
                AND
                (
                    (
                        Source = ""{block.source}"" or 
                        Destination = ""{block.destination}""
                    )
                    --AND NOT
                    --(
                        --Source = ""{block.source}"" and
                        --Destination = ""{block.destination}""
                    --)
                )
                ORDER by 
	            Schedule_Priority ASC,
	            Ground_Priority ASC,
	            Freq_Priority ASC,
	            (Stop-Start) DESC
                ;
            ";
            IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int conID = reader.GetInt32(0);
                double conStart = scenario.windows.Find(i => i.ID == conID).start;
                double conStop = scenario.windows.Find(i => i.ID == conID).stop;
                int conCase = -1; 
                //if(conID == 1713)
                    //Debug.Log($"Before change: {scenario.windows.Find(i => i.ID == conID).start}->{scenario.windows.Find(i => i.ID == conID).stop}");
                if(block.start < conStart && conStart < block.stop && conStop > block.stop)
                {
                    conCase = 2;
                    /*conStart = block.stop;
                    scenario.windows.Find(i => i.ID == conID).start = conStart;
                    scenario.windows.Find(i => i.ID == conID).duration = scenario.windows.Find(i => i.ID == conID).stop - block.stop;

                    scenario.windows.Find(i => i.ID == conID).days = Enumerable.Range((int)Math.Floor(conStart), (int)Math.Floor(conStop)-(int)Math.Floor(conStart)+1).ToList();
                    scenario.windows.Find(i => i.ID == conID).timeSpentInDay = new List<double>();
                    if (scenario.windows.Find(i => i.ID == conID).days.Count>1)
                    {
                        scenario.windows.Find(i => i.ID == conID).timeSpentInDay.Add(1-(conStart-scenario.windows.Find(i => i.ID == conID).days[0]));
                        foreach (int day in Enumerable.Range(scenario.windows.Find(i => i.ID == conID).days[0], scenario.windows.Find(i => i.ID == conID).days[scenario.windows.Find(i => i.ID == conID).days.Count-1]-1-scenario.windows.Find(i => i.ID == conID).days[0])) scenario.windows.Find(i => i.ID == conID).timeSpentInDay.Add(1);
                        scenario.windows.Find(i => i.ID == conID).timeSpentInDay.Add(conStop- scenario.windows.Find(i => i.ID == conID).days[ scenario.windows.Find(i => i.ID == conID).days.Count-1]);
                    }
                    else  scenario.windows.Find(i => i.ID == conID).timeSpentInDay.Add(conStop-conStart);
                    //if(conID == 1713)
                        //Debug.Log($"After changing start: {conStart}->{conStop}");
                        */
                }
                else if(block.start < conStop && conStop < block.stop && conStart < block.start)
                {
                    conCase = 1;
                    /*conStop = block.start;
                    scenario.windows.Find(i => i.ID == conID).stop = conStop;
                    scenario.windows.Find(i => i.ID == conID).duration = conStop - scenario.windows.Find(i => i.ID == conID).start;

                    scenario.windows.Find(i => i.ID == conID).days = Enumerable.Range((int)Math.Floor(conStart), (int)Math.Floor(conStop)-(int)Math.Floor(conStart)+1).ToList();
                    scenario.windows.Find(i => i.ID == conID).timeSpentInDay = new List<double>();
                    if (scenario.windows.Find(i => i.ID == conID).days.Count>1)
                    {
                        scenario.windows.Find(i => i.ID == conID).timeSpentInDay.Add(1-(conStart-scenario.windows.Find(i => i.ID == conID).days[0]));
                        foreach (int day in Enumerable.Range(scenario.windows.Find(i => i.ID == conID).days[0], scenario.windows.Find(i => i.ID == conID).days[scenario.windows.Find(i => i.ID == conID).days.Count-1]-1-scenario.windows.Find(i => i.ID == conID).days[0])) scenario.windows.Find(i => i.ID == conID).timeSpentInDay.Add(1);
                        scenario.windows.Find(i => i.ID == conID).timeSpentInDay.Add(conStop- scenario.windows.Find(i => i.ID == conID).days[ scenario.windows.Find(i => i.ID == conID).days.Count-1]);
                    }
                    else  scenario.windows.Find(i => i.ID == conID).timeSpentInDay.Add(conStop-conStart);
                    //if(conID == 1713)
                        //Debug.Log($"After changing stop: {conStart}->{conStop}");
                    continue;*/
                }
                //if(conID == 1713)
                    //Debug.Log($"Didn't change: {conStart}->{conStop}");
                cons.Add((reader.GetInt32(0), conCase));

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

    public static void doDFS(string date)
    {
        List<int> totalConflicts = new List<int>();
        foreach (var curBlock in scenario.windows)
        {
            if (totalConflicts.Contains(curBlock.ID)) continue;
            
            for ( int i = 0; i < curBlock.conflicts.Count(); i++)
            {
                (int, int) con = curBlock.conflicts[i];
                Window conWin = scenario.windows.Find(i=> i.ID==con.Item1);
                if (con.Item1==1168)
                    Debug.Log($"Before: {conStart}->{conStop}");
                switch(con.Item2)
                {
                    case 1:

                        break;
                    case 2:
                        break;
                    default:
                        break;
                }
            }
            for (int i = 0; i < curBlock.days.Count;i++)
            {
                if (scenario.users[curBlock.source].blockedDays.Contains(curBlock.days[i])) continue;
                if (scenario.users[curBlock.source].boxes[curBlock.days[i]] == 0) continue;
                if (!scenario.schedule.Contains(curBlock)) scenario.schedule.Add(curBlock);
                /*if (curBlock.source == "HLS-Docked")
                {
                    Debug.Log($"Source:{curBlock.source}\tI:{i}\tcurBlock.days[i]:{curBlock.days[i]}\tTimeSpentInDay: {curBlock.timeSpentInDay[i]}\nBox before subtraction: {scenario.users[curBlock.source].boxes[curBlock.days[i]]}");
                }*/
                scenario.users[curBlock.source].boxes[curBlock.days[i]] -= curBlock.timeSpentInDay[i];
                /*if (curBlock.source == "HLS-Docked")
                {
                    Debug.Log($"Source:{curBlock.source}\tI:{i}\tcurBlock.days[i]:{curBlock.days[i]}\tTimeSpentInDay: {curBlock.timeSpentInDay[i]}\nBox after subtraction: {scenario.users[curBlock.source].boxes[curBlock.days[i]]}");
                }*/
                //if (scenario.users[curBlock.source].boxes[curBlock.days[i]] < 0) scenario.users[curBlock.source].boxes[curBlock.days[i]] = 0;
                if (scenario.users[curBlock.source].boxes[curBlock.days[i]] <= 0)
                {
                    scenario.users[curBlock.source].blockedDays.Add(curBlock.days[i]);
                }
            }


        }
        scenario.users.ToList();
        string json = JsonConvert.SerializeObject(scenario.users, Formatting.Indented);
        System.IO.File.WriteAllText (@$"PostDFSUsers.txt", json);

        List<int> ScheduleIDs = (from x in scenario.schedule select x.ID).ToList();
        Debug.Log(string.Join(", ", ScheduleIDs));
        json = JsonConvert.SerializeObject(scenario.schedule, Formatting.Indented);
        System.IO.File.WriteAllText(@$"Schedule.txt", json);
        System.Diagnostics.Process.Start(@"Assets\Code\scheduler\json2csv.exe", $"Schedule.txt Assets/Code/scheduler/{date}/ScheduleCSV.csv").WaitForExit();
    }
   /* public static void test()
    {
        Window finded = scenario.windows.Find(item => item.ID==6);
        foreach(var con in finded.conflicts)
            Debug.Log($"ID: {con.ID}, Source: {con.source}, Destination: {con.destination}, Start: {con.start}, Stop: {con.stop}");
    }*/

}


public class Scenario
{
    public string epochTime;
    public string fileGenDate;
    public List<Window> windows;
    public List<Window> schedule = new List<Window>();
    public Dictionary<string, User> users = new Dictionary<string, User>();
}

public class Window
{
    public int ID;
    public string frequency;
    public string source; //user
    public string destination;
    public double start;
    public double stop;
    public double duration;

    [JsonIgnore]
    public int Freq_Priority;
    [JsonIgnore]
    public double Schedule_Priority;
    [JsonIgnore]
    public double Ground_Priority;

    //public double rate;
    //public double latency;
    public List<(int, int)> conflicts = new List<(int,int)>();
    public List<int> days;
    public List<double> timeSpentInDay;
}

public class User
{
    public int numDays;
    public double serviceLevel;
    public double timeIntervalStart;
    public double priority;
    public double timeIntervalStop;
    //public List<(double box, double timeInBox)> boxes = new List<(double, double)>();
    public Dictionary<double, double> boxes = new Dictionary<double, double>();
    public List<int> blockedDays = new List<int>();
    public List<string> allowedProviders = new List<string>();
    public string print()
    {
        return $"NumDays: {numDays}\ttimeStart:{timeIntervalStart}\ttimeStop{timeIntervalStop}";
    }

}
