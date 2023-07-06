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

                if (hit) spans[spans.Count - 1].setEnd(master.time.julian);
                else spans.Add(new accessCallTimeSpan(master.time.julian, 0));
            }

            master.time.addJulianTime(inc);

            iterations++;
        }

        // close any remaining windows
        if (!isBlocked) spans[spans.Count - 1].setEnd(master.time.julian);

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
            master.requestPositionUpdate();

            Vector3 src = earth.localGeoToUnityPos(pos, alt);
            Vector3 dst = (Vector3) ((target.pos - master.referenceFrame - master.currentPosition) / master.scale);

            bool hit = Physics.Linecast(src, dst, (1 << 6) | (1 << 7)); // terrain and planets only
            Debug.DrawLine(src, dst, Color.red, 10000000);

            if (isBlocked != hit) {
                isBlocked = hit;

                if (hit) spans[spans.Count - 1].setEnd(master.time.julian);
                else spans.Add(new accessCallTimeSpan(master.time.julian, 0));
            }

            master.time.addJulianTime(currentInc);
        }

        // close any remaining windows
        if (!isBlocked) spans[spans.Count - 1].setEnd(master.time.julian);

        // reset time
        master.time.addJulianTime(initialTime - master.time.julian);
        master.requestPositionUpdate();

        return spans;
    }

    private double findBoundary(double inc, bool original, double minInc) {
        // termination condition- original, !original (separated by minInc)

        //bool originalHit = 
        return 0;
    }

    private bool raycast(double time) {
        master.time.addJulianTime(time - master.time.julian);
        master.requestPositionUpdate();

        return false;
    }

    private bool raycast() {
        Vector3 src = earth.localGeoToUnityPos(pos, alt);
        Vector3 dst = (Vector3) ((target.pos - master.referenceFrame - master.currentPosition) / master.scale);

        return Physics.Linecast(src, dst, (1 << 6) | (1 << 7)); // terrain and planets only
    }
}

public struct accessCallTimeSpan {
    public double start, end;
    public accessCallTimeSpan(double start, double end) {
        this.start = start;
        this.end = end;
    }

    public void setEnd(double end) {this.end = end;}

    public void setStart(double start) {this.start = start;}

    public override string ToString() => $"{start} to {end}";
}
