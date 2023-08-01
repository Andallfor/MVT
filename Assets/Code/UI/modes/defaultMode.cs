using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public sealed class defaultMode : IMode {
    protected override IModeParameters modePara => new defaultParameters();

    protected override bool enable() => true;
    protected override bool disable() => true;

    protected override void _initialize() {}

    public override void update() {
        
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
        
        playerControls.addKey("z", conTrig.down, () => {
            if (general.showingTrails) trailRenderer.disableAll();
            else trailRenderer.enableAll();

            general.showingTrails = !general.showingTrails;
            general.notifyTrailsChange();
        }, whitelist: new List<IMode>() {this, planetFocus.instance});
        */
    }

    private defaultMode() {}
    private static readonly Lazy<defaultMode> lazy = new Lazy<defaultMode>(() => new defaultMode());
    public static defaultMode instance => lazy.Value;
}
