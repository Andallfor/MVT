using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class universalTerrainMesh : IMesh {
    public universalTerrainMesh() {}

    public universalTerrainMesh(int width, int height, position ll, position max, bool reverse, Func<Vector2Int, Vector2> customUV = null) {
        base.init(width, height, ll, max, reverse, customUV: customUV);
    }

    public override Vector3 addPoint(int x, int y, geographic g, double h) {
        Vector3 v = (Vector3) (g.toCartesian(h).swapAxis() / master.scale);
        this.verts[toIndex(x, y)] = v;
        return v;
    }
}