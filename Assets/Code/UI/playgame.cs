using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class playgame : MonoBehaviour
{
    // Start is called before the first frame update
    public Canvas info, controls, scenario;
    public Button Info, Control, Scenario;
    
    public int Taptime;
    public float resetTimer;
    
    IEnumerator ResetTapTimes(){
        yield return new WaitForSeconds(resetTimer);
        Taptime=0;
    }

    //public Button Info, Control, Scenario;
    void Start(){
        hidebuttons();
        Info.onClick.AddListener(genclick);
        Scenario.onClick.AddListener(genclick);
        Control.onClick.AddListener(genclick);

    }
    void Update(){
        

        if(Taptime>=2){
            Taptime=0;
            hidebuttons();
            
        }
    }
    public void genclick(){
        Taptime++;
    }
    public void test(){
        Debug.Log("clicked");
    }
    
    public void hidebuttons(){
        info.enabled=false;
        controls.enabled=false;
        scenario.enabled=false;
    }
    public void Playgame(){
        SceneManager.LoadScene(1);
    }
    public void showinfo(){
        
        hidebuttons();
        info.enabled=true;
        
    }
    public void hideinfo(){
        info.enabled=false;
    }
    public void showcontrols(){
        
        hidebuttons();
        controls.enabled=true;
    }
    public void hidecontrols(){
        controls.enabled=false;
        
    }
    public void showscenario(){
        
        hidebuttons();
        scenario.enabled=true;
    }
    public void hidescenario(){
        scenario.enabled=false;
        
    }
}
