using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class uiTimeController : MonoBehaviour
{
    private TextMeshProUGUI gregorian, julian;
    private GameObject timeline;
    private List<GameObject> nearbyTimes;
    private Vector3 lastMousePosition = new Vector3();
    private float acceleration;
    void Start()
    {
        gregorian = GameObject.FindGameObjectWithTag("ui/time/display/gregorian").GetComponent<TextMeshProUGUI>();
        julian = GameObject.FindGameObjectWithTag("ui/time/display/julian").GetComponent<TextMeshProUGUI>();
        timeline = GameObject.FindGameObjectWithTag("ui/time/slider");
        lastMousePosition = Input.mousePosition;
        master.onPauseChange += onPauseChange;
    }

    void Update()
    {
        float totalTimeChange = 0;

        gregorian.text = master.time.date.ToString("M/d/yy HH':'mm 'UTC'");
        julian.text = Math.Round(master.time.julian, 3).ToString();

        if (Input.GetKeyDown("space")) master.pause = !master.pause;

        if (Input.GetMouseButton(0) && EventSystem.current.IsPointerOverGameObject() &&
            EventSystem.current.currentSelectedGameObject != null)
        {
            if (EventSystem.current.currentSelectedGameObject.CompareTag("ui/time/slider"))
            {
                master.pause = true;
                float xDiff = lastMousePosition.x - Input.mousePosition.x;
                if (xDiff != 0)
                {
                    totalTimeChange += 2f * xDiff / Screen.width;
                    acceleration += 2f * xDiff / Screen.width;
                } else acceleration = 0;
            }
        }

        float loss = acceleration - (acceleration * 0.1f);
        acceleration -= loss * UnityEngine.Time.deltaTime;
        if (Mathf.Abs(acceleration) < (0.5f / Screen.width)) acceleration = 0;
        if (acceleration != 0) totalTimeChange += acceleration / 100f;

        lastMousePosition = Input.mousePosition;

        master.tickStart(new Time(master.time.julian + totalTimeChange));
        master.time.addJulianTime(totalTimeChange, true);
    }

    private void onPauseChange(object sender, EventArgs args)
    {
        acceleration = 0;
    }
}
