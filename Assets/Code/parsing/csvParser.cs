using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;

public static class csvParser
{
    /// <summary> Returns a dictionary (tilename, all data in line). Note that the tilename will be repeated in the value. </summary>
    public static Dictionary<string, string> loadSentinelTiles(string path) {
        // does not parse the value as it would unnecssary. we only use a few tiles so parsing all 57k would be wasteful
        string[] data = File.ReadAllText(path).Split('\n');

        Dictionary<string, string> output = new Dictionary<string, string>();
        // the first 5 chars is always the tile name
        foreach (string line in data) output[general.combineCharArray(line.Take(5).ToArray())] = line;

        return output;
    }

    public static Timeline loadPlanetCsv(string path, double timestep)
    {
        StringBuilder formatted = new StringBuilder();
        Dictionary<Time, position> processed = new Dictionary<Time, position>();
        TextAsset data = (TextAsset) Resources.Load(path);

        // if were using an abs path instead of a file in Resources
        if (ReferenceEquals(data, null))
        {
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream) formatted.Append(sr.ReadLine());
            }
        }
        else formatted.Append(data.ToString());

        return _loadPlanetCsv(formatted.ToString(), timestep);
    }

    public static Timeline loadPlanetCsv(TextAsset data, double timestep) => _loadPlanetCsv(data.ToString(), timestep);

    public static List<Timeline> loadMultiplePlanetCsvs(TextAsset[] ta, double timestep)
    {
        List<Timeline> multi = new List<Timeline>();
        foreach (TextAsset t in ta)
        {
            multi.Add(_loadPlanetCsv(t.ToString(), timestep));
        }

        return multi;
    }

    private static Timeline _loadPlanetCsv(string ss, double timestep)
    {
        List<string> formatted = ss.Split('\n').ToList();
        Dictionary<double, position> processed = new Dictionary<double, position>();
        foreach (string f in formatted)
        {
            string[] s = f.Split(',');
            if (s.Length != 4) continue; // someTimes happens at end of file

            double t = double.Parse(s[0], System.Globalization.NumberStyles.Any);
            position p = new position(
                double.Parse(s[1], System.Globalization.NumberStyles.Any),
                double.Parse(s[2], System.Globalization.NumberStyles.Any),
                double.Parse(s[3], System.Globalization.NumberStyles.Any));
            
            processed.Add(t, p);
        }

        return new Timeline(processed, timestep);
    }

    public static IEnumerable<facilityListStruct> loadFacilites(string path)
    {
        Regex splitter = new Regex("," + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        TextAsset data = Resources.Load(path) as TextAsset;
        List<string> formatted = data.ToString().Split('\n').ToList();

        int index = 0;
        foreach (string line in formatted)
        {
            if (index == 0)
            {
                index++;
                continue;
            }
            List<string> s = splitter.Split(line).ToList();

            if (master.allFacilites.Exists(x => x.name == s[2])) continue;

            yield return new facilityListStruct(s[2], new geographic(
                double.Parse(s[6], System.Globalization.NumberStyles.Any),
                double.Parse(s[7], System.Globalization.NumberStyles.Any)));
        }
    }

    public static void loadScheduling(string path)
    {
        TextAsset data = Resources.Load(path) as TextAsset;
        List<string> formatted = data.ToString().Split('\n').ToList();

        Dictionary<string, List<scheduling>> dict = new Dictionary<string, List<scheduling>>();
        // load all schedules
        foreach (string line in formatted)
        {
            List<string> s = line.Split(',').ToList();

            string satName = s[0];
            string facName = s[4];

            if (abbreviations.ContainsKey(satName)) satName = abbreviations[satName];

            schedulingTime time = new schedulingTime(parseDateTime(s[1]).julian, parseDateTime(s[2]).julian);

            satellite connection = master.allSatellites.Find(x => x.name == satName);
            facility connector = master.allFacilites.Find(x => x.name == facName);
            if (ReferenceEquals(connection, null) || ReferenceEquals(connector, null)) continue;

            // handling all cases of if a facility has already been added or not
            if (dict.ContainsKey(facName))
            {
                if (!ReferenceEquals(dict[facName], null) && dict[facName].Exists(x => x.connectingTo.name == satName))
                {
                    scheduling sch = dict[facName].Find(x => x.connectingTo.name == satName);
                    sch.times.Add(time);
                }
                else
                {
                    dict[facName].Add(new scheduling(
                        master.allSatellites.Find(x => x.name == satName), 
                        new List<schedulingTime>() {time}));
                }
            }
            else
            {
                dict[facName] = new List<scheduling>() {new scheduling(
                    master.allSatellites.Find(x => x.name == satName),
                    new List<schedulingTime>() {time})};
            }
        }

        // send schedules to facilities
        foreach (KeyValuePair<string, List<scheduling>> kvp in dict)
        {
            facility f = master.allFacilites.Find(x => x.name == kvp.Key);
            if (ReferenceEquals(kvp.Value, null)) continue;
            f.registerScheduling(kvp.Value);
        }
    }

    // expects in format mm/dd/yy hh:mm:ss
    public static Time parseDateTime(string dt)
    {
        List<string> t = ((dt.Split(' '))[0]).Split('/').ToList();
        t.AddRange(((dt.Split(' '))[1]).Split(':').ToList());

        return new Time(new DateTime(
            year: int.Parse("20" + t[2]),
            month: int.Parse(t[0]),
            day: int.Parse(t[1]),
            hour: int.Parse(t[3]),
            minute: int.Parse(t[4]),
            second: int.Parse(t[5])));
    }

    public static Dictionary<string, Timeline> loadRpt(string path)
    {
        string[] data = File.ReadAllText(path).Split('\n');

        Dictionary<string, Timeline> dataHolder = new Dictionary<string, Timeline>();
        foreach (string l in data)
        {
            // Semi-major axis, Eccentricity, inclination, argument of perigee, longitude of the ascending node, mean anomaly
            // mote that rpt usually has tabs, need to find way to get rid of it- add to char[] or something
            string[] line = l.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            dataHolder.Add(line[1], new Timeline(
                double.Parse(line[9], System.Globalization.NumberStyles.Any),
                double.Parse(line[10], System.Globalization.NumberStyles.Any),
                double.Parse(line[11], System.Globalization.NumberStyles.Any),
                double.Parse(line[12], System.Globalization.NumberStyles.Any),
                double.Parse(line[13], System.Globalization.NumberStyles.Any), 
                double.Parse(line[14], System.Globalization.NumberStyles.Any),
                3000, new Time(DateTime.Parse(line[2] + " " + line[3])).julian,
                0.3986e6));
        }

        return dataHolder;
    }

    public readonly static Dictionary<string, string> abbreviations = new Dictionary<string, string>() {
        {"ACE", "ACE"},
        {"AIM", "AIM"},
        {"AQA", "AQUA"},
        {"AUR", "AURA"},
        {"DSCO", "DSCOVR"},
        {"GF2", "GRACE-FO2"},
        {"HST", "HST"},
        {"IC2", "ICESAT-2"},
        {"ICN", "ICON"},
        {"IRI", "IRIS"},
        {"L7", "LANDSAT-7"},
        {"LS8", "LANDSAT-8"},
        {"LRO", "LRO"},
        {"MEA", "METOP A"},
        {"MEB", "METOP B"},
        {"MEC", "METOP C"},
        {"MS1", "MMS-1"},
        {"MS2", "MMS-2"},
        {"MS3", "MMS-3"},
        {"MS4", "MMS-4"},
        {"NUS", "NUSTAR"},
        {"OC2", "OCO-2"},
        {"SCI", "SCISAT-1"},
        {"SE1", "SEAHAWK-1"},
        {"SMP", "SMAP"},
        {"SOLB", "SOLAR-B"},
        {"SWIFT", "SWIFT"},
        {"TD3", "TDRS 3"},
        {"TD5", "TDRS 5"},
        {"TERRA", "TERRA"},
        {"THA", "THEMIS A"},
        {"THB", "THEMIS B"},
        {"THC", "THEMIS C"},
        {"THD", "THEMIS D"},
        {"THE", "THEMIS E"},
        {"AM1", "TERRA"}};
}


public struct facilityListStruct
{
    public geographic geo;
    public string name;

    public facilityListStruct(string name, geographic geo)
    {
        this.geo = geo;
        this.name = name;
    }
}