using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class IMode {
    public bool active {get; protected set;}
    protected abstract IModeParameters modePara {get;}
    public virtual void toggle(bool active, bool force = false) {
        if (!force && active == this.active) return;

        if (active) {
            modeParameters.load(modePara);
            if (!enable()) {
                modeParameters.load(new defaultParameters());
                return;
            }
        }
        else {
            if (!disable()) return;
            modeParameters.load(new defaultParameters());
        }

        callback();

        this.active = active;
    }
    public virtual void toggle() {toggle(!active);}
    public virtual void update() {}
    protected virtual void callback() {}

    public bool initialized {get; private set;} = false;
    public void initialize() {
        if (initialized) {
            Debug.LogWarning(this.ToString() + " has already been initialized.");
            return;
        }

        initialized = true;

        _initialize();

        loadControls();
    }

    protected virtual void _initialize() { // for if ur really lazy
        toggle(true, true);
        toggle(false, true);
    }

    protected abstract void loadControls();
    protected abstract bool enable();
    protected abstract bool disable();
}

public static class modeController {
    private static List<IMode> modes = new List<IMode>();
    private static bool initialized = false;
    private static IMode activeMode;

    public static void initialize() {
        if (initialized) {
            Debug.LogWarning("Mode Controller has already been initialized.");
            return;
        }
        initialized = true;

        modes.Add(defaultMode.instance);
        foreach (IMode mode in modes) {
            if (!mode.initialized) mode.initialize();
        }

        defaultMode.instance.toggle(true);
        activeMode = defaultMode.instance;
    }

    public static void registerMode(IMode m) {
        modes.Add(m);
        if (!m.initialized) m.initialize();
    }

    public static void enable(IMode target) {
        foreach (IMode m in modes) m.toggle(false);
        target.toggle(true);

        activeMode = target;
    }

    public static void disableAll() {
        foreach (IMode m in modes) m.toggle(false);
        defaultMode.instance.toggle(true);
    }

    public static void toggle(IMode target) {
        master.requestScaleUpdate();
        
        bool state = target.active;
        foreach (IMode m in modes) m.toggle(false);
        target.toggle(!state);

        master.clearAllLines();
        general.notifyStatusChange();

        if (state == true) {
            defaultMode.instance.toggle(true);
            activeMode = defaultMode.instance;
        } else activeMode = target;
    }

    public static void update() {
        if (activeMode != default(IMode)) activeMode.update();
    }
}