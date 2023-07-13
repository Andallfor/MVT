using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// See https://aka.ms/new-console-template for more information

public class Kepler
{
    public double semiMajorAxis, eccentricity, inclination, argOfPerigee, longOfAscNode, mu, startingEpoch, v0;

    private const double degToRad = Math.PI / 180.0;
    private const double radToDeg = 180.0 / Math.PI;

    public position find(double t)
    {
        double meanAnom = Calc.meanAnomSolve(v0, eccentricity, mu, semiMajorAxis, t, startingEpoch);

        double EA = Calc.kepSolve(meanAnom, eccentricity);

        double trueAnom1 = (Math.Sqrt(1 - eccentricity * eccentricity) * Math.Sin(EA)) / (1 - eccentricity * Math.Cos(EA));
        double trueAnom2 = (Math.Cos(EA) - eccentricity) / (1 - eccentricity * Math.Cos(EA));

        double trueAnom = Math.Atan2(trueAnom1, trueAnom2);

        double theta = trueAnom + argOfPerigee;

        double radius = (semiMajorAxis * (1 - eccentricity * eccentricity)) / (1 + eccentricity * Math.Cos(trueAnom));

        double xp = radius * Math.Cos(theta);
        double yp = radius * Math.Sin(theta);

        position pos = new position(
        xp * Math.Cos(longOfAscNode) - yp * Math.Cos(inclination) * Math.Sin(longOfAscNode),
        xp * Math.Sin(longOfAscNode) + yp * Math.Cos(inclination) * Math.Cos(longOfAscNode),
        yp * Math.Sin(inclination));

        return pos;
    }

    /// <summary> give in degrees </summary>
    public Kepler(double semiMajorAxis, double eccentricity, double inclination, double longOfAscNode, double argOfPerigee, double v0, double startEpoch, double mu)
    {
        this.semiMajorAxis = semiMajorAxis;
        this.eccentricity = eccentricity;
        this.inclination = inclination * degToRad;
        this.argOfPerigee = argOfPerigee * degToRad;
        this.longOfAscNode = longOfAscNode * degToRad;
        this.mu = mu;
        this.v0 = v0;
        this.startingEpoch = startEpoch;
    }
}



