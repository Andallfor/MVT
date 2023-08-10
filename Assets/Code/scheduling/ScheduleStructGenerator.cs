#if (UNITY_EDITOR || UNITY_STANDALONE) && !UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Mono.Data.Sqlite;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class ScheduleStructGenerator
{
    public static Scenario scenario = new Scenario();
    public static void genDB(dynamic missionStructure, string misName, string JSONPath, string date, string dbName)
    {
        //Select * from Windows_data where Source="HLS-Surface" and Destination="LCN-12hourfrozen-1-LowPower" and Frequency="Ka Band" order by Start ASC
        scenario.users.Clear();
        string newDBPath = DBReader.output.getDB(dbName);
        if (File.Exists(newDBPath)) File.Delete(newDBPath);
        bool fileExists = false;
        SqliteConnection connection = new SqliteConnection(DBReader.getDBConnection(newDBPath));
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
        string restrictionPath = DBReader.data.get("restrictions2023.json");
        JObject restrictionJson = JObject.Parse(File.ReadAllText(restrictionPath));
        string filePath = DBReader.data.get($"schedulingJSONs/{JSONPath}");
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
                Debug.Log(source + misName + missionStructure[misName].Item2[source]["Service_Level"]);
                service_Level = (double)missionStructure[misName].Item2[source]["Service_Level"];
            }
            catch (KeyNotFoundException)
            {
                Debug.Log($"Either satellite: {source} or Service Level didn't exist");
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
        DBReader.output.write("PreDFSUsers.txt", debugJson);
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
        string newDBPath = DBReader.output.getDB(dbName);
        if (File.Exists(newDBPath)) File.Delete(newDBPath);

        bool fileExists = false;
        SqliteConnection connection = new SqliteConnection(DBReader.getDBConnection(newDBPath));
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
        Debug.Log("WinLength: " + scenario.windows.Count());
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
        DBReader.output.write("PreDFSUsers.txt", debugJson);
        //Debug.Log($@"ID: {scenario.windows[0].ID}\tSource:{scenario.windows[0].source}\tDestination{scenario.windows[0].destination}
        //\tschedPrio: {scenario.windows[0].Schedule_Priority}\tGroundPrio: {scenario.windows[0].Ground_Priority}\tFreq_Prio: {scenario.windows[0].Freq_Priority}\tDuration: {scenario.windows[0].duration}");
        /*foreach (var printWindow in scenario.windows)
        {
            string print = JsonUtility.ToJson(printWindow, true);
            Debug.Log(print);
        }*/
        connection.Close();
    }
    public static List<Window> FindConflicts(Window block, int i, List<int> divs)
    {
       /* var result = scenario.windows.Where(window =>
            window.ID != block.ID &&
            ((window.start >= block.start && window.start <= block.stop) ||
            (window.stop >= block.start && window.stop <= block.stop) ||
            (window.start < block.start && window.stop > block.stop)) &&
            (window.source == block.source || window.destination == block.destination)).ToList();
       */
        List<Window> result = new List<Window>();
        int divIndex = divs.IndexOf(divs.FirstOrDefault(x => x > i));
        for (int x = divs[divIndex]; x < scenario.windows.Count; x += divs[divIndex++])
        {
            if (scenario.windows[x].source != block.source && scenario.windows[x].destination != block.destination) continue;
            for (int y = x; y < scenario.windows.Count && scenario.windows[y].start <= block.stop; y++)
            {
                (bool, int, int) stillCon = stillConflict(block, scenario.windows[y]);
                if (stillCon.Item1) result.Add(scenario.windows[y]);
            }
        }
        return result;
    }

    public static IEnumerator createConflictList(string date, List<int> divs)
    {
        string uri = DBReader.getDBConnection(DBReader.output.getDB($"PreconWindows"));
        Debug.Log(uri);
        System.Diagnostics.Stopwatch stopWatch = new Stopwatch();
        
        for (int i = 0; i < scenario.windows.Count - 1; i++)
        {
            //Debug.Log("i=" + i + "\tcount: " + scenario.windows.Count());
            if (i==100) stopWatch.Start();
            if (i == 200)
            {
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                UnityEngine.Debug.Log("RunTime " + elapsedTime);
                yield return new WaitForSeconds(0.001f);
            }
            
            Window block = scenario.windows[i];
            List<(int, int)> cons = new List<(int, int)>();
            List<Window> conflicts = FindConflicts(block, i, divs);
            Debug.Log("i=" + i+"\tConLength = "+conflicts.Count);
            yield return new WaitForSeconds(0.001f);
            //stopWatch.Stop();
            //long ticks = stopWatch.ElapsedTicks;
            //Debug.Log("RunTime for db call: " + ticks+" ticks");
            //yield return new WaitForSeconds(0.001f);
            //stopWatch = new Stopwatch();
            //stopWatch.Reset();
            //stopWatch.Start();
            foreach (Window con in conflicts)
            {
                int conID = con.ID;
                //double conStart = scenario.windows.Find(i => i.ID == conID).start;
                bool itemExists = scenario.windows.Any(obj => obj.ID == conID);
                if (!itemExists) continue;
                double conStart = con.start;
                //double conStart = 0;
                /*try
                { conStart = scenario.windows.Find(i => i.ID == conID).start; }
                catch
                {
                    
                    Debug.Log("ERROR!, i="+i);
                }*/
                double conStop = con.stop;
                double conSchedPrio = con.Schedule_Priority;
                double conGroundPrio = con.Ground_Priority;
                int conCase = -1;
                //if(conID == 1713)
                //Debug.Log($"Before change: {scenario.windows.Find(i => i.ID == conID).start}->{scenario.windows.Find(i => i.ID == conID).stop}");
                if (block.start < conStart && conStart < block.stop && conStop > block.stop)
                    conCase = 2;
                else if (block.start < conStop && conStop < block.stop && conStart < block.start)
                    conCase = 1;
                else if (block.start < conStart && conStart < block.stop && block.start < conStop && conStop < block.stop)
                {
                    //Debug.Log("concase 3");
                    conCase = 3;
                    if (conSchedPrio < block.Schedule_Priority || (conSchedPrio == block.Schedule_Priority && conGroundPrio < block.Ground_Priority))
                    {
                        //Debug.Log("Deleting index "+i);
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

                        if (block1.duration > scenario.minWinTime && block2.duration > scenario.minWinTime)
                        {
                            scenario.windows.Add(block1);
                            scenario.windows.Add(block2);
                            scenario.windows.Remove(block);
                            i--;
                        }
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
                cons.Add((con.ID, conCase));

                //Debug.Log(reader["ID"]);
            }
            //ticks = stopWatch.ElapsedTicks;
            //Debug.Log("RunTime for ocnflict checking " + ticks + " ticks");
            //yield return new WaitForSeconds(0.001f);
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
        List<Window> tempWins = scenario.windows.OrderBy(s => s.Schedule_Priority).ThenBy(s => s.Ground_Priority).ThenBy(s => s.Freq_Priority).ThenByDescending(s => s.duration).ToList();
        scenario.windows = tempWins;
        string debugJson = JsonConvert.SerializeObject(scenario.windows, Formatting.Indented);
        DBReader.output.write("WindowsSorted.txt", debugJson);
        yield return new WaitForSeconds(0.001f);

    }

    public static void doDFS(string date)
    {


        for (int w = 0; w < scenario.windows.Count(); w++)
        {
            Window curBlock = scenario.windows[w];
            if (curBlock.duration <= scenario.minWinTime)
            {
                Debug.Log("too small, cut");
                continue;
            }
            if(curBlock.source=="THEMIS_C")
            {
                Debug.Log("1361");
            }
            if (scenario.totalConflicts.Contains(curBlock.ID)) continue;
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
                        if (conWin.duration <= scenario.minWinTime)
                        {
                            scenario.totalConflicts.Add(conWin.ID); break;
                        }
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
                        if (conWin.duration <= scenario.minWinTime)
                        {
                            scenario.totalConflicts.Add(conWin.ID); break;
                        }
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
                        scenario.totalConflicts.Add(con.Item1);
                        break;
                    case 4:
                        scenario.totalConflicts.Add(con.Item1);
                        break;
                    case 5:
                        scenario.totalConflicts.Add(con.Item1);
                        break;
                    case 6:
                    case 7:
                    default:
                        scenario.totalConflicts.Add(con.Item1);
                        break;

                }
            }
            for (int i = 0; i < curBlock.days.Count; i++)
            {
                if (scenario.users[curBlock.source].blockedDays.Contains(curBlock.days[i])) continue;
                try
                {
                    if (scenario.users[curBlock.source].boxes[curBlock.days[i]] <= -1.1*scenario.minWinTime) continue;
                }
                catch
                {
                    Debug.Log($"Error with checking 0: source: {curBlock.source}, i: {i}, curBlock.days[i]: {curBlock.days[i]}");
                }
                if (scenario.users[curBlock.source].boxes[curBlock.days[i]] <= 0)
                    continue;
                if (scenario.users[curBlock.source].boxes[curBlock.days[i]] - curBlock.timeSpentInDay[i] < 0)
                {
                    if (scenario.users[curBlock.source].boxes[curBlock.days[i]] >= scenario.minWinTime)
                    {
                        curBlock.stop = curBlock.start + scenario.users[curBlock.source].boxes[curBlock.days[i]];
                        //curBlock.stop = curBlock.days[i] + scenario.users[curBlock.source].boxes[curBlock.days[i]];
                    }
                    else
                    {
                        curBlock.stop = curBlock.start + scenario.minWinTime;
                    }

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
                    try
                    {
                        scenario.users[curBlock.source].blockedDays.Add(curBlock.days[i]);
                    }
                    catch
                    {
                        scenario.users[curBlock.source].blockedDays.Add(curBlock.days[i-1]);
                        i--;
                    }

                    
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
                if (scenario.users[curBlock.source].boxes[curBlock.days[i]] <= -1.1*scenario.minWinTime)
                {
                    scenario.users[curBlock.source].blockedDays.Add(curBlock.days[i]);
                }
            }
        }
        //Debug.Log("got here 4");
        scenario.users.ToList();
        string json = JsonConvert.SerializeObject(scenario.users, Formatting.Indented);
        DBReader.output.write("PostDFSUsers.txt", json);

        List<int> ScheduleIDs = (from x in scenario.schedule select x.ID).ToList();
        Debug.Log(string.Join(", ", ScheduleIDs));

        json = JsonConvert.SerializeObject(scenario.schedule, Formatting.Indented);
        DBReader.output.write("Schedule.txt", json);
        System.Diagnostics.Process.Start(DBReader.apps.json2csv, $"{DBReader.output.getClean("Schedule.txt")} {DBReader.output.get("ScheduleCSV", "csv")}").WaitForExit();

        json = JsonConvert.SerializeObject(scenario.windows, Formatting.Indented);
        DBReader.output.write("FinalWindows.txt", json);
        System.Diagnostics.Process.Start(DBReader.apps.json2csv, $"{DBReader.output.getClean("FinalWindows.txt")} {DBReader.output.get("FinalWindowsCSV", "csv")}").WaitForExit();
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

    public static IEnumerator doScheduleWithAccess()
    {
        if (scenario.aryasWindows.Count == 0)
        {
            Debug.Log("aryas windows empty");
            yield return new WaitForSeconds(1);
        }
        string date = DateTime.Now.ToString("MM-dd_hhmm");
        if (!File.Exists(DBReader.mainDBPath))
        {
            UnityEngine.Debug.Log("Generating main.db");
            UnityEngine.Debug.Log("command: " + $"{DBReader.data.get("2023EarthAssets")} {DBReader.mainDBPath}");
            System.Diagnostics.Process.Start(DBReader.apps.excelParser, $"{DBReader.data.get("2023EarthAssetsWithOrbits.xlsx")} {DBReader.mainDBPath}").WaitForExit();
        }
        var missionStructure = DBReader.getData();
        Debug.Log("epoch: " + missionStructure["EarthTest"].epoch);
        //DBReader.output.setOutputFolder(Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), date));
        //string json = JsonConvert.SerializeObject(missionStructure, Formatting.Indented);
        //DBReader.output.write("MissionStructure_2023.txt", json);
        //Select * from Windows_data where Source="HLS-Surface" and Destination="LCN-12hourfrozen-1-LowPower" and Frequency="Ka Band" order by Start ASC
        scenario.users.Clear();
        //string newDBPath = DBReader.output.getDB("PreconWindows");
        //if (File.Exists(newDBPath)) File.Delete(newDBPath);
        bool fileExists = false;
        //SqliteConnection connection = new SqliteConnection(DBReader.getDBConnection(newDBPath));
        /*if (!fileExists)
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
        }*/
        //string restrictionPath = DBReader.data.get("restrictions2023.json");
        //JObject restrictionJson = JObject.Parse(File.ReadAllText(restrictionPath));
        //string filePath = DBReader.data.get($"schedulingJSONs/{JSONPath}");
        //JObject json = JObject.Parse(File.ReadAllText(filePath));
        string misName = "EarthTest";
        scenario.epochTime = missionStructure[misName].Item1;
        scenario.fileGenDate = DateTime.Today.ToString();
        List<Window> windList = new List<Window>();
        /*if (!fileExists)
        {
            var command = connection.CreateCommand();
            command.CommandText = "BEGIN;";
            command.ExecuteNonQuery();
        }*/
        foreach (Window aWin in scenario.aryasWindows)
        {
            aWin.start = Math.Max(aWin.start, 0);
            string source = (aWin.source).Replace("\"", "");
            if (source.EndsWith("-Backup")) continue;
            string destination = aWin.destination.Replace("\"", "");
            double schedPrio = source.EndsWith("-Backup") ? 0.1 : 0;
            double service_Level = 0;
            double groundPrio = 0;
            try
            {
                Debug.Log(source + misName + missionStructure[misName].Item2[source]["Service_Level"]);
                service_Level = missionStructure[misName].Item2[source]["Service_Level"];
            }
            catch
            {
                service_Level = Double.Parse(missionStructure[misName].Item2[source]["Service_Level"]);
            }
            try
            {
                schedPrio += missionStructure[misName].Item2[source]["Schedule_Priority"];
            }
            catch
            {
                schedPrio += Double.Parse(missionStructure[misName].Item2[source]["Schedule_Priority"]);
            }
            try
            {
                groundPrio = missionStructure[misName].Item2[destination]["Ground_Priority"];
            }
            catch
            {
                groundPrio = Double.Parse(missionStructure[misName].Item2[destination]["Ground_Priority"]);
                //Debug.Log($"Either destination: {wind.destination} or ground priority didn't exist");
            }
            int freqPrio = 0;
            switch (aWin.frequency)
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
                scenario.users.Add(source, currentUser);
            }
            /*if (!(scenario.users[source].allowedProviders.Contains(destination)))
            {
                Debug.Log($"Window kicked out because {source} is not allowed to talk to {destination}");
                continue;
            }*/
            aWin.Freq_Priority = freqPrio;
            aWin.Schedule_Priority = schedPrio;
            aWin.Ground_Priority = groundPrio;
            aWin.days = Enumerable.Range((int)Math.Floor(aWin.start), (int)Math.Floor(aWin.stop) - (int)Math.Floor(aWin.start) + 1).ToList();
            aWin.timeSpentInDay = new List<double>();
            if (aWin.days.Count > 1)
            {
                aWin.timeSpentInDay.Add(1 - (aWin.start - aWin.days[0]));
                foreach (int day in Enumerable.Range(aWin.days[0], aWin.days[aWin.days.Count - 1] - 1 - aWin.days[0])) aWin.timeSpentInDay.Add(1);
                aWin.timeSpentInDay.Add(aWin.stop - aWin.days[aWin.days.Count - 1]);
            }
            else aWin.timeSpentInDay.Add(aWin.stop - aWin.start);
            //Debug.Log($"aWin.start: {aWin.start}, aWin.stop: {aWin.stop}\t\t{string.Join(", ", aWin.days)}\t{string.Join(", ", aWin.timeSpentInDay)}");



            /*if (!fileExists)
            {
                var command = connection.CreateCommand();
                command.CommandText = $@"
		INSERT INTO Windows_data (Block_ID, Source, Destination, Start, Stop, Frequency, Freq_Priority, Schedule_Priority, Ground_Priority, Service_Level) VALUES 
		({aWin.ID},""{aWin.source}"",""{aWin.destination}"",{aWin.start},{aWin.stop},""{aWin.frequency}"", {aWin.Freq_Priority}, {aWin.Schedule_Priority}, {aWin.Ground_Priority}, {scenario.users[aWin.source].serviceLevel})";
                command.ExecuteNonQuery();
            }*/
            //if (!(scenario.aWinows==null) || !scenario.aWinows.Any(x=>x.ID==aWin.ID))
            if (scenario.windows == null)
                windList.Add(aWin);
            else if (scenario.windows.Any(x => x.ID == aWin.ID))
                windList.Add(aWin);
        }
       /* if (!fileExists)
        {
            var command = connection.CreateCommand();
            command.CommandText = "COMMIT;";
            command.ExecuteNonQuery();
            connection.Close();
        }*/
        scenario.windows = windList.OrderBy(s => s.Schedule_Priority).ThenBy(s => s.Ground_Priority).ThenBy(s => s.Freq_Priority).ThenBy(s => s.start).ToList();
        string debugJson = JsonConvert.SerializeObject(scenario.users, Formatting.Indented);
        //DBReader.output.write("PreDFSUsers.txt", debugJson);
        string sortWind = JsonConvert.SerializeObject(scenario.windows, Formatting.Indented);
        //DBReader.output.write("sortedWindows.txt", sortWind);
        Debug.Log("Generated all the windows");
        List<int> dividers = new List<int>();
        string curDivUser = "";
        string curDivGS = "";
        for (int i = 0; i < scenario.windows.Count;i ++)
        {
            if (scenario.windows[i].source != curDivUser)
            {
                curDivUser = scenario.windows[i].source;
                dividers.Add(i);
            }
            if (scenario.windows[i].destination != curDivGS)
            {
                curDivGS = scenario.windows[i].destination;
                if (!dividers.Contains(i)) dividers.Add (i);
            }
        }
        //Debug.Log($@"ID: {scenario.windows[0].ID}\tSource:{scenario.windows[0].source}\tDestination{scenario.windows[0].destination}
        //\tschedPrio: {scenario.windows[0].Schedule_Priority}\tGroundPrio: {scenario.windows[0].Ground_Priority}\tFreq_Prio: {scenario.windows[0].Freq_Priority}\tDuration: {scenario.windows[0].duration}");
        /*foreach (var printWindow in scenario.windows)
        {
            string print = JsonUtility.ToJson(printWindow, true);
            Debug.Log(print);
        }*/
       // connection.Close();
        yield return new WaitForSeconds(0.001f);
        
        yield return ScheduleStructGenerator.createConflictList(date, dividers);
        Debug.Log("generated conflict list.....");
        scenario.windows = scenario.windows.OrderBy(s => s.Schedule_Priority).ThenBy(s => s.Ground_Priority).ThenBy(s => s.Freq_Priority).ThenBy(s => s.start).ToList();
        yield return new WaitForSeconds(0.5f);
        //ScheduleStructGenerator.genDBNoJSON(missionStructure, date, "cut1Windows");
        Debug.Log("genDBNoJSON");
        yield return new WaitForSeconds(0.5f);
       // ScheduleStructGenerator.createConflictList(date);
        //Debug.Log("generated conflict list again.....");
        //yield return new WaitForSeconds(0.5f);
        Debug.Log("Doing DFS.....");
        ScheduleStructGenerator.doDFS(date);
        Debug.Log("Did DFS.....");
        yield return new WaitForSeconds(0.5f);
        //Debug.Log(DBReader.output.getClean("PostDFSUsers.txt"));
        //System.Diagnostics.Process.Start(DBReader.apps.heatmap, $"{DBReader.output.getClean("PostDFSUsers.txt")} {DBReader.output.get("PostDFSUsers", "png")} 0 30 2");
        //System.Diagnostics.Process.Start(DBReader.apps.heatmap, $"{DBReader.output.getClean("PreDFSUsers.txt")} {DBReader.output.get("PreDFSUsers", "png")} 0 1 6");
        //System.Diagnostics.Process.Start(DBReader.apps.schedGen, $"{DBReader.output.get("ScheduleCSV", "csv")} source destination 0 30 {DBReader.output.get("sched", "png")} 0");
        Debug.Log("generated diagrams.....");
        yield return new WaitForSeconds(0.5f);
        
    }


    public class Scenario
    {
        public string epochTime;
        public string fileGenDate;
        //public double minWinTime = 0;
        public List<Window> aryasWindows = new List<Window>();
        public double minWinTime = 0.00347222;
        public List<int> totalConflicts = new List<int>();
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

        public Window()
        {
            this.ID = 0;
            this.frequency = "";
            this.destination = "";
            this.start = 0;
            this.stop = 0;
            this.duration = 0;
        }

        public Window(int ID, string frequency, string source, string destination, double start, double stop, double duration)
        {
            this.ID = ID;
            this.frequency = frequency;
            this.source = source;
            this.destination = destination;
            this.start = start;
            this.stop = stop;
            this.duration = duration;
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
        [JsonIgnore]
        public List<int> blockedDays = new List<int>();
        [JsonIgnore]
        public List<string> allowedProviders = new List<string>();
        public string print()
        {
            return $"NumDays: {numDays}\ttimeStart:{timeIntervalStart}\ttimeStop{timeIntervalStop}";
        }

    }
}
#endif