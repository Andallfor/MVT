using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class terrainUtility
{
    private position lastPlayerPos;
    private Vector3 lastCamPos, lastFRot;
    private float lastFov;
    private int lastTick = 0, lastTickRunTimes = 0;
    private planet parent;

    public terrainUtility(planet parent) {
        this.parent = parent;
    }

    public bool canRun() {
        bool change = false;

        if (planetFocus.instance.active) {
            bool fov = lastFov != general.camera.fieldOfView;
            bool cmove = lastCamPos != general.camera.transform.position;
            bool fRot = lastFRot != planetFocus.instance.rotation;
            bool move = master.currentPosition != lastPlayerPos;

            if (fov || cmove || fRot || move) change = true;
        } else if (!planetOverview.instance.active) {
            bool move = master.currentPosition != lastPlayerPos;

            if (move) change = true;
        }

        if (change) {
            lastTickRunTimes = 0;
            lastTick = master.currentTick;
        }

        if (master.currentTick - lastTick > 50 && !change && lastTickRunTimes < 10) {
            change = true;
            lastTick = master.currentTick;
            lastTickRunTimes++;
        }

        lastPlayerPos = master.currentPosition;
        lastFRot = planetFocus.instance.rotation;
        lastCamPos = general.camera.transform.position;
        lastFov = general.camera.fieldOfView;

        return change;
    }
}