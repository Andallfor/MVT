using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public static class uiHelper
{
    public static Canvas canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
    public static void drawTextOverObject(TextMeshProUGUI text, Vector3 dest)
    {
        Vector3 p = getScreenPosition(dest);
        text.rectTransform.anchoredPosition = p;

        if (p.z < 0) text.enabled = false;
        else if (!text.enabled) text.enabled = true;
    }

    public static Vector3 getScreenPosition(Vector3 pos) {
        Vector3 screenSize = new Vector3(Screen.width, Screen.height, 0);
        Vector3 screenPos = general.camera.WorldToScreenPoint(pos) - (screenSize / 2f);
        
        screenPos /= canvas.scaleFactor;

        return screenPos;
    }

    public static Vector2 getPixelPosition(Vector3 screenPos) {
        return new Vector2(
            Screen.width * ((screenPos.x + canvas.pixelRect.width) / (2f * canvas.pixelRect.width * canvas.scaleFactor)),
            Screen.height * ((screenPos.y + canvas.pixelRect.height) / (2f * canvas.pixelRect.height * canvas.scaleFactor)));
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

    public static float screenSize(MeshRenderer mr, Vector3 pos) {
        float diameter = mr.bounds.extents.magnitude;
        float distance = Vector3.Distance(pos, general.camera.transform.position);
        float angularSize = (diameter / distance) * Mathf.Rad2Deg;
        return ((angularSize * Screen.height) / general.camera.fieldOfView);
    }
}
