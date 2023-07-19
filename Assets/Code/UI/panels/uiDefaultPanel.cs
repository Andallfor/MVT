using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class uiDefaultPanel : MonoBehaviour
{
    public Button exit, start;
    public bool visible {get; private set;} = false;

    private List<uiDefaultPanel> panels = new List<uiDefaultPanel>();

    public void Awake() {
        hide();
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("ui/bottomBar/defaultPanel")) {
            if (go == this.gameObject) continue;
            panels.Add(go.GetComponent<uiDefaultPanel>());   
        }

        exit.onClick.AddListener(() => {hide();});
        start.onClick.AddListener(() => {
            foreach (uiDefaultPanel panel in panels) panel.hide();
            if (visible) hide();
            else show();
        });
    }

    public void hide() {
        this.gameObject.transform.GetChild(0).gameObject.SetActive(false);
        visible = false;
    }

    public void show() {
        this.gameObject.transform.GetChild(0).gameObject.SetActive(true);
        visible = true;
    }
}
