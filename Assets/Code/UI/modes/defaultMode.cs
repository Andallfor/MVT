using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public sealed class defaultMode : IMode {
    protected override IModeParameters modePara => new defaultParameters();

    protected override bool enable() => true;
    protected override bool disable() => true;

    protected override void _initialize() {
        rotationInterp = new v3Interp(1);
    }

    private Vector3 rotation;
    private v3Interp rotationInterp;

    private double scaleChange;
    private double scaleChangeSpeed = 10;
    private bool introAnimationDone = true;
    private Coroutine introAnimationCoroutine;

    public void runIntroAnimation() {
        introAnimationDone = false;
        trailRenderer.enableAll();
        general.showingTrails = true;
        general.notifyTrailsChange();
        master.scale = 5_000_000;
        scaleChange = 1500 - master.scale;
        scaleChangeSpeed = 0.01;

        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, 30);

        introAnimationCoroutine = controller.self.StartCoroutine(introAnimation());
    }

    private IEnumerator introAnimation() {
        int length = 60 * 5;
        double minSpeed = scaleChangeSpeed;
        double maxSpeed = 10;

        for (int i = 0; i < length; i++) {
            double percent = (double) i / (double) length;
            double smoothPercent = percent < 0.5 ? 4 * percent * percent * percent : 1 - Math.Pow(-2 * percent + 2, 3) / 2.0;
            scaleChangeSpeed = (maxSpeed - minSpeed) * smoothPercent + minSpeed;

            yield return new WaitForFixedUpdate();
        }
    }

    public override void update() {
        // controls
        // scroll- change zoom level- this should scale based on how zoomed in/out we are
        // left click + drag- rotate camera about reference frame
        // right click + drag- rotate camera
        // middle click + drag- pan camera- this should scale as well
        // wasd doesnt do anything

        if (Input.anyKeyDown && !introAnimationDone) {
            introAnimationDone = true;
            scaleChange = 0;
            scaleChangeSpeed = 10;

            if (introAnimationCoroutine != null) controller.self.StopCoroutine(introAnimationCoroutine);
        }

        if (Input.GetMouseButton(1) && !Input.GetMouseButtonUp(1) && !EventSystem.current.IsPointerOverGameObject()) {
            Vector3 difference = Input.mousePosition - playerControls.lastMousePos;

            float adjustedDifference = (difference.x / Screen.width) * 75f;
            rotation.z = adjustedDifference;

            rotationInterp.stop();
        }

        if (Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject()) {
            // we want the drag to match how much it looks like it should drag on screen
            Vector3 difference = Input.mousePosition - playerControls.lastMousePos;

            Vector2 adjustedDifference = new Vector2(-difference.y / Screen.height, difference.x / Screen.width);
            adjustedDifference *= 75f;

            rotation.x = adjustedDifference.x;
            rotation.y = adjustedDifference.y;

            rotationInterp.stop();
        }

        if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) && !EventSystem.current.IsPointerOverGameObject()) {
            rotationInterp.stop();
            rotationInterp.mark(rotation, Vector3.zero);
        }

        if (Input.mouseScrollDelta.y != 0 && !EventSystem.current.IsPointerOverGameObject()) {
            scaleChange += -(0.1 * master.scale) * Mathf.Sign(Input.mouseScrollDelta.y);

            // dont let the user scroll into the reference frame
            // can cache this value for performance
            float d = general.camera.transform.position.magnitude;
            body b = master.requestReferenceFrame();
            double radius = (b is planet) ? ((planet) b).radius : 1;

            double minScale = (radius * 1.01) / d;
            if (master.scale + scaleChange <= minScale) {
                scaleChange = minScale - master.scale;
            }
        }

        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, rotation.x);
        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.up, rotation.y);

        general.camera.transform.rotation *= Quaternion.AngleAxis(rotation.z, Vector3.forward);

        // use interp because mouse movement is continuous stop and start; user is not spamming clicking
        if (rotationInterp.isRunning) rotation = rotationInterp.get();
        
        // use buffer system for scale because there are constant starts and stops (since scrolling is not smooth)
        if (scaleChange != 0) {
            double c = scaleChange * UnityEngine.Time.deltaTime * scaleChangeSpeed;
            if (Math.Abs(c) < 0.1) scaleChange = 0;
            else scaleChange -= c;
            master.scale += c;
        }

        trailRenderer.update();
    }

    protected override void loadControls() {
        List<IMode> w = new List<IMode>() {this};

        /*
        playerControls.addKey("w", conTrig.held,
            () => master.currentPosition += general.camera.transform.forward * 5f * (float) master.scale * UnityEngine.Time.deltaTime,
            whitelist: w);
        playerControls.addKey("s", conTrig.held,
            () => master.currentPosition -= general.camera.transform.forward * 5f * (float) master.scale * UnityEngine.Time.deltaTime,
            whitelist: w);
        playerControls.addKey("d", conTrig.held,
            () => master.currentPosition += general.camera.transform.right * 5f * (float) master.scale * UnityEngine.Time.deltaTime,
            whitelist: w);
        playerControls.addKey("a", conTrig.held,
            () => master.currentPosition -= general.camera.transform.right * 5f * (float) master.scale * UnityEngine.Time.deltaTime,
            whitelist: w);
        
        playerControls.addKey("", conTrig.none, () => {
            Transform c = general.camera.transform;
            c.Rotate(0, Input.GetAxis("Mouse X") * 2, 0);
            c.Rotate(-Input.GetAxis("Mouse Y") * 2, 0, 0);
            c.localEulerAngles = new Vector3(c.localEulerAngles.x, c.localEulerAngles.y, 0);
        }, precondition: () => Input.GetMouseButton(1) && !EventSystem.current.IsPointerOverGameObject(),
           whitelist: w);
        */
        playerControls.addKey("z", conTrig.down, () => {
            if (general.showingTrails) trailRenderer.disableAll();
            else trailRenderer.enableAll();

            general.showingTrails = !general.showingTrails;
            general.notifyTrailsChange();
        }, whitelist: new List<IMode>() {this, planetFocus.instance});
    }

    private defaultMode() {}
    private static readonly Lazy<defaultMode> lazy = new Lazy<defaultMode>(() => new defaultMode());
    public static defaultMode instance => lazy.Value;
}

internal struct v3Interp {
    private Vector3 reference, target;
    private readonly float length;
    private float startTime;
    public bool isRunning {get; private set;}

    public v3Interp(float length) {
        this.length = length;

        reference = Vector3.zero;
        startTime = 0;
        target = Vector3.zero;
        isRunning = false;
    }

    public void mark(Vector3 reference, Vector3 target) {
        this.reference = reference;
        this.target = target;
        isRunning = true;
    }

    public void stop() {
        isRunning = false;
        startTime = 0;
    }

    public Vector3 get() {
        if (!isRunning) return target;
        if (startTime == 0) startTime = UnityEngine.Time.time;

        float percent = (UnityEngine.Time.time - startTime) / length;
        if (percent >= 1) {
            stop();
            return target;
        }

        Vector3 d = target - reference;
        d *= (1f - Mathf.Pow(1f - percent, 4));

        return reference + d;
    }
}