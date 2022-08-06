using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class uiToggleClear : MonoBehaviour
{
    public List<TMP_InputField> fields = new List<TMP_InputField>();
    public Button button;

    public void Awake() {
        button.onClick.AddListener(onClick);
    }

    private void onClick() {
        foreach (TMP_InputField input in fields) {
            input.text = "";
            input.onEndEdit.Invoke("");
        }
    }
}
