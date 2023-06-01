using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class planetOverviewUI : MonoBehaviour
{
    public Button back;
    public Toggle toggleSat, toggleMoon, toggleLine;
    public GameObject disclaimer;

    public float rotationalOffset;
    public string focus;

    public void Update() {
        rotationalOffset = planetOverview.rotationalOffset;
        focus = planetOverview.focus.name;
    }
}
