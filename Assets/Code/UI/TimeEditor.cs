using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class TimeEditor : MonoBehaviour
{
    public Button ResetButton;
    public Button ApplyChanges;
    public TMP_InputField Julian;
    private string Julianinput;
    private double settimeto;

    // Start is called before the first frame update
    void Start()
    {
        ResetButton.onClick.AddListener(TaskOnClickResetButton);
        ApplyChanges.onClick.AddListener(SpecifyTime);
    }

    // Update is called once per frame
    void TaskOnClickResetButton(){
        master.time.addJulianTime(2460806.5 - master.time.julian);
    }
    void SpecifyTime(){
        settimeto = double.Parse(Julian.text);
        if(settimeto<2460806.5){
            Debug.Log("invalid");
        }else{
            master.time.addJulianTime(settimeto-master.time.julian);
            Debug.Log(master.time.julian);
        }
        


    }
    void Update()
    {
        
    }
}
