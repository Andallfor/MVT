using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class uiHelper
{
    public static Canvas canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
    public static void drawTextOverObject(TextMeshProUGUI text, Vector3 dest)
    {
        Vector3 screenSize = new Vector3(Screen.width, Screen.height, 0);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(dest) - (screenSize / 2f);
        
        screenPos /= canvas.scaleFactor;
        text.rectTransform.anchoredPosition = screenPos;

        if (screenPos.z < 0) text.enabled = false;
        else if (!text.enabled) text.enabled = true;
    }

    public static Vector3 vRotate(float pitch, float roll, float yaw, Vector3 pos)
    {
        float Axx = Mathf.Cos(yaw) * Mathf.Cos(pitch);
        float Axy = Mathf.Cos(yaw) * Mathf.Sin(pitch) * Mathf.Sin(roll) - Mathf.Sin(yaw) * Mathf.Cos(roll);
        float Axz = Mathf.Cos(yaw) * Mathf.Sin(pitch) * Mathf.Cos(roll) + Mathf.Sin(yaw) * Mathf.Sin(roll);

        float Ayx = Mathf.Sin(yaw) * Mathf.Cos(pitch);
        float Ayy = Mathf.Sin(yaw) * Mathf.Sin(pitch) * Mathf.Sin(roll) + Mathf.Cos(yaw) * Mathf.Cos(roll);
        float Ayz = Mathf.Sin(yaw) * Mathf.Sin(pitch) * Mathf.Cos(roll) - Mathf.Cos(yaw) * Mathf.Sin(roll);

        float Azx = -Mathf.Sin(pitch);
        float Azy = Mathf.Cos(pitch) * Mathf.Sin(roll);
        float Azz = Mathf.Cos(pitch) * Mathf.Cos(roll);

        Vector3 p = new Vector3(
            Axx * pos.x + Axy * pos.y + Axz * pos.z,
            Ayx * pos.x + Ayy * pos.y + Ayz * pos.z,
            Azx * pos.x + Azy * pos.y + Azz * pos.z);
        
        return p;
    }
}
