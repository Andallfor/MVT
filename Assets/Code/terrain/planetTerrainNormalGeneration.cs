using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class terrainNormalMap
{
    Dictionary<Vector2Int, terrainNormalPoint> normalPoints = new Dictionary<Vector2Int, terrainNormalPoint>();
    Texture2D normalMap;
    int w, h, step;
    public terrainNormalMap(int h, int w, int step)
    {
        this.w = w;
        this.h = h;
        this.step = step;
        normalMap = new Texture2D(w / step, h / step, TextureFormat.ARGB32, false);
    }

    public void addPoint(int x, int y, int height)
    {
        terrainNormalPoint p = new terrainNormalPoint(x, y, height);
        p.toLocal(x, y, step);

        normalPoints[p.pos] = p;

        // check neighboring points to see if they are now valid due to the addition of the new point
        for (int i = 0; i < 5; i++)
        {
            Vector2Int k = wrap(directions[i] + p.pos);
            if (normalPoints.ContainsKey(k))
            {
                terrainNormalPoint np = normalPoints[k];
                if (tryAddNeighbors(np)) addPointToMap(np);
            }
        }
    }

    private Vector2Int[] directions = new Vector2Int[5] {Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.zero};
    private bool tryAddNeighbors(terrainNormalPoint p, bool countOldSuccesses = false)
    {
        // sometimes we dont want to mark something that has already been successful as successful (namely when adding points to the normal map
        // since weve already added it- adding it again would just drain performance
        if (p.isValid() && !countOldSuccesses) return false;
        else if (p.isValid()) return true;

        // https://stackoverflow.com/questions/2431732/checking-if-a-bit-is-set-or-not
        // check if a certain position is set, if so then we check if a neighbor now exists in that position
        if ((p.successes & (1 << (4 + 1))) == 0) p.successes |= checkForNeighbor(wrap(p.pos + directions[3]), (byte) 0b10001); // w
        if ((p.successes & (1 << (2 + 1))) == 0) p.successes |= checkForNeighbor(wrap(p.pos + directions[2]), (byte) 0b10010); // s
        if ((p.successes & (1 << (3 + 1))) == 0) p.successes |= checkForNeighbor(wrap(p.pos + directions[1]), (byte) 0b10100); // e
        if ((p.successes & (1 << (1 + 1))) == 0) p.successes |= checkForNeighbor(wrap(p.pos + directions[0]), (byte) 0b11000); // n
        return p.isValid();
    }

    private byte checkForNeighbor(Vector2Int pos, byte ifTrue) => normalPoints.ContainsKey(pos) ? ifTrue : (byte) 0b00000;
    private Vector2Int wrap(Vector2Int v)
    {
        if (v.x == -1) v.x = w - 1;
        else if (v.x == w) v.x = 0;
        if (v.y == -1) v.y = h - 1;
        else if (v.y == h) v.y = 0;

        return v;
    }

    public void saveTexture(string path)
    {
        normalMap.Apply(false, false);
        File.WriteAllBytes(path, normalMap.EncodeToJPG());
    }

    private static Vector2Int vbl = new Vector2Int(-1, -1);
    private static Vector2Int vtl = new Vector2Int(-1, 1);
    private static Vector2Int vtr = new Vector2Int(1, 1);
    private static Vector2Int vbr = new Vector2Int(1, -1);
    private void addPointToMap(terrainNormalPoint p)
    {
        // https://forum.unity.com/threads/normal-map-from-height-map-on-runtime.259273/
        // when this is called, we are guaranteed that all neighbors exist
        float tl = intensity(normalPoints[wrap(p.pos + vbl             )].height);
        float t  = intensity(normalPoints[wrap(p.pos + Vector2Int.left )].height);
        float tr = intensity(normalPoints[wrap(p.pos + vtl             )].height);
        float r  = intensity(normalPoints[wrap(p.pos + Vector2Int.up   )].height);
        float br = intensity(normalPoints[wrap(p.pos + vtr             )].height);
        float b  = intensity(normalPoints[wrap(p.pos + Vector2Int.right)].height);
        float bl = intensity(normalPoints[wrap(p.pos + vbr             )].height);
        float l  = intensity(normalPoints[wrap(p.pos + Vector2Int.down )].height);

        //Sobel filter
        float dX = (tr + 2.0f * r + br) - (tl + 2.0f * l + bl);
        float dY = (bl + 2.0f * b + br) - (tl + 2.0f * t + tr);
        float dZ = 1.0f;
        
        Vector3 vc = new Vector3(2 * dX, 2 * dY, dZ);
        vc.Normalize();
        
        normalMap.SetPixel(p.x, p.y, new Color(0.5f + 0.5f * vc.x, 0.5f + 0.5f * vc.y, 0.5f + 0.5f * vc.z, 0.0f));
    }

    private static float intensity(int height) => (10902f + (float) height) / (8848f + 10902f); // scale height to 0-1
}

public class terrainNormalPoint // try to switch to struct eventually- currently we rely on linking
{
    public int x, y, height;
    public Vector2Int pos;
    public byte successes = 0b10000; // _ n e s w, 0 represents no neighbor 1 represents neighbor (inital 1 is just to prevent leading 0 from being cut)

    public terrainNormalPoint(int x, int y, int height)
    {
        this.x = x;
        this.y = y;
        this.pos = new Vector2Int(x, y);
        this.height = height;
    }

    public void toLocal(int wx, int wy, int step)
    {
        this.x = wx / step;
        this.y = wy / step;
        this.pos = new Vector2Int(x, y);
    }

    public bool isValid() => successes == 0b11111;

    // szudzik is love, szudzik is life
    public override int GetHashCode() => x >= y ? x * x + x + y : x + y * y;
}
