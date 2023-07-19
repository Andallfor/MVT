using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ambientlight : MonoBehaviour
{
    // Start is called before the first frame update
    public Light ambient;
    public Button ambientbutton;
    void Start()
    {
        ambientbutton.onClick.AddListener(setambient);
        ambient.enabled=false;
    }

    // Update is called once per frame
    void setambient(){
        if(ambient.enabled==true){
            ambient.enabled=false;
        }else{
            ambient.enabled=true;
        }
    }

}
