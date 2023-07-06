using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class accessCallGeneratorWGS<T> where T : IMesh, new() {
    private satellite target;
    private geographic pos;
    private double alt;
    private planet earth;
    public accessCallGeneratorWGS(planet earth, geographic pos, double alt, satellite target) {
        this.target = target;
        this.pos = pos;
        this.alt = alt;
        this.earth = earth;
    }

    public List<accessCallTimeSpan> bruteForce(Time start, Time end, double inc, meshDistributor<T> terrain) {
        foreach (IMesh child in terrain.allMeshes) child.addCollider();

        double initialTime = master.time.julian;
        master.time.addJulianTime(start.julian - master.time.julian);
        master.requestPositionUpdate();

        bool isBlocked = true;
        List<accessCallTimeSpan> spans = new List<accessCallTimeSpan>();

        int iterations = 0;

        while (master.time.julian < end.julian) {
            master.requestPositionUpdate();

            Vector3 src = earth.localGeoToUnityPos(pos, alt);
            Vector3 dst = (Vector3) ((target.pos - master.referenceFrame - master.currentPosition) / master.scale);

            bool hit = Physics.Linecast(src, dst, (1 << 6) | (1 << 7)); // terrain and planets only
            Debug.DrawLine(src, dst, Color.red, 10000000);

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

    public List<accessCallTimeSpan> findTimes(Time start, Time end, double maxInc, double minInc, meshDistributor<T> terrain) {
        foreach (IMesh child in terrain.allMeshes) child.addCollider();

        double initialTime = master.time.julian;
        master.time.addJulianTime(start.julian - master.time.julian);
        master.requestPositionUpdate();

        bool isBlocked = true;
        List<accessCallTimeSpan> spans = new List<accessCallTimeSpan>();

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

            master.time.addJulianTime((currentIterationTime + maxInc) - master.time.julian);
        }

        // close any remaining windows
        if (!isBlocked) {
            accessCallTimeSpan span = spans[spans.Count - 1];
            spans[spans.Count - 1] = new accessCallTimeSpan(span.start, master.time.julian);
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
        master.requestPositionUpdate();

        bool result = raycast();

        if (reset) {
            master.time.addJulianTime(initialTime - master.time.julian);
            master.requestPositionUpdate();
        }

        return result;
    }

    private bool raycast() {
        Vector3 src = earth.localGeoToUnityPos(pos, alt);
        Vector3 dst = (Vector3) ((target.pos - master.referenceFrame - master.currentPosition) / master.scale);

        bool result = Physics.Linecast(src, dst, (1 << 6) | (1 << 7)); // terrain and planets only
        //Debug.DrawLine(src, dst, result ? Color.red : Color.green, 10000000);
        return result;
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
