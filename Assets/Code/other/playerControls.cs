using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class playerControls {
    private static Dictionary<string, playerControlKey> keys = new Dictionary<string, playerControlKey>();
    public static Vector3 lastMousePos;

    public static void addKey(string key, conTrig trigger, Action callback, Func<bool> precondition = null, List<IMode> whitelist = null, List<IMode> blacklist = null) {
        // can use "" as the input to trigger always
        if (key == "") key = "all";
        // we can have the same key bound to multiple actions
        keys[key + keys.Count.ToString()] = new playerControlKey(key, trigger, callback, precondition, whitelist, blacklist);
    }

    public static void update() {
        foreach (playerControlKey k in keys.Values) k.update();
    }
}

internal class playerControlKey {
    public string key {get; private set;}
    private Action callback;
    private List<IMode> whitelist, blacklist;
    private Func<bool> precondition;
    private conTrig trigger;
    private bool triggerAll;

    public playerControlKey(string key, conTrig trigger, Action callback, Func<bool> precondition = null, List<IMode> whitelist = null, List<IMode> blacklist = null) {
        this.key = key;
        this.trigger = trigger;
        this.callback = callback;
        this.precondition = (precondition == null) ? () => true : precondition;
        this.whitelist = (whitelist == null) ? new List<IMode>() : whitelist;
        this.blacklist = (blacklist == null) ? new List<IMode>() : blacklist;

        if (key.StartsWith("all")) triggerAll = true;
    }

    public void update() {
        if (!triggerAll) {
            if (trigger == conTrig.down && !Input.GetKeyDown(key)) return;
            if (trigger == conTrig.up && !Input.GetKeyUp(key)) return;
            if (trigger == conTrig.held && !Input.GetKey(key)) return;
        }

        if (!precondition()) return;
        if (whitelist.Count != 0) {
            bool whitelistPassed = false;
            foreach (IMode m in whitelist) {
                if (m.active) {
                    whitelistPassed = true;
                    break;
                }
            }

            if (!whitelistPassed) return;
        }

        if (blacklist.Count != 0) {
            foreach (IMode m in blacklist) {
                if (m.active) return;
            }
        }

        callback(); 
    }
}

public enum conTrig {
    down, up, held, none
}