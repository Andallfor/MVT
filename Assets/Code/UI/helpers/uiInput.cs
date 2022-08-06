using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class uiInput : MonoBehaviour
{
    public bool enable = false;
    public float defaultValue, minValue, maxValue;

    [Header("Extra Options")]
    public bool isDay = false;

    private TMP_InputField input;
    private uiInput monthData, yearData;

    public float currentValue;

    public void Awake() {
        currentValue = defaultValue;
        input = this.GetComponent<TMP_InputField>();
        input.onEndEdit.AddListener(verifyInput);
        input.onSubmit.AddListener(verifyInput);
        if (isDay) {
            monthData = GameObject.FindGameObjectWithTag("ui/bottomBar/settings/month").GetComponent<uiInput>();
            yearData = GameObject.FindGameObjectWithTag("ui/bottomBar/settings/year").GetComponent<uiInput>();

            monthData.GetComponent<TMP_InputField>().onDeselect.AddListener(verifyInput);
            yearData.GetComponent<TMP_InputField>().onDeselect.AddListener(verifyInput);
        }
    }

    private void verifyInput(string s) {
        s = input.text;
        if (isDay) {
            int month = (int) monthData.currentValue;
            maxValue = daysPerMonth[month];
            
            if (month == 2 && yearData.currentValue % 4 == 0) maxValue++;
        }

        float result = 0;
        if (!float.TryParse(s, out result)) {
            input.SetTextWithoutNotify(defaultValue.ToString());
            currentValue = defaultValue;
        } else {
            if (result > maxValue) {
                input.SetTextWithoutNotify(maxValue.ToString());
                currentValue = maxValue;
            } else if (result < minValue) {
                input.SetTextWithoutNotify(minValue.ToString());
                currentValue = minValue;
            } else {
                currentValue = result;
                input.SetTextWithoutNotify(result.ToString());
            }
        }
    }

    private Dictionary<int, int> daysPerMonth = new Dictionary<int, int>() {
        {1, 31},
        {2, 28},
        {3, 31},
        {4, 30},
        {5, 31},
        {6, 30},
        {7, 31},
        {8, 31},
        {9, 30},
        {10, 31},
        {11, 30},
        {12, 31}
    };
}
