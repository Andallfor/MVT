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
        meshFile.overrideToCart(geographic.toCartesianWGS);

        double alt = meshFile.getHeight(pos);
        worldPositionWithHeight = pos.toCartesianWGS(alt + 0.01);
        unityPositionWithHeight = (Vector3) (worldPositionWithHeight.swapAxis() / master.scale);

        //master.currentPosition = earth.representation.gameObject.transform.rotation * (Vector3) worldPositionNoHeight;
        master.currentPosition = new position(500, 500, 500);

        // draw terrain
        //meshDist = meshFile.load(start, end, 0, res, -1 * unityPositionNoHeight);
        meshDist = meshFile.load(start, end, 0, res, default(position));
        //meshDist.drawAll(GameObject.FindGameObjectWithTag("fakeMeshParent").transform);
        meshDist.drawAll(earth.representation.gameObject.transform);
        foreach (universalTerrainMesh mesh in meshDist.allMeshes) {
            mesh.addCollider();
            //mesh.go.transform.rotation = earth.representation.gameObject.transform.rotation;
            //mesh.hide();
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
        //meshWGS.drawAll(GameObject.FindGameObjectWithTag("fakeMeshParent").transform);
        meshWGS.drawAll(earth.representation.gameObject.transform);
        foreach (universalTerrainMesh mesh in meshWGS.allMeshes) {
            mesh.addCollider();
            //mesh.go.transform.position = -(Vector3) (master.currentPosition / master.scale);
            //mesh.go.transform.rotation = earth.representation.gameObject.transform.rotation;
            //mesh.hide();
        }

        master.requestScaleUpdate();
        master.requestPositionUpdate();
    }

    public List<accessCallTimeSpan> bruteForce(Time start, Time end, double inc) {
        double initialTime = master.time.julian;
        master.time.addJulianTime(start.julian - master.time.julian);
        master.requestPositionUpdate();

        bool isBlocked = true;
        List<accessCallTimeSpan> spans = new List<accessCallTimeSpan>();

        int iterations = 0;

        while (master.time.julian < end.julian) {
            master.requestPositionUpdate();

            Vector3 src = earth.representation.gameObject.transform.rotation * unityPositionWithHeight;
            Vector3 dst = (Vector3) ((target.pos - master.referenceFrame - master.currentPosition) / master.scale);

            bool hit = Physics.Linecast(src, dst, (1 << 6) | (1 << 7)); // terrain and planets only
            Debug.DrawLine(src, dst, hit ? Color.red : Color.green, 10000000);

            if (isBlocked != hit) {
                isBlocked = hit;

                if (hit) {
                    accessCallTimeSpan span = spans[spans.Count - 1];
                    spans[spans.Count - 1] = new accessCallTimeSpan(span.start, master.time.julian - inc);
                }
                else spans.Add(new accessCallTimeSpan(master.time.julian, 0));
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
        //master.requestPositionUpdate();
        updateMeshes();

        bool isBlocked = true;
        List<accessCallTimeSpan> spans = new List<accessCallTimeSpan>();

        int count = 0;

        double currentInc = maxInc;
        while (master.time.julian < end.julian) {
            double currentIterationTime = master.time.julian;
            master.requestPositionUpdate();

            bool hit = raycast();

            if (isBlocked != hit) {
                isBlocked = hit;

                if (hit) {
                    accessCallTimeSpan span = spans[spans.Count - 1];
                    spans[spans.Count - 1] = new accessCallTimeSpan(span.start, findBoundary(master.time.julian, maxInc, false, minInc));
                }
                else spans.Add(new accessCallTimeSpan(findBoundary(master.time.julian, maxInc, true, minInc) + minInc, 0));
            }

            //if (master.time.julian > 2461026.93171353) return null;

            //if (count > 200) return null;
            //return null;

            master.time.addJulianTime((currentIterationTime + maxInc) - master.time.julian);
            updateMeshes();

            count++;
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
        //master.requestPositionUpdate();

        bool result = raycast();

        if (reset) {
            master.time.addJulianTime(initialTime - master.time.julian);
            master.requestPositionUpdate();
        }

        return result;
    }

    private bool raycast() {
        Quaternion q = earth.representation.gameObject.transform.rotation;
        //Vector3 src = q * (unityPositionWithHeight - unityPositionNoHeight);
        Vector3 src = q * unityPositionWithHeight;
        Vector3 dst = (Vector3) ((target.pos - master.referenceFrame - master.currentPosition) / master.scale);

        Vector3 offset = (Vector3) (master.currentPosition / master.scale);
        src -= offset;

        RaycastHit ray;
        bool result = Physics.Linecast(src, dst, out ray, (1 << 6) | (1 << 7)); // terrain and planets only
        Debug.DrawLine(src, dst, result ? Color.red : Color.green, 10000000);
        //if (result) {
        //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    go.transform.position = ray.point;
        //    Debug.Log(ray.collider.gameObject.name);
        //    ray.collider.gameObject.SetActive(false);
        //}
        return result;
    }

    private void updateMeshes() {
        //Quaternion earthRot = earth.representation.gameObject.transform.rotation;
        //master.currentPosition = earthRot * (Vector3) worldPositionNoHeight; // TODO
        //foreach (universalTerrainMesh m in meshDist.allMeshes) m.go.transform.rotation = earthRot;
        //foreach (universalTerrainMesh m in meshWGS.allMeshes) {
        //    m.go.transform.position = -(Vector3) (master.currentPosition / master.scale);
        //    m.go.transform.rotation = earthRot;
        //}

        master.requestPositionUpdate();
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
