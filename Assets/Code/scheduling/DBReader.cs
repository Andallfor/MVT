using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Data;
using Mono.Data.Sqlite;

public static class DBReader
{
    public static readonly string mainDBPath = new Uri(Path.Combine(Application.streamingAssetsPath, "scheduling/main.db")).AbsolutePath;
    public static readonly string mainDBConnection = getDBConnection(mainDBPath);

    public static string getDBConnection(string path) => $"URI=file:{path};New=False";

    public struct apps
    {
        public static readonly string folder = Path.Combine(Application.streamingAssetsPath, "scheduling/apps");
        public static readonly string excelParser = Path.Combine(folder, "ExcelParser.exe");
        public static readonly string json2csv = Path.Combine(folder, "json2csv.exe");
        public static readonly string heatmap = Path.Combine(folder, "heatmapVerbose.exe");
        public static readonly string schedGen= Path.Combine(folder, "ScheduleDiagramGen.exe");
    }

    public struct data
    {
        public static readonly string folder = Path.Combine(Application.streamingAssetsPath, "scheduling/data");

        public static string get(string name) => Path.Combine(folder, name);
    }

    public struct output
    {
        public static void setOutputFolder(string path)
        {
            folder = path;
            id = new DirectoryInfo(path).Name;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        }

        public static void write(string name, string text)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), text);
        }

        public static string getDB(string name) => Path.Combine(folder, $"{name}_{id}.db");
        public static string get(string name, string ext) => Path.Combine(folder, $"{name}_{id}.{ext}");
        public static string getClean(string name) => Path.Combine(folder, name);

        public static string folder { get; private set; } = Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), "scheduling_output");
        public static string id { get; private set; } = "scheduling_output";
    }

    public static Dictionary<string, (string epoch, Dictionary<string, dynamic> satellites)> getData()
    {
        Debug.Log(mainDBConnection); // no idea if this works
        Dictionary<string, (string epoch, Dictionary<string, dynamic> satellites)> missions = new Dictionary<string, (string epoch, Dictionary<string, dynamic> satellites)>();
        using (var connection = new SqliteConnection(mainDBConnection))
        {
            connection.Open();
            List<(string epochDate, string missionName)> tables = new List<(string epochDate, string missionName)>();

            using (var command = connection.CreateCommand()) //creates list of tables/missions
            {
                command.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE \"%_details\" ORDER BY name;";
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string full = reader["name"].ToString();
                        Regex dateReg = new Regex("[a-zA-Z]+_[0-9]+_[0-9]+", RegexOptions.IgnoreCase);
                        string EpochDate = dateReg.Match(full).ToString();
                        // Debug.Log($"full: {full}\tReadEpochDate: {EpochDate}");
                        string misName = full.Remove(full.IndexOf(EpochDate) - 1);
                        tables.Add((EpochDate, misName));
                    }
                    reader.Close();
                }
            }

            List<string> Bands = new List<string>();
            foreach (var table in tables)
            {
                string theEpoch = table.epochDate;
                Dictionary<string, dynamic> sats = new Dictionary<string, dynamic>();
                using (var command = connection.CreateCommand())
                {
                    string TableName = table.missionName + "_" + table.epochDate;
                    command.CommandText = $"SELECT * FROM \"{TableName}\"";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
                        foreach (int i in Enumerable.Range(0, reader.FieldCount))
                        {
                            if (reader[i] == System.DBNull.Value) continue;
                            if (String.Equals(reader[i], table.missionName + "_" + reader["Name"]))
                            {
                                if (!Bands.Contains(reader.GetName(i))) Bands.Add(reader.GetName(i));
                                continue;
                            }
                            data.Add(reader.GetName(i), reader[i]);
                        }
                        sats.Add((string)reader["Name"], data);
                    }
                    reader.Close();

                }
                missions.Add(table.missionName, (table.epochDate, sats));
            }

            foreach (string band in Bands)
            {
                //Debug.Log($"Band: {band}");
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {band}_details";
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        string ID = (string)reader[0];
                        Regex misReg = new Regex(".*_");
                        string misName = misReg.Match(ID).ToString().Replace("__", "_");
                        if (misName.EndsWith("_")) misName = misName.Remove(misName.Length - 1, 1);
                        string satName = ID.Remove(0, misReg.Match(ID).ToString().Length);
                        if (satName.StartsWith("_")) satName = satName.Remove(0, 1);
                        Dictionary<string, dynamic> bandValues = new Dictionary<string, dynamic>();

                        foreach (int i in Enumerable.Range(1, reader.FieldCount - 1))
                        {
                            if (reader[i] == System.DBNull.Value) continue;
                            bandValues.Add(reader.GetName(i), reader[i]);
                        }
                        //foreach(KeyValuePair<string, dynamic> kvp in missions[misName].satellites[satName]) Debug.Log($"Key: {kvp.Key}\t\tValue:{kvp.Value}");
                        if (missions.ContainsKey(misName) && missions[misName].satellites.ContainsKey(satName) && !(missions[misName].satellites[satName].ContainsKey(band)))
                            missions[misName].satellites[satName].Add(band, bandValues);
                    }
                    reader.Close();
                }
            }
            //foreach(KeyValuePair<string, dynamic> kvp in sats) Debug.Log($"Key: {kvp.Key}\t\tValue:{kvp.Value}");

        }
        return missions;
    }
}