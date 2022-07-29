using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHelper1 : MonoBehaviour
{
    public Button NavMenuButton;
    public GameObject menuToHide1;
    // Start is called before the first frame update
    void Start()
    {
        NavMenuButton.onClick.AddListener(TaskOnClick);
    }

    // Update is called once per frame
    void TaskOnClick(){
        Debug.Log("nav menu button clicked");
        menuToHide1.gameObject.SetActive(false);
    }
    void Update()
    {
        
    }
}
