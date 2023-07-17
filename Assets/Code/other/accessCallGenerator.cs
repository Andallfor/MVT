using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.IO;

public class accessCallGeneratorWGS {
    private satellite target;
    private geographic pos;
    private planet earth;
    private position worldPositionNoHeight, worldPositionWithHeight;
    private Vector3 unityPositionWithHeight, unityPositionNoHeight;
    private bool initialized = false;
    private meshDistributor<universalTerrainMesh> meshDist, meshWGS;
    private universalTerrainJp2File meshFile;
    private double altitude;

    public accessCallGeneratorWGS(planet earth, geographic pos, satellite target) {
        this.target = target;
        this.pos = pos;
        this.earth = earth;
    }

    public void initialize(string path, uint res) {initialize(path, Vector2.zero, Vector2.one, res);}
    public void initialize(string path, Vector2 start, Vector2 end, uint res) {
        initialized = true;

        master.scale = 1;
        worldPositionNoHeight = pos.toCartesianWGS(0);
        unityPositionNoHeight = (Vector3) (worldPositionNoHeight.swapAxis() / master.scale);

        meshFile = new universalTerrainJp2File(path, false);

        double alt = meshFile.getHeight(pos);
        altitude = alt;
        //alt = 0.55;
        worldPositionWithHeight = pos.toCartesianWGS(alt + 0.01);
        unityPositionWithHeight = (Vector3) (worldPositionWithHeight.swapAxis() / master.scale);

        master.currentPosition = earth.representation.gameObject.transform.rotation * (Vector3) worldPositionWithHeight.swapAxis();

        // draw terrain
        meshDist = meshFile.load(start, end, 0, res, -worldPositionWithHeight);
        //meshDist = meshFile.load(start, end, 0, res, default(position));
        meshDist.drawAll(GameObject.FindGameObjectWithTag("fakeMeshParent").transform);
        //meshDist.drawAll(earth.representation.gameObject.transform);
        foreach (universalTerrainMesh mesh in meshDist.allMeshesOrdered) {
            mesh.addCollider();
            //mesh.go.transform.position = -(Vector3) ((master.currentPosition - earth.representation.gameObject.transform.rotation * (Vector3) worldPositionNoHeight.swapAxis()) / master.scale);
            mesh.go.transform.rotation = earth.representation.gameObject.transform.rotation;
            mesh.hide();
        }

        // draw wgs sphere
        int sy = 450;
        int sx = 900;
        meshWGS = new meshDistributor<universalTerrainMesh>(new Vector2Int(sx, sy), Vector2Int.zero, Vector2Int.zero);
        for (int r = 0; r < sy; r++) {
            for (int c = 0; c < sx; c++) {
                geographic g = new geographic(180.0 * (double) r / (double) sy - 90.0, 360.0 * (double) c / (double) sx - 180.0);
                meshWGS.addPoint(c, r, g.toCartesianWGS(0).swapAxis() / master.scale);
            }
        }
        meshWGS.drawAll(GameObject.FindGameObjectWithTag("fakeMeshParent").transform);
        //meshWGS.drawAll(earth.representation.gameObject.transform);
        foreach (universalTerrainMesh mesh in meshWGS.allMeshesOrdered) {
            mesh.addCollider();
            mesh.go.transform.position = -(Vector3) (master.currentPosition / master.scale);
            mesh.go.transform.rotation = earth.representation.gameObject.transform.rotation;
            //mesh.hide();
        }

        master.requestScaleUpdate();
        master.requestPositionUpdate();
        updateMeshes();
    }

    public List<accessCallTimeSpan> bruteForce(Time start, Time end, double inc) {
        if (!initialized) throw new MethodAccessException("Cannot run access calls unless .initialize(...) has been called!");

        double initialTime = master.time.julian;
        master.time.addJulianTime(start.julian - master.time.julian);
        updateMeshes();

        bool isBlocked = true;
        List<accessCallTimeSpan> spans = new List<accessCallTimeSpan>();

        int iterations = 0;
        while (master.time.julian < end.julian) {
            double currentIterationTime = master.time.julian;

            //enter code here
            

            if (Calc.topo(target.data.positions.find(master.time), pos, altitude, master.time.julian) - 5 > 0)
            {
                updateMeshes();

                bool hit = raycast();

                if (isBlocked != hit)
                {
                    isBlocked = hit;

                    if (hit)
                    {
                        accessCallTimeSpan span = spans[spans.Count - 1];
                        spans[spans.Count - 1] = new accessCallTimeSpan(span.start, master.time.julian - inc);
                    }
                    else spans.Add(new accessCallTimeSpan(master.time.julian, 0));
                }
            }

            master.time.addJulianTime(inc);

            iterations++;
        }

        // close any remaining windows
        if (!isBlocked) {
            accessCallTimeSpan span = spans[spans.Count - 1];
            spans[spans.Count - 1] = new accessCallTimeSpan(span.start, master.time.julian);
        }

        Debug.Log($"Total of {iterations} iterations for brute force access calls");

        // reset time
        //master.time.addJulianTime(initialTime - master.time.julian);
        //master.requestPositionUpdate();

        return spans;
    }

    public List<accessCallTimeSpan> findTimes(Time start, Time end, double maxInc, double minInc) {
        if (!initialized) throw new MethodAccessException("Cannot run access calls unless .initialize(...) has been called!");

        double initialTime = master.time.julian;
        master.time.addJulianTime(start.julian - master.time.julian);
        updateMeshes();

        bool isBlocked = true;
        List<accessCallTimeSpan> spans = new List<accessCallTimeSpan>();

        double currentInc = maxInc;
        while (master.time.julian < end.julian) {
            double currentIterationTime = master.time.julian;
            updateMeshes();

            bool hit = raycast();

            if (isBlocked != hit) {
                isBlocked = hit;

                if (hit) {
                    accessCallTimeSpan span = spans[spans.Count - 1];
                    spans[spans.Count - 1] = new accessCallTimeSpan(span.start, findBoundary(master.time.julian, maxInc, false, minInc));
                }
                else spans.Add(new accessCallTimeSpan(findBoundary(master.time.julian, maxInc, true, minInc) + minInc, 0));
            }

            master.time.addJulianTime((currentIterationTime + maxInc) - master.time.julian);
        }

        // close any remaining windows
        if (!isBlocked) {
            accessCallTimeSpan span = spans[spans.Count - 1];
            spans[spans.Count - 1] = new accessCallTimeSpan(span.start, master.time.julian);
        }

        // join together any spans that are closer than maxInc to each other
        for (int i = 0; i < spans.Count - 1; i++) {
            accessCallTimeSpan current = spans[i];
            accessCallTimeSpan next = spans[i + 1];
            if (next.start - current.end <= maxInc) {
                Debug.LogWarning("Warning: Two spans detected that are separated by less then maxInc from each other. Joining the two spans together.");
                spans.RemoveAt(i + 1);
                spans[i] = new accessCallTimeSpan(current.start, next.end);
            }
        }

        // reset time
        //master.time.addJulianTime(initialTime - master.time.julian);
        //master.requestPositionUpdate();

        return spans;
    }

    private double findBoundary(double time, double inc, bool targetStart, double minInc) {
        // termination condition- original, !original (separated by minInc)
        if (inc <= minInc) return time;

        bool originalHit = raycast(time, false);
        if (originalHit != targetStart) return findBoundary(time - inc / 2.0, inc / 2.0, targetStart, minInc);

        bool next = raycast(time + minInc, false);
        if (next != targetStart) return time;
        return findBoundary(time + inc / 2.0, inc / 2.0, targetStart, minInc);
    }

    public bool raycast(double time, bool reset = true) {
        double initialTime = master.time.julian;
        master.time.addJulianTime(time - master.time.julian);
        updateMeshes();

        bool result = raycast();

        if (reset) {
            master.time.addJulianTime(initialTime - master.time.julian);
            updateMeshes();
        }

        return result;
    }

    private bool raycast() {
        Vector3 dst = (Vector3) ((target.pos - master.referenceFrame - master.currentPosition) / master.scale);

        // (0,0,0) because we center (via master.currentPosition) on the correct starting position
        // raycast instead of linecast to prevent physics from checking too much (we really only need to check whats nearby)
        bool result = Physics.Raycast(Vector3.zero, dst, 100, (1 << 6) | (1 << 7)); // terrain and planets only
        Debug.DrawLine(Vector3.zero, dst, result ? Color.red : Color.green, 10000000);
        return result;
    }

    private void updateMeshes() {
        master.currentPosition = earth.representation.gameObject.transform.rotation * (Vector3) worldPositionWithHeight.swapAxis();

        Quaternion earthRot = earth.representation.gameObject.transform.rotation;
        //master.currentPosition = earthRot * (Vector3) worldPositionNoHeight; // TODO
        //master.currentPosition = ((quaternionDouble) earthRot).mult(worldPositionNoHeight.swapAxis());
        foreach (universalTerrainMesh m in meshDist.allMeshesOrdered) {
            //m.go.transform.position = -(Vector3) ((master.currentPosition - earth.representation.gameObject.transform.rotation * (Vector3) worldPositionNoHeight.swapAxis()) / master.scale);
            m.go.transform.rotation = earthRot;
        }
        foreach (universalTerrainMesh m in meshWGS.allMeshesOrdered) {
            m.go.transform.position = -(Vector3) (master.currentPosition / master.scale);
            m.go.transform.rotation = earthRot;
        }

        master.requestPositionUpdate();

        Physics.SyncTransforms();
    }

    public void saveResults(List<accessCallTimeSpan> spans) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < spans.Count; i++) {
            accessCallTimeSpan span = spans[i];
            sb.AppendLine($"{i},{span.start},{span.end},{span.end-span.start}");
        }

        string path = Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), "accessNew.txt");
        File.WriteAllText(path, sb.ToString());
    }
}

public struct accessCallTimeSpan {
    public double start, end;
    public accessCallTimeSpan(double start, double end) {
        this.start = start;
        this.end = end;
    }

    public override string ToString() => $"{start} to {end}";
}

internal struct quaternionDouble {
    public double x, y, z, w;
    public quaternionDouble(double x, double y, double z, double w) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public quaternionDouble(Quaternion q) {
        this.x = q.x;
        this.y = q.y;
        this.z = q.z;
        this.w = q.w;
    }

    public position mult(position p) {
        double _x = x * 2.0;
        double _y = y * 2.0;
        double _z = z * 2.0;
        double xx = _x * x;
        double yy = _y * y;
        double zz = _z * z;
        double xy = _x * y;
        double xz = _x * z;
        double yz = _y * z;
        double wx = w * x;
        double wy = w * y;
        double wz = w * z;

        double ox = (1.0 - (yy + zz)) * p.x + (xy - wz) * p.y + (xz + wy) * p.z;
        double oy = (xy + wz) * p.x + (1.0 - (xx + zz)) * p.y + (yz - wx) * p.z;
        double oz = (xz - wy) * p.x + (yz + wx) * p.y + (1.0 - (xx + yy)) * p.z;
        
        return new position(ox, oy, oz);
    }

    public static explicit operator quaternionDouble(Quaternion q) => new quaternionDouble(q.x, q.y, q.z, q.w);
}