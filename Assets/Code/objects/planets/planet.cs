using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class planet : body, IJsonFile<jsonPlanetStruct>
{
    public planetRepresentation representation {get; private set;}
    private planetData data;

    public double radius {get {return data.radius;}}
    public planetType pType {get {return data.pType;}}
    public position rotation {get; private set;}

    public planet(string name, planetData data, representationData rData)
    {
        base.name = name;
        this.data = data;
        base.init(rData);

        master.allPlanets.Add(this);
        master.requestJsonQueueUpdate();
    }

    // INITALIZATION
    private protected override void loadPhysicalData(representationData rData) {representation = new planetRepresentation(name, radius, pType, rData);}

    // EVENTS
    public override void updatePosition(object sender, EventArgs args)
    {
        localPos = pos = data.positions.find(master.time);
        if (!ReferenceEquals(parent, null)) pos += parent.pos;

        if (!planetOverview.usePlanetOverview) representation.setPosition(pos - master.currentPosition - master.referenceFrame);
        else
        {
            representation.setPosition(planetOverview.planetOverviewPosition(pos));
            representation.setRadius((master.scale / 2.0) / 4.0);
        }

        if (data.rotate) rotation = representation.rotate(this.calculateRotation());

        base.updateChildren();
    }
    public override position requestLocalPosition(Time t)
    {
        position p = data.positions.find(t);
        if (!ReferenceEquals(parent, null)) p += parent.requestLocalPosition(t);
        return p;
    }
    public override void updateScale(object sender, EventArgs args)
    {
        representation.setRadius(data.radius);
    }

    public position geoOnPlanet(geographic g, double alt) => Quaternion.Euler((Vector3) this.rotation) * (Vector3) g.toCartesian(this.radius + alt).swapAxis();

    private position calculateRotation()
    {
        double lon = 0;
        //double lat = 0;
        double timezone = 0;
        double jc = master.time.julianCentury;
        double tr = Math.PI / 180.0;
        double td = 180.0 / Math.PI;

        // calculate solar noon in julian
        double gMeanLonSun = (280.46646 + jc * (36000.76983 + jc * 0.0003032)) % 360.0;
        double gMeanAnomSun = 357.52911 + jc * (35999.05029 - 0.0001537 * jc);
        double eEarthOrbit = 0.016708634 - jc * (0.000042037 + 0.0000001267 * jc);
        double meanOblEcliptic = 23.0 + (26.0 + ((21.448 - jc * (46.815 + jc * (0.00059 - jc * 0.001813)))) / 60.0) / 60.0;
        double oblCorr = meanOblEcliptic + 0.00256 * Math.Cos(tr * (125.04 - 1934.136 * jc));
        double y = Math.Tan(tr * (oblCorr / 2.0)) * Math.Tan(tr * (oblCorr / 2.0));

        double eqOfTime = 4.0 * td * ((y * Math.Sin(2.0 * (tr * gMeanLonSun))) -
            2.0 * eEarthOrbit * Math.Sin(tr * gMeanAnomSun) +
            4.0 * eEarthOrbit * y * Math.Sin(tr * gMeanAnomSun) * Math.Cos(2.0 * tr * gMeanLonSun) -
            0.5 * y * y * Math.Sin(4 * tr * gMeanLonSun) -
            1.25 * eEarthOrbit * eEarthOrbit * Math.Sin(2 * tr * gMeanAnomSun));
        double solarNoon = (720.0 - 4.0 * lon - eqOfTime + timezone * 60.0) / 1440.0;
        double solarNoonJulian = (solarNoon - 0.5 < 0) ? solarNoon + 0.5 : solarNoon - 0.5;

        // calc sun declination
        double sunEqOfCtr = Math.Sin(tr * gMeanAnomSun) * (1.914602 - jc * (0.004817 + 0.000014 * jc)) + Math.Sin(tr * (2 * gMeanAnomSun)) * (0.0199993 - 0.000101 * jc) + Math.Sin(tr * (3 * gMeanAnomSun)) * 0.000289;
        double sunTrueLon = gMeanLonSun + sunEqOfCtr;
        double sunAppLon = sunTrueLon - 0.00569 - 0.00478 * Math.Sin(tr * (125.04 - 1934.136 * jc));
        double sunDeclin = td * (Math.Asin(Math.Sin(tr * oblCorr) * Math.Sin(tr * sunAppLon)));

        // get time until next solar noon
        double currentHour = master.time.julian % 1;
        solarNoonJulian += (currentHour > solarNoonJulian) ? 1 : 0;
        double timeUntilSNoon = solarNoonJulian - currentHour;

        position noonPos = data.positions.find(new Time(master.time.julian));
        geographic noonLatLon = geographic.toGeographic(master.sun.pos - noonPos, this.data.radius);

        double rotationDirection = 1;
        double rotationDays = rotationDirection * (-noonLatLon.lon + (360 * timeUntilSNoon));

        return new position(rotationDays, sunDeclin, 0);
    }

    public new jsonPlanetStruct requestJsonFile()
    {
        return new jsonPlanetStruct() {
            radius = this.radius,
            positions = this.data.positions.requestJsonFile(),
            name = this.name,
            representationData = this.representation.requestJsonFile(),
            rotate = this.data.rotate,
            planetType = (int) this.pType,
            bodyData = base.requestJsonFile()
        };
    }
    
    public override int GetHashCode() => name.GetHashCode();
}

public class planetData
{
    public double radius;
    public bool rotate;
    public readonly planetType pType;
    public Timeline positions;

    public planetData(double radius, bool rotate, string positionPath, double timestep, planetType pType)
    {
        this.radius = radius;
        this.pType = pType;
        this.rotate = rotate;
        positions = csvParser.loadPlanetCsv(positionPath, timestep);
    }

    public planetData(double radius, bool rotate, TextAsset positionAsset, double timestep, planetType pType)
    {
        this.radius = radius;
        this.pType = pType;
        this.rotate = rotate;
        positions = csvParser.loadPlanetCsv(positionAsset, timestep);
    }

    public planetData(double radius, bool rotate, Timeline positions, double timestep, planetType pType)
    {
        this.radius = radius;
        this.pType = pType;
        this.rotate = rotate;
        this.positions = positions;
    }

}

public enum planetType
{
    planet = 0,
    moon = 1
}