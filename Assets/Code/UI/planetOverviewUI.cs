using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class planetOverviewUI : MonoBehaviour
{
    public Button back;
    public Toggle toggleSat, toggleMoon;
    public GameObject disclaimer;

    public float rotationalOffset;

    public void Update() {
        rotationalOffset = planetOverview.rotationalOffset;
    }
}
