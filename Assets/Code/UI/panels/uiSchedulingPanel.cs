using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class uiSchedulingPanel : MonoBehaviour
{
    private float RAAN, AOP, Eccentricity, SMAxis, Start, Duration, MeanAnom, Inclination;
    private string nameOFSAT;
    // Start is called before the first frame update
    
    //void Start(){}
    

    // Update is called once per frame
    
   

    
    
    public void ReadRAAN(string s){
        RAAN=float.Parse(s);
        
        
    }
    public void ReadAOP(string s){
        AOP=float.Parse(s);
    }
    public void ReadEccentricity(string s){
        Eccentricity=float.Parse(s);
    }
    public void ReadSMAxis(string s){
        SMAxis=float.Parse(s);
    }
    public void ReadStart(string s){
        Start=float.Parse(s);
    }
    public void ReadDuration(string s){
        Duration=float.Parse(s);
    }
    public void ReadMeanAnom(string s){
        MeanAnom=float.Parse(s);
    }
    public void ReadInclination(string s){
        Inclination=float.Parse(s);
    }

    public void ReadName(string s){
        nameOFSAT=s;
    }

    public void CreateSat(){
       Debug.Log("sat screated");

    }
}
