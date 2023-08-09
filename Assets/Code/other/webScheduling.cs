using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Linq;
using System.IO;

public class webScheduling
{
    public static byte[] runner(byte[] data)
    {
        double scenarioStart;
        using (BinaryReader br = new BinaryReader(new MemoryStream(data)))
        {
            scenarioStart = br.ReadDouble();
        }
        
        master.ID = 0;
        List<satellite> users = new List<satellite>();
        List<ScheduleStructGenerator.Window> windows = new List<ScheduleStructGenerator.Window>();

        foreach (var u in linkBudgeting.users)
        {
            users.Add(master.allSatellites.Find(x => x.name == u.Key));
        }

        foreach (var p in linkBudgeting.providers)
        {
            facility provider = master.allFacilities.Find(x => x.name == p.Key);

            accessCallGeneratorWGS access = new accessCallGeneratorWGS(master.allPlanets.Find(x => x.name == "Earth"), provider.geo, users, p.Key);
            access.initialize(Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth/" + p.Key), 2);
            var output = access.findTimes(new Time(scenarioStart), new Time(scenarioStart + 5), 0.00069444444, 0.00001157407 / 2.0, true); // ADD TO WINDOWS LIST HERE
                                                                                                                                            //StartCoroutine(stall(access));
            foreach (ScheduleStructGenerator.Window w in output)
            {
                windows.Add(w);
            }
        }
        ScheduleStructGenerator.scenario.aryasWindows = windows;

        //scheduling code here

        return new byte[10];

    }

    public static byte[] updateSats(byte[] data)
    {
        using (BinaryReader br = new BinaryReader(new MemoryStream(data)))
        {
            string name = br.ReadString();
            double semiMajor = br.ReadDouble();
            double ecc = br.ReadDouble();
            double incl = br.ReadDouble();
            double argOfPer = br.ReadDouble();
            double longOfAsc = br.ReadDouble();
            double meanAnom = br.ReadDouble();
            double startTime = br.ReadDouble();
            double mu = br.ReadDouble();
            planet parent = master.allPlanets.Find(x => x.name == br.ReadString());


            satellite sat = new satellite(name, new satelliteData(new Timeline(semiMajor, ecc, incl, argOfPer, longOfAsc, meanAnom, 1, startTime, mu)), new representationData("planet", "defaultMat"), parent);
            master.parentBody.Add(sat, parent);
            master.relationshipSatellite[parent].Add(sat);
            linkBudgeting.users.Add(sat.name, (false, 0, 1));

            return null;
        }
    }
}

