using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class navbuttonhelper : MonoBehaviour
{
 


    public Button NavMenuButton;
    public Button ESCNavMenu;
    public GameObject menuToHide1;
    // Start is called before the first frame update
    void Start()
    {
        NavMenuButton.onClick.AddListener(TaskOnClick);
        ESCNavMenu.onClick.AddListener(ESCTaskOnClick);
    }

    // Update is called once per frame
    void TaskOnClick(){
        Debug.Log("nav menu button clicked");
        menuToHide1.gameObject.SetActive(true);
    }
    void ESCTaskOnClick(){
        Debug.Log("escnav menu button clicked");
        menuToHide1.gameObject.SetActive(false);
    }
    void Update()
    {
        
    }

}
