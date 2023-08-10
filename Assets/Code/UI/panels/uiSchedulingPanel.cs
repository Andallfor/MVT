using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Linq;
using System.IO;
using UnityEngine.UI;



public class uiSchedulingPanel : MonoBehaviour
{
    private double RAAN, AOP, Eccentricity, SMAxis, MeanAnom, Inclination, epochTime, duration,startTime;
    public Toggle SA2, ASF1, ASF2, ASF3, BGS, GLC, HBK, KIR, MG1, PA1, SG1, SG2, SG3, SG12, SING, TR2, TR3, USA1, USA3, USA4, USA5, USD1, USH1, USH2, WG2, WG1, WS1;
    private bool[] stations;
    private Toggle[] toggles;

    private string satName;
    private planet centerBody;
    private ArrayList arlist;
    
    void Start(){
        arlist = new ArrayList();
        toggles = new Toggle[]{SA2, ASF1, ASF2, ASF3, BGS, GLC, HBK, KIR, MG1, PA1, SG1, SG2, SG3, SG12, SING, TR2, TR3, USA1, USA3, USA4, USA5, USD1, USH1, USH2, WG2, WG1, WS1};
        stations = new bool[27];
    }
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

    public void ReadET(string s)
    {
        epochTime = Time.strDateToJulian(s);
    }

    public void ReadCB(string s)
    {
        centerBody = master.allPlanets.Find(x => x.name == s);
    }

    public void ReadDuration(string s){
        duration = double.Parse(s);
    }

    public void ReadStart(string s){
        startTime = Time.strDateToJulian(s);
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
        satName = s;
    }
    public void togglestuff(){
        int b = 0;
        for(int i = 0; i < 27; i++){
            stations[i] = false;
        }
        foreach(Toggle t in toggles){
            stations[b] = t.isOn;
            b++;
        }              
    }

    public void AryaGenButton()
    {
        List<string> antennas = new List<string>();
       
            for (int i = 0; i < 27; i++)
            {
                if (stations[i])
                {
                    if (i == 0) antennas.Add("SA2");
                    else if (i == 1) antennas.Add("ASF1");
                    else if (i == 2) antennas.Add("ASF2");
                    else if (i == 3) antennas.Add("ASF3");
                    else if (i == 4) antennas.Add("BGS");
                    else if (i == 5) antennas.Add("GLC");
                    else if (i == 6) antennas.Add("HBK");
                    else if (i == 7) antennas.Add("KIR");
                    else if (i == 8) antennas.Add("MG1");
                    else if (i == 9) antennas.Add("PA1");
                    else if (i == 10) antennas.Add("SG1");
                    else if (i == 11) antennas.Add("SG2");
                    else if (i == 12) antennas.Add("SG3");
                    else if (i == 13) antennas.Add("SG12");
                    else if (i == 14) antennas.Add("SING");
                    else if (i == 15) antennas.Add("TR2");
                    else if (i == 16) antennas.Add("TR3");
                    else if (i == 17) antennas.Add("USA1");
                    else if (i == 18) antennas.Add("USA3");
                    else if (i == 19) antennas.Add("USA4");
                    else if (i == 20) antennas.Add("USA5");
                    else if (i == 21) antennas.Add("USD1");
                    else if (i == 22) antennas.Add("USH1");
                    else if (i == 23) antennas.Add("USH2");
                    else if (i == 24) antennas.Add("WG2");
                    else if (i == 25) antennas.Add("WG1");
                    else if (i == 26) antennas.Add("WS1");
                }
            }
        

        if(web.isClient)
        {

        }
        else
        {
            List<ScheduleStructGenerator.Window> windows = new List<ScheduleStructGenerator.Window>();
            windows = webScheduling.runAccessCalls(startTime);

            List<ScheduleStructGenerator.Window> userWindows = new List<ScheduleStructGenerator.Window>();
            userWindows = webScheduling.runUserAccessCalls(startTime, master.userGenerated, antennas);

            foreach (ScheduleStructGenerator.Window x in userWindows)
            {
                windows.Add(x);
            }
            Debug.Log("Done");
        }
    }

    public void CreateSat()
    {
        representationData rd = new representationData("planet", "defaultMat");
        satellite sat = new satellite(satName, new satelliteData(new Timeline(SMAxis, Eccentricity, Inclination, AOP, RAAN, MeanAnom, 1, epochTime, master.planetMu[centerBody.name])), rd, centerBody);
        master.parentBody.Add(sat, centerBody);
        master.relationshipSatellite[centerBody].Add(sat);
        linkBudgeting.users.Add(sat.name, (false, 0, 1));
        master.userGenerated.Add(sat);

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
