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
            enable();
        }
        else {
            disable();
            modeParameters.load(new defaultParameters());
        }

        callback();

        this.active = active;
    }
    public virtual void toggle() {toggle(!active);}
    protected virtual void callback() {}

    private bool initialized = false;
    public void initialize() {
        if (initialized) {
            Debug.LogWarning(this.ToString() + " has already been initialized.");
            return;
        }

        initialized = true;

        _initialize();
    }

    protected virtual void _initialize() { // for if ur really lazy
        toggle(true, true);
        toggle(false, true);
    }

    protected abstract void enable();
    protected abstract void disable();
}

public static class modeController {
    private static List<IMode> modes = new List<IMode>();
    private static bool initialized = false;

    public static void initialize() {
        if (initialized) {
            Debug.LogWarning("Mode Controller has already been initialized.");
            return;
        }
        initialized = true;

        foreach (IMode mode in modes) mode.initialize();

        modeParameters.load(new defaultParameters());
    }

    public static void registerMode(IMode m) {
        modes.Add(m);
        if (initialized) m.initialize();
    }

    public static void enable(IMode target) {
        foreach (IMode m in modes) m.toggle(false);
        target.toggle(true);
    }

    public static void disableAll() {
        foreach (IMode m in modes) m.toggle(false);
        modeParameters.load(new defaultParameters());
    }

    public static void toggle(IMode target) {
        foreach (IMode m in modes) m.toggle(false);
        target.toggle();
    }
}