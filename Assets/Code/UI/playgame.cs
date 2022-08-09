using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class playgame : MonoBehaviour
{
    // Start is called before the first frame update
    public Canvas info, controls, scenario;
    //public Button Info, Control, Scenario;
    void Start(){
        info.enabled=false;
        controls.enabled=false;
        scenario.enabled=false;
        showbuttons();
        hidebuttons();
    }
    public void test(){
        Debug.Log("clicked");
    }
    public void showbuttons(){
        //Info.enabled=true;
        //Control.enabled=true;
        //Scenario.enabled=true;
    }
    public void hidebuttons(){
        //Info.enabled=false;
        //Control.enabled=false;
        //Scenario.enabled=false;
    }
    public void Playgame(){
        SceneManager.LoadScene(1);
    }
    public void showinfo(){
        info.enabled=true;
        //hidebuttons();
        Debug.Log("clicked");
    }
    public void hideinfo(){
        info.enabled=false;
        showbuttons();
    }
    public void showcontrols(){
        controls.enabled=true;
        hidebuttons();
    }
    public void hidecontrols(){
        controls.enabled=false;
        showbuttons();
    }
    public void showscenario(){
        scenario.enabled=true;
        hidebuttons();
    }
    public void hidescenario(){
        scenario.enabled=false;
        showbuttons();
    }
}
