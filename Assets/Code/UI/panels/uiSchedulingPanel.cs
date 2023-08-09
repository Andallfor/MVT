using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Linq;
using System.IO;


public class uiSchedulingPanel : MonoBehaviour
{
    private double RAAN, AOP, Eccentricity, SMAxis, MeanAnom, Inclination, epochTime;
    private string name;
    private planet centerBody;
    
    public void ReadRAAN(string s)
    {
        RAAN = double.Parse(s);        
    }

    public void ReadAOP(string s)
    {
        AOP = double.Parse(s);
    }

    public void ReadEccentricity(string s)
    {
        Eccentricity = double.Parse(s);
    }

    public void ReadSMAxis(string s)
    {
        SMAxis = double.Parse(s);
    }

    public void ReadStart(string s)
    {
        epochTime = Time.strDateToJulian(s);
    }

    public void ReadDuration(string s)
    {
        centerBody = master.allPlanets.Find(x => x.name == s);
    }

    public void ReadMeanAnom(string s)
    {
        MeanAnom = double.Parse(s);
    }

    public void ReadInclination(string s)
    {
        Inclination = double.Parse(s);
    }

    public void ReadName(string s)
    {
        name = s;
    }

    public void CreateSat()
    {
        representationData rd = new representationData("planet", "defaultMat");
        satellite sat = new satellite(name, new satelliteData(new Timeline(SMAxis, Eccentricity, Inclination, AOP, RAAN, MeanAnom, 1, epochTime, master.planetMu[centerBody.name])), rd, centerBody);
        master.parentBody.Add(sat, centerBody);
        master.relationshipSatellite[centerBody].Add(sat);
        linkBudgeting.users.Add(sat.name, (false, 0, 1));

        if (web.isClient)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(sat.name);
                    bw.Write(SMAxis);
                    bw.Write(Eccentricity);
                    bw.Write(Inclination);
                    bw.Write(AOP);
                    bw.Write(RAAN);
                    bw.Write(MeanAnom);
                    bw.Write(epochTime);
                    bw.Write(master.planetMu[centerBody.name]);
                    bw.Write(sat.parent.name);

                }

                data = ms.ToArray();
            }

            web.sendMessage((byte) userWebHandles.updateSatellites, data);
        }
    }
}
