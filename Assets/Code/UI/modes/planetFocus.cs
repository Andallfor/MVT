using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public sealed class planetFocus : IMode {
    public Vector3 rotation;
    public float zoom = general.defaultCameraFOV;
    public bool usePoleFocus {get; private set;} = false;

    public planet focus {get; private set;}
    public position movementOffset;
    public bool lunarTerrainFilesExist {get; private set;}

    protected override IModeParameters modePara => new planetFocusParameters();

    protected override bool enable() {
        reset();
        if (master.requestReferenceFrame() is planet) {
            // we want planet to take up about 60%
            focus = (planet) master.requestReferenceFrame();
            master.scale = focus.radius / (0.6 * -general.defaultCameraPosition.z);
            master.requestPositionUpdate();
            general.camera.transform.LookAt(focus.representation.gameObject.transform.position);
        } else return false;
        return true;
    }

    protected override bool disable() {
        if (lunarTerrainFilesExist) {
            if (general.pt != null) general.pt.unload();
            if (general.plt != null) general.plt.clear();
        }

        return true;
    }

    protected override void callback() {
        usePoleFocus = false;
    }

    public void togglePoleFocus(bool use) {
        if (!lunarTerrainFilesExist) return;

        if (focus.name != "Luna") {
            usePoleFocus = false;
            return;
        }
        usePoleFocus = use;
        reset();

        if (use) {
            master.scale = 50;
            master.currentPosition = focus.rotateLocalGeo(new geographic(-90, 0), 0);

            focus.representation.forceHide = true;
        } else {
            master.scale = focus.radius / (0.6 * -general.defaultCameraPosition.z);
            focus.representation.forceHide = false;
        }
    }

    protected override void _initialize() {
        string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MVT/terrain");
        if (Directory.Exists(p)) lunarTerrainFilesExist = true;

        base._initialize();
    }

    public void reset() {
        modeParameters.load(new planetFocusParameters());
        zoom = general.defaultCameraFOV;
        rotation = Vector3.zero;
        movementOffset = new position(0, 0, 0);
    }

    public void update() {
        if (usePoleFocus) master.currentPosition = focus.rotateLocalGeo(new geographic(-90, 0), 0) + movementOffset;
        else master.currentPosition = movementOffset;

        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, rotation.x);
        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.up, rotation.y);

        general.camera.transform.rotation *= Quaternion.AngleAxis(rotation.z, Vector3.forward);

        rotation *= Mathf.Max(1f - (UnityEngine.Time.deltaTime * 5f), 0);
        if (rotation.magnitude < 0.001f) rotation = Vector3.zero;

        general.camera.fieldOfView = zoom;
    }

    protected override void loadControls() {
        playerControls.addKey("e", conTrig.down, () => {
            modeController.toggle(planetFocus.instance);
            if (lunarTerrainFilesExist) {
                general.pt.unload();
                general.plt.clear();
            }
        });

        List<IMode> w = new List<IMode>() {this};

        playerControls.addKey("", conTrig.none, () => {
            Vector3 difference = Input.mousePosition - playerControls.lastMousePos;

            Vector2 adjustedDifference = new Vector2(-difference.y / Screen.height, difference.x / Screen.width);
            adjustedDifference *= 100f;

            planetFocus.instance.rotation.x = adjustedDifference.x * planetFocus.instance.zoom / 125f;
            planetFocus.instance.rotation.y = adjustedDifference.y * planetFocus.instance.zoom / 125f;
            planetFocus.instance.rotation.z = 0;
        }, precondition: () => Input.GetMouseButton(0), whitelist: w);

        playerControls.addKey("", conTrig.none, () => {
            Vector3 difference = Input.mousePosition - playerControls.lastMousePos;

            float adjustedDifference = (difference.x / Screen.width) * 100;
            planetFocus.instance.rotation.x = 0;
            planetFocus.instance.rotation.y = 0;
            planetFocus.instance.rotation.z = adjustedDifference;
        }, precondition: () => Input.GetMouseButton(1), whitelist: w);

        playerControls.addKey("", conTrig.none, () => {
            // hi!
            // i know you probably have questions about y tf the code below here exists
            // well too bad
            // if u want to fix it go ahead, otherwise its staying here
            if (planetFocus.instance.usePoleFocus && lunarTerrainFilesExist) {
                float change = (float) (0.1 * master.scale) * Mathf.Sign(Input.mouseScrollDelta.y);
                master.scale -= change;
                planetFocus.instance.update();
                master.requestPositionUpdate();
            } else {
                planetFocus.instance.zoom -= Input.mouseScrollDelta.y * planetFocus.instance.zoom / 10f;
                planetFocus.instance.zoom = Mathf.Max(Mathf.Min(planetFocus.instance.zoom, 90), 0.1f);
            }
        }, precondition: () => Input.mouseScrollDelta.y != 0, whitelist: w);

        playerControls.addKey("t", conTrig.down, () => {
            if (!lunarTerrainFilesExist) return;
            planetFocus.instance.togglePoleFocus(!planetFocus.instance.usePoleFocus);
            if (planetFocus.instance.usePoleFocus) general.plt.genMinScale();
            else general.plt.clear();
        }, precondition: () => !general.plt.currentlyDrawing, whitelist: w);

        playerControls.addKey("-", conTrig.down, () => {
            if (!lunarTerrainFilesExist) return;
            general.plt.decreaseScale();
        }, precondition: () => planetFocus.instance.usePoleFocus, whitelist: w);

        playerControls.addKey("=", conTrig.down, () => {
            if (!lunarTerrainFilesExist) return;
            general.plt.increaseScale();
        }, precondition: () => planetFocus.instance.usePoleFocus, whitelist: w);

        playerControls.addKey("", conTrig.none, () => update(), whitelist: w);

        //playerControls
    }

    private planetFocus() {}
    private static readonly Lazy<planetFocus> lazy = new Lazy<planetFocus>(() => new planetFocus());
    public static planetFocus instance => lazy.Value;
}
