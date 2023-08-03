using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct scenarioData {
    public double timeStart;
    public Dictionary<string, body> importantBodies;
}

public abstract class IScenario {
    // cant return from coroutine, so write output here instead
    public scenarioData metadata;
    protected abstract IEnumerator _generate();
    public virtual IEnumerator generate() {
        yield return controller.self.StartCoroutine(_generate());
    }
}