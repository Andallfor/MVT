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
    private double RAAN, AOP, Eccentricity, SMAxis, MeanAnom, Inclination, epochTime, duration,starttime;
    public Toggle SA2, ASF1, ASF2, ASF3, BGS, GLC, HBK, KIR, MG1, PA1, SG1, SG2, SG3, SG12, SING, TR2, TR3, USA1, USA3, USA4, USA5, USD1, USH1, USH2, WG2, WG1, WS1;
    private bool[] stations;
    private Toggle[] toggles;

    private string nameOFSAT;
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
        duration=double.Parse(s);
    }

    public void ReadStart(string s){
        starttime= Time.strDateToJulian(s);
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
        nameOFSAT = s;
    }
    public void togglestuff(){
        int b= 0;
        for(int i = 0; i<27; i++){
            stations[i]=false;
        }
        foreach(Toggle t in toggles){
            stations[b]=t.isOn;
            b++;
        }
        

        
    }

    public void AryaGenButton(){
        //arya, write your shit here
        Debug.Log("it works");
    }

    public void CreateSat()
    {
        togglestuff();
        representationData rd = new representationData("planet", "defaultMat");
        satellite sat = new satellite(nameOFSAT, new satelliteData(new Timeline(SMAxis, Eccentricity, Inclination, AOP, RAAN, MeanAnom, 1, epochTime, master.planetMu[centerBody.name])), rd, centerBody);
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
