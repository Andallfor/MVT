using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class menucontroller : MonoBehaviour
{
 


    public Button NavMenuButton;
    public Button ESCNavMenu;
    public GameObject menuToHide1;
    public GameObject Othermenu1;
    public GameObject Othermenu2;
    public GameObject Othermenu3;
    public GameObject Othermenu4;

    // Start is called before the first frame update
    void Start()
    {
        NavMenuButton.onClick.AddListener(TaskOnClick);
        ESCNavMenu.onClick.AddListener(ESCTaskOnClick);
        menuToHide1.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Othermenuinactive(){
        Othermenu1.gameObject.SetActive(false);
        Othermenu2.gameObject.SetActive(false);
        Othermenu3.gameObject.SetActive(false);
        Othermenu4.gameObject.SetActive(false);
    }
    void Othermenuactive(){
        Othermenu1.gameObject.SetActive(true);
        Othermenu2.gameObject.SetActive(true);
        Othermenu3.gameObject.SetActive(true);
        Othermenu4.gameObject.SetActive(true);
    }
    void TaskOnClick(){
        
        menuToHide1.gameObject.SetActive(true);
        NavMenuButton.gameObject.SetActive(false);
        Othermenuinactive();
    }
    void ESCTaskOnClick(){
        
        menuToHide1.gameObject.SetActive(false);
        NavMenuButton.gameObject.SetActive(true);
        Othermenuactive();
    }
    void Update()
    {
        
    }

}
