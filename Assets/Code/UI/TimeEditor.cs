using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TimeEditor : MonoBehaviour
{
    public Button ResetButton;
    // Start is called before the first frame update
    void Start()
    {
        ResetButton.onClick.AddListener(TaskOnClickResetButton);
    }

    // Update is called once per frame
    void TaskOnClickResetButton(){
        master.time.addJulianTime(2460806.5 - master.time.julian);
    }
    void Update()
    {
        
    }
}
