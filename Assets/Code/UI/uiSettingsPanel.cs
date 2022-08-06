using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class uiSettingsPanel : MonoBehaviour
{
    [Header("Top")]
    [Tooltip("Day, Month, Year, Hour, Min, Sec")]
    public uiInput[] date = new uiInput[6]; // day, month, year, hour, min, sec
    public uiInput speed, scale, intensity;

    private Light sceneLighting;
    private Shader unlit, lit;
    private Material moonMat;
    private static double simulationStart, scaleStart;
    public void Awake() {
        simulationStart = master.time.julian;
        scaleStart = master.scale;
        sceneLighting = GameObject.FindGameObjectWithTag("light").GetComponent<Light>();

        unlit = Resources.Load<Shader>("Materials/planets/moon/moonTexture");
        lit = Resources.Load<Shader>("Materials/planets/moon/moonShaded");
        moonMat = Resources.Load<Material>("Materials/planets/moon/moon");

        moonMat.shader = lit;
    }

    public static void reset() {
        master.time.addJulianTime(simulationStart - master.time.julian);
        master.scale = scaleStart;
        master.currentPosition = new position(0, 0, 0);
        master.setReferenceFrame(controller.defaultReferenceFrame);
        general.camera.fieldOfView = general.defaultCameraFOV;
        general.camera.transform.position = general.defaultCameraPosition;
        general.camera.transform.localEulerAngles = new Vector3(0, 0, 0);

        foreach (planet p in master.allPlanets) p.tr.disable();
        foreach (satellite s in master.allSatellites) s.tr.disable();

        if (planetOverview.usePlanetOverview) planetOverview.enable(false);
        if (planetFocus.usePlanetFocus) planetFocus.enable(false);
        if (uiMap.useUiMap) uiMap.map.toggle(false);

        general.pt.unload();
        general.plt.clear();

        master.requestScaleUpdate();
        master.clearAllLines();
        general.notifyStatusChange();
    }

    public void access() {
        Debug.Log("Ask how to do this");
    }

    public void windows() {
        Debug.Log("Ask how to do this");
    }

    public void setTime () {
        DateTime dt = new DateTime(
            (int) date[2].currentValue, 
            (int) date[1].currentValue,
            (int) date[0].currentValue,
            (int) date[3].currentValue,
            (int) date[4].currentValue,
            (int) date[5].currentValue);
        
        Time t = new Time(dt);
        master.time.addJulianTime(t.julian - master.time.julian);
    }

    public void setSpeed() {controller.speed = (speed.currentValue / 1440f) / ((float) controller.tickrate / 60f);}

    public void setScale() {master.scale = scale.currentValue;}

    public void setLighting() {
        if (sceneLighting.intensity == 0 && intensity.currentValue != 0) {
            moonMat.shader = lit;
        } else if (sceneLighting.intensity != 0 && intensity.currentValue == 0) {
            moonMat.shader = unlit;
        }

        sceneLighting.intensity = ((float) intensity.currentValue) / 5f;
    }
}
