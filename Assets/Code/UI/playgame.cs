using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class playgame : MonoBehaviour
{
    // Start is called before the first frame update
    public Canvas info;
    void Start(){
        info.enabled=false;
    }
    public void Playgame(){
        SceneManager.LoadScene(1);
    }
    public void showinfo(){
        info.enabled=true;
    }
    public void hideinfo(){
        info.enabled=false;
    }
}
