using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class uiBottomPanelController : MonoBehaviour
{
    public bool visible {get; private set;} = true;
    public RectTransform rt, arrow;

    public void show() {
        if (visible) return;
        visible = true;
        rt.anchoredPosition = new Vector2(0, 0.9f);
        arrow.localEulerAngles = new Vector3(0, 0, 0);
    }

    public void hide() {
        if (!visible) return;
        visible = false;
        rt.anchoredPosition = new Vector2(-225f * rt.localScale.x, 0.9f);
        arrow.localEulerAngles = new Vector3(0, 0, 180);

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("ui/bottomBar/defaultPanel")) {
            go.GetComponent<uiDefaultPanel>().hide();
        }
    }

    public void toggle() {
        if (visible) hide();
        else show();
    }

    public void quit() {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
