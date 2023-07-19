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
using TreeEditor;
using static UnityEngine.GraphicsBuffer;


public static class ScheduleStructGenerator
{
    public static Scenario scenario = new Scenario();
    public static void genDB(dynamic missionStructure, string misName, string JSONPath, string date, string dbName)
    {
        //Select * from Windows_data where Source="HLS-Surface" and Destination="LCN-12hourfrozen-1-LowPower" and Frequency="Ka Band" order by Start ASC
        scenario.users.Clear();
        if (File.Exists(@$"Assets/Code/scheduler/{date}/{dbName}_{date}.db"))
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
        string restrictionPath = $"Assets/Code/scheduler/restrictions2023.json";
        JObject restrictionJson = JObject.Parse(File.ReadAllText(restrictionPath));
        string filePath = $"Assets/Resources/SchedulingJSONS/{JSONPath}";
        JObject json = JObject.Parse(File.ReadAllText(filePath));
        scenario.epochTime = (string)json["epochTime"];
        scenario.fileGenDate = (string)json["fileGenDate"];
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
            switch ((string)window["frequency"])
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
                default:
                    freqPrio = 0;
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
                    servicePeriod += Double.Parse(stringServicePeriod.Remove(stringServicePeriod.LastIndexOf(" Day"))) - 1;
                }
                catch (KeyNotFoundException)
                {
                    //Debug.Log($"Either satellite: {wind.source} or service period didn't exist");
                }

                currentUser.numDays = (int)(30 / servicePeriod);
                currentUser.serviceLevel = service_Level;
                try
                {
                    currentUser.priority = (double)missionStructure[misName].Item2[source]["Schedule_Priority"];
                }
                catch
                {
                    Debug.Log($"Priority error: MisName: {misName}, source: {source}");
                    Application.Quit(10);
                }
                currentUser.timeIntervalStart = (double)missionStructure[misName].Item2[source]["TimeInterval_start"];
                currentUser.timeIntervalStop = (double)missionStructure[misName].Item2[source]["TimeInterval_stop"];
                for (double i = 0; i <= currentUser.numDays; i += servicePeriod)
                {

                    (double, double) curBox = (i, 0);
                    if (i == Math.Floor(currentUser.timeIntervalStart) && currentUser.timeIntervalStart % 1 != 0)
                    {
                        curBox.Item2 = (1 - (currentUser.timeIntervalStart - i)) * currentUser.serviceLevel;
                    }
                    //else if (i == Math.Floor(currentUser.timeIntervalStop) && currentUser.timeIntervalStop%1!=0)
                    //{
                    //curBox.Item2 = (currentUser.timeIntervalStop-i)*currentUser.serviceLevel;
                    //}
                    else if (i == Math.Floor(currentUser.timeIntervalStop))
                    {
                        curBox.Item2 = (currentUser.timeIntervalStop - i) * currentUser.serviceLevel;
                    }
                    else if (currentUser.timeIntervalStart <= i && i <= currentUser.timeIntervalStop)
                    {
                        curBox.Item2 = currentUser.serviceLevel;
                    }
                    currentUser.boxes.Add(curBox.Item1, curBox.Item2);
                }
                try
                {
                    foreach (var provider in restrictionJson[source]) currentUser.allowedProviders.Add((string)provider);
                }
                catch { }
                scenario.users.Add(source, currentUser);
            }
            /*if (!(scenario.users[source].allowedProviders.Contains(destination)))
            {
                Debug.Log($"Window kicked out because {source} is not allowed to talk to {destination}");
                continue;
            }*/
            foreach (var block in window["windows"])
            {
                Window wind = new Window();
                wind.ID = count;
                double start = (double)block[0];
                double stop = (double)block[1];
                if (start == stop) continue;
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
                wind.duration = stop - start;
                //wind.latency = (double)block[2];
                wind.days = Enumerable.Range((int)Math.Floor(start), (int)Math.Floor(stop) - (int)Math.Floor(start) + 1).ToList();
                wind.timeSpentInDay = new List<double>();
                if (wind.days.Count > 1)
                {
                    wind.timeSpentInDay.Add(1 - (start - wind.days[0]));
                    foreach (int day in Enumerable.Range(wind.days[0], wind.days[wind.days.Count - 1] - 1 - wind.days[0])) wind.timeSpentInDay.Add(1);
                    wind.timeSpentInDay.Add(stop - wind.days[wind.days.Count - 1]);
                }
                else wind.timeSpentInDay.Add(stop - start);
                //Debug.Log($"Start: {start}, Stop: {stop}\t\t{string.Join(", ", wind.days)}\t{string.Join(", ", wind.timeSpentInDay)}");



                if (!fileExists)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                    INSERT INTO Windows_data (Block_ID, Source, Destination, Start, Stop, Frequency, Freq_Priority, Schedule_Priority, Ground_Priority, Service_Level) VALUES 
                    ({wind.ID},""{source}"",""{destination}"",{start},{stop},""{(string)window["frequency"]}"", {freqPrio}, {schedPrio}, {groundPrio}, {service_Level})";
                    command.ExecuteNonQuery();
                }
                count += 1;
                //if (!(scenario.windows==null) || !scenario.windows.Any(x=>x.ID==wind.ID))
                if (scenario.windows == null)
                    windList.Add(wind);
                else if (scenario.windows.Any(x => x.ID == wind.ID))
                    windList.Add(wind);
            }
        }
        if (!fileExists)
        {
            var command = connection.CreateCommand();
            command.CommandText = "COMMIT;";
            command.ExecuteNonQuery();
            connection.Close();
        }
        scenario.windows = windList.OrderBy(s => s.Schedule_Priority).ThenBy(s => s.Ground_Priority).ThenBy(s => s.Freq_Priority).ThenByDescending(s => s.duration).ToList();
        string debugJson = JsonConvert.SerializeObject(scenario.users, Formatting.Indented);
        System.IO.File.WriteAllText($"PreDFSUsers.txt", debugJson);
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
    public static void genDBNoJSON(dynamic missionStructure, string date, string dbName)
    {
        //Select * from Windows_data where Source="HLS-Surface" and Destination="LCN-12hourfrozen-1-LowPower" and Frequency="Ka Band" order by Start ASC
        //scenario.users.Clear();
        if (File.Exists(@$"Assets/Code/scheduler/{date}/{dbName}_{date}.db"))
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
        if (!fileExists)
        {
            var command = connection.CreateCommand();
            command.CommandText = "BEGIN;";
            command.ExecuteNonQuery();
        }
        Debug.Log("WinLength: "+ scenario.windows.Count());
        foreach (Window w in scenario.windows)
        {
            if (!fileExists)
            {
                var command = connection.CreateCommand();
                command.CommandText = $@"
            INSERT INTO Windows_data (Block_ID, Source, Destination, Start, Stop, Frequency, Freq_Priority, Schedule_Priority, Ground_Priority, Service_Level) VALUES 
            ({w.ID},""{w.source}"",""{w.destination}"",{w.start},{w.stop},""{w.frequency}"", {w.Freq_Priority}, {w.Schedule_Priority}, {w.Ground_Priority}, 1)";
                command.ExecuteNonQuery();
            }
        }

        if (!fileExists)
        {
            var command = connection.CreateCommand();
            command.CommandText = "COMMIT;";
            command.ExecuteNonQuery();
            connection.Close();
        }
        string debugJson = JsonConvert.SerializeObject(scenario.users, Formatting.Indented);
        System.IO.File.WriteAllText($"PreDFSUsers.txt", debugJson);
        //Debug.Log($@"ID: {scenario.windows[0].ID}\tSource:{scenario.windows[0].source}\tDestination{scenario.windows[0].destination}
        //\tschedPrio: {scenario.windows[0].Schedule_Priority}\tGroundPrio: {scenario.windows[0].Ground_Priority}\tFreq_Prio: {scenario.windows[0].Freq_Priority}\tDuration: {scenario.windows[0].duration}");
        /*foreach (var printWindow in scenario.windows)
        {
            string print = JsonUtility.ToJson(printWindow, true);
            Debug.Log(print);
        }*/
        connection.Close();
    }
    public static void createConflictList(string date)
    {
        SqliteConnection connection = new SqliteConnection($"URI=file:Assets/Code/scheduler/{date}/PreconWindows_{date}.db;New=False");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "BEGIN;";
        command.ExecuteNonQuery();
        for (int i = 0; i < scenario.windows.Count - 1; i++)
        {
            //Debug.Log("i=" + i + "\tcount: " + scenario.windows.Count());

            //Debug.Log(i);
            Window block = scenario.windows[i];
            if (block.ID == 31)
            {
                Debug.Log(31);
            }
            List<(int, int)> cons = new List<(int, int)>();
            int id = block.ID;
            command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Block_ID, Start, Stop from Windows_data WHERE Block_ID <> {id} AND
                (
                    (Start > {block.start} and Start < {block.stop}) OR
                    (Stop > {block.start} AND Stop < {block.stop}) OR
                    (Start <  {block.start} AND Stop > {block.stop}) OR
                    (Start = {block.start} AND Stop = {block.stop})
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
                bool itemExists = scenario.windows.Any(obj => obj.ID == conID);
                if (!itemExists) continue;
                double conStart = 0;
                /*try
                { conStart = scenario.windows.Find(i => i.ID == conID).start; }
                catch
                {
                    
                    Debug.Log("ERROR!, i="+i);
                }*/
                double conStop = scenario.windows.Find(i => i.ID == conID).stop;
                double conSchedPrio = scenario.windows.Find(i => i.ID == conID).Schedule_Priority;
                double conGroundPrio = scenario.windows.Find(i => i.ID == conID).Ground_Priority;
                int conCase = -1;
                //if(conID == 1713)
                //Debug.Log($"Before change: {scenario.windows.Find(i => i.ID == conID).start}->{scenario.windows.Find(i => i.ID == conID).stop}");
                if (block.start < conStart && conStart < block.stop && conStop > block.stop)
                    conCase = 2;
                else if (block.start < conStop && conStop < block.stop && conStart < block.start)
                    conCase = 1;
                else if (block.start < conStart && conStart < block.stop && block.start < conStop && conStop < block.stop)
                {
                    conCase = 3;
                    if (conSchedPrio < block.Schedule_Priority || (conSchedPrio==block.Schedule_Priority && conGroundPrio < block.Ground_Priority))
                    {
                        Debug.Log("Deleting index "+i);
                        Window block1 = block.ShallowCopy();
                        Window block2 = block.ShallowCopy();
                        int maxID = scenario.windows.Max(obj => obj.ID);

                        block1.ID = maxID + 1;
                        block1.stop = conStart;
                        block1.duration = block1.stop - block1.start;
                        block1.days = Enumerable.Range((int)Math.Floor(block1.start), (int)Math.Floor(block1.stop) - (int)Math.Floor(block1.start) + 1).ToList();
                        block1.timeSpentInDay = new List<double>();
                        if (block1.days.Count > 1)
                        {
                            block1.timeSpentInDay.Add(1 - (block1.start - block1.days[0]));
                            foreach (int day in Enumerable.Range(block1.days[0], block1.days[block1.days.Count - 1] - 1 - block1.days[0])) block1.timeSpentInDay.Add(1);
                            block1.timeSpentInDay.Add(block1.stop - block1.days[block1.days.Count - 1]);
                        }
                        else block1.timeSpentInDay.Add(block1.duration);

                        block2.ID = maxID + 2;
                        block2.start = conStop;
                        block2.duration = block2.stop - block2.start;
                        block2.days = Enumerable.Range((int)Math.Floor(block2.start), (int)Math.Floor(block2.stop) - (int)Math.Floor(block2.start) + 1).ToList();
                        block2.timeSpentInDay = new List<double>();
                        if (block2.days.Count > 1)
                        {
                            block2.timeSpentInDay.Add(1 - (block2.start - block2.days[0]));
                            foreach (int day in Enumerable.Range(block2.days[0], block2.days[block2.days.Count - 1] - 1 - block2.days[0])) block2.timeSpentInDay.Add(1);
                            block2.timeSpentInDay.Add(block2.stop - block2.days[block2.days.Count - 1]);
                        }
                        else block2.timeSpentInDay.Add(block2.duration);
                        scenario.windows.Add(block1);
                        scenario.windows.Add(block2);
                        scenario.windows.Remove(block);
                        i--;
                        break;
                    }
                }
                else if (block.start == conStart && conStop < block.stop)
                {
                    conCase = 4;
                }
                else if (block.start < conStart && conStop == block.stop)
                    conCase = 5;
                else if (conStart < block.start && conStop > block.stop)
                    conCase = 6;
                else if (conStart == block.start && conStop == block.stop)
                    conCase = 7;
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
        Debug.Log("after conflict, winLength=" + scenario.windows.Count());
    }

    public static void doDFS(string date)
    {
        List<int> totalConflicts = new List<int>();
        for (int w = 0; w < scenario.windows.Count(); w++)
        {
            Window curBlock = scenario.windows[w];
            //if(curBlock.ID == 375)
            //{
            //    Debug.Log("375");
           //}
            if (totalConflicts.Contains(curBlock.ID)) continue;
            for (int i = 0; i < curBlock.conflicts.Count(); i++)
            {
                
                (int, int) con = curBlock.conflicts[i];
                int conWinIndex = scenario.windows.FindIndex(i => i.ID == con.Item1);
                Window conWin = scenario.windows[conWinIndex];
               // if (conWin.ID == 375)
               // {
               //     Debug.Log("conflic 375");
               // }
                if (conWin.Schedule_Priority < curBlock.Schedule_Priority) continue;
                else if (conWin.Schedule_Priority == curBlock.Schedule_Priority && conWin.Ground_Priority < curBlock.Ground_Priority) continue;
                (bool, int, int) ST = stillConflict(curBlock, conWin);
                if (!ST.Item1) continue;
                con.Item2 = ST.Item3;
                switch (con.Item2)
                {
                    case 1:
                        conWin.stop = Math.Min(curBlock.start, conWin.stop);
                        conWin.duration = conWin.stop - conWin.start;
                        conWin.days = Enumerable.Range((int)Math.Floor(conWin.start), (int)Math.Floor(conWin.stop) - (int)Math.Floor(conWin.start) + 1).ToList();
                        conWin.timeSpentInDay = new List<double>();
                        if (conWin.days.Count > 1)
                        {
                            conWin.timeSpentInDay.Add(1 - (conWin.start - conWin.days[0]));
                            foreach (int day in Enumerable.Range(conWin.days[0], conWin.days[conWin.days.Count - 1] - 1 - conWin.days[0])) conWin.timeSpentInDay.Add(1);
                            conWin.timeSpentInDay.Add(conWin.stop - conWin.days[conWin.days.Count - 1]);
                        }
                        else conWin.timeSpentInDay.Add(conWin.duration);
                        scenario.windows[conWinIndex] = conWin;
                        break;
                    case 2:
                        conWin.start = Math.Max(curBlock.stop, conWin.start);
                        conWin.duration = conWin.stop - conWin.start;
                        conWin.days = Enumerable.Range((int)Math.Floor(conWin.start), (int)Math.Floor(conWin.stop) - (int)Math.Floor(conWin.start) + 1).ToList();
                        conWin.timeSpentInDay = new List<double>();
                        if (conWin.days.Count > 1)
                        {
                            conWin.timeSpentInDay.Add(1 - (conWin.start - conWin.days[0]));
                            foreach (int day in Enumerable.Range(conWin.days[0], conWin.days[conWin.days.Count - 1] - 1 - conWin.days[0])) conWin.timeSpentInDay.Add(1);
                            conWin.timeSpentInDay.Add(conWin.stop - conWin.days[conWin.days.Count - 1]);
                        }
                        else conWin.timeSpentInDay.Add(conWin.stop - conWin.start);
                        scenario.windows[conWinIndex] = conWin;
                        break;
                    case 3:
                        totalConflicts.Add(con.Item1);
                        break;
                    case 4:
                        totalConflicts.Add(con.Item1);
                        break;
                    case 5:
                        totalConflicts.Add(con.Item1);
                        break;
                    case 6:
                    case 7:
                    default:
                        totalConflicts.Add(con.Item1);
                        break;

                }
            }
            for (int i = 0; i < curBlock.days.Count; i++)
            {
                if (scenario.users[curBlock.source].blockedDays.Contains(curBlock.days[i])) continue;
                try
                {
                    if (scenario.users[curBlock.source].boxes[curBlock.days[i]] <= 0) continue;
                }
                catch
                {
                    Debug.Log($"Error with checking 0: source: {curBlock.source}, i: {i}, curBlock.days[i]: {curBlock.days[i]}");
                }
                if (scenario.users[curBlock.source].boxes[curBlock.days[i]] - curBlock.timeSpentInDay[i] < 0)
                {
                    curBlock.stop = curBlock.start+scenario.users[curBlock.source].boxes[curBlock.days[i]];
                    //curBlock.stop = curBlock.days[i] + scenario.users[curBlock.source].boxes[curBlock.days[i]];


                    curBlock.duration = curBlock.stop - curBlock.start;
                    curBlock.days = Enumerable.Range((int)Math.Floor(curBlock.start), (int)Math.Floor(curBlock.stop) - (int)Math.Floor(curBlock.start) + 1).ToList();
                    curBlock.timeSpentInDay = new List<double>();
                    if (curBlock.days.Count > 1)
                    {
                        curBlock.timeSpentInDay.Add(1 - (curBlock.start - curBlock.days[0]));
                        foreach (int day in Enumerable.Range(curBlock.days[0], curBlock.days[curBlock.days.Count - 1] - 1 - curBlock.days[0])) curBlock.timeSpentInDay.Add(1);
                        curBlock.timeSpentInDay.Add(curBlock.stop - curBlock.days[curBlock.days.Count - 1]);
                    }
                    else curBlock.timeSpentInDay.Add(curBlock.duration);
                }
                try
                {
                    scenario.users[curBlock.source].boxes[curBlock.days[i]] -= curBlock.timeSpentInDay[i];
                }
                catch
                {
                    Debug.Log($"ERROR: Source: {curBlock.source}, i: {i}");
                }
                if (!scenario.schedule.Contains(curBlock)) scenario.schedule.Add(curBlock);
                if (scenario.users[curBlock.source].boxes[curBlock.days[i]] <= 0)
                {
                    scenario.users[curBlock.source].blockedDays.Add(curBlock.days[i]);
                }
            }
        }
        //Debug.Log("got here 4");
        scenario.users.ToList();
        string json = JsonConvert.SerializeObject(scenario.users, Formatting.Indented);
        System.IO.File.WriteAllText(@$"PostDFSUsers.txt", json);

        List<int> ScheduleIDs = (from x in scenario.schedule select x.ID).ToList();
        Debug.Log(string.Join(", ", ScheduleIDs));

        json = JsonConvert.SerializeObject(scenario.schedule, Formatting.Indented);
        System.IO.File.WriteAllText(@$"Schedule.txt", json);
        System.Diagnostics.Process.Start(@"Assets\Code\scheduler\json2csv.exe", $"Schedule.txt Assets/Code/scheduler/{date}/ScheduleCSV_{date}.csv").WaitForExit();

        json = JsonConvert.SerializeObject(scenario.windows, Formatting.Indented);
        System.IO.File.WriteAllText(@$"FinalWindows.txt", json);
        System.Diagnostics.Process.Start(@"Assets\Code\scheduler\json2csv.exe", $"FinalWindows.txt Assets/Code/scheduler/{date}/FinalWindowsCSV_{date}.csv").WaitForExit();
        //Debug.Log("Got here 5");
    }

    private static (bool, int, int) stillConflict(Window og, Window possCon)
    {
        (bool, int, int) ret = (false, -1, -1);
        if ((possCon.start > og.start && possCon.start < og.stop) ||
            (possCon.stop > og.start && possCon.stop < og.stop) ||
            (possCon.start < og.start && possCon.stop > og.stop) ||
            (possCon.start == og.start && possCon.stop == og.stop))
        {
            ret.Item1 = true;
            ret.Item2 = possCon.ID;
            if (og.start <= possCon.start && possCon.start < og.stop && possCon.stop > og.stop)
                ret.Item3 = 2;
            else if (og.start < possCon.stop && possCon.stop <= og.stop && possCon.start < og.start)
                ret.Item3 = 1;
            else if (og.start < possCon.start && possCon.start < og.stop && og.start < possCon.stop && possCon.stop < og.stop)
                ret.Item3 = 3;
            else if (og.start == possCon.start && possCon.stop < og.stop)
                ret.Item3 = 4;
            else if (og.start < possCon.start && possCon.stop == og.stop)
                ret.Item3 = 5;
            else if (possCon.start < og.start && possCon.stop > og.stop)
                ret.Item3 = 6;
            else if (possCon.start == og.start && possCon.stop == og.stop)
                ret.Item3 = 7;
        }


        return ret;
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
        public List<(int, int)> conflicts = new List<(int, int)>();
        public List<int> days;
        public List<double> timeSpentInDay;
        public Window ShallowCopy()
        {
            return (Window)this.MemberwiseClone();
        }
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
}