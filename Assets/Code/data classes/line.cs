using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct line {
    public double m, b;
    public List<geographic> points;
    public line(double m, double b, List<geographic> points) {
        this.m = m;
        this.b = b;
        this.points = points;
    }

    public line(List<geographic> points) {
        this.m = (points[1].lat - points[0].lat) / (points[1].lon - points[0].lon);
        this.b = points[0].lat - this.m * points[0].lon;

        this.points = points;
    }

    public double xIntersection(double y) => (y - b) / m;
    public double yIntersection(double x) => x * m + b;
}
