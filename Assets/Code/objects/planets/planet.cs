using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class planet : body
{
    public planetRepresentation representation {get; private set;}
    private planetData data;

    public double radius {get {return data.radius;}}
    public planetType pType {get {return data.pType;}}
    public position rotation {get; private set;}
    public trailRenderer tr;
    public double yt;
    public double p1check;
    public double secd = 0;
    public double p2check;

    public planet(string name, planetData data, representationData rData, planet parent = null) {
        if (master.allPlanets.Exists(x => x.name == name)) Debug.LogWarning("Duplicate planet detected");

        base.name = name;
        base.positions = data.positions;
        this.data = data;
        base.init(rData);

        master.allPlanets.Add(this);
        master.requestJsonQueueUpdate();

        if (parent != null) body.addFamilyNode(parent, this);

        tr = new trailRenderer(name, representation.gameObject, positions, this);
    }

    // INITIALIZATION
    private protected override void loadPhysicalData(representationData rData) {representation = new planetRepresentation(name, radius, pType, rData);}

    // EVENTS
    public override void updatePosition(object sender, EventArgs args)
    {
        localPos = pos = data.positions.find(master.time);
        if (!ReferenceEquals(parent, null)) pos += parent.pos;

        representation.setPosition(pos - master.currentPosition - master.referenceFrame);
        if (planetOverview.instance.active) representation.setRadius(general.camera.orthographicSize * 0.2f * (master.scale / 2.0) / 4.0);

        if (data.rotate == rotationType.moon)
        {
            Quaternion newQuaternion = new Quaternion();
            newQuaternion.Set(0f, 0f, 0f, 1);
            representation.gameObject.transform.rotation = newQuaternion;
            position moon = master.rod[0].find(master.time).swapAxis();
            position velocity = master.rod[1].find(master.time).swapAxis();
            position unitMoon = new position(moon.x / Math.Sqrt(moon.x * moon.x + moon.y * moon.y + moon.z * moon.z), moon.y / Math.Sqrt(moon.x * moon.x + moon.y * moon.y + moon.z * moon.z), moon.z / Math.Sqrt(moon.x * moon.x + moon.y * moon.y + moon.z * moon.z)) * -1;
            float theta2 = (float)(Math.Asin(-1 * unitMoon.z));
            float theta1 = (float)(Math.Atan2(unitMoon.y, unitMoon.x));
            float thetaCheck = (float)(Math.Acos(unitMoon.x / Math.Cos(theta2)));
            float zAngle = (float)(-1 * theta2 * 180 / Math.PI);
            float yAngle1 = (float)(-1 * theta1 * 180 / Math.PI);
            float yAngle2 = (float)(-1 * thetaCheck * 180 / Math.PI);

            position yprime = new position(-1 * Math.Sin(theta2) * Math.Cos(theta1), -1 * Math.Sin(theta2) * Math.Sin(theta1), Math.Cos(theta2));
            position k3 = position.cross(moon, velocity);
            position yAxis = position.cross(k3, moon);

            float xAngle = (float)(position.dotProductTheta(yAxis, yprime) * 180 / Math.PI);

            representation.gameObject.transform.Rotate(0f, yAngle1, zAngle);
            representation.gameObject.transform.Rotate(new Vector3(1, 0, 0), xAngle);
        }
        //else if (data.rotate == rotationType.earth) rotation = representation.rotate(this.calculateRotation());
        else if (data.rotate == rotationType.earth)
        {
            position r = this.calcRotWNutation().swapAxis();
            /*if (secd == 0)
            {
                representation.gameObject.transform.Rotate(45f, 0f, 0f);
                /*representation.gameObject.transform.RotateAround(transform.position, Vector3.right, rotation.x);
                representation.gameObject.transform.RotateAround(transform.position, Vector3.up, rotation.y);
            }
            secd += 1;
            representation.gameObject.transform.RotateAround(representation.gameObject.transform.position, representation.gameObject.transform.up, .10f);
            */
            //representation.gameObject.transform.rotation = new Quaternion(0f,0f,0f,1f); 
            //representation.gameObject.transform.rotation = new Quaternion((float)r.x, (float)r.y, (float)r.z, 1f);
            //Debug.Log(representation.gameObject.transform.rotation);
        }


        base.updateChildren();
    }
    public override position requestPosition(Time t) => requestPosition(t.julian);

    public override position requestPosition(double julian) {
        position p = data.positions.find(julian);
        if (!ReferenceEquals(parent, null)) p += parent.requestPosition(julian);
        return p;
    }
    public override void updateScale(object sender, EventArgs args)
    {
        representation.setRadius(data.radius);
    }

    public position rotateLocalGeo(geographic g, double alt) => geographic.toGeographic(representation.gameObject.transform.rotation * (Vector3) (g.toCartesian(radius + alt)).swapAxis(), radius).toCartesian(radius + alt).swapAxis();

    /// <summary> Takes a world pos and converts it to the respective geographic on the planet, respecting the planets rotation </summary>
    public geographic worldPosToLocalGeo(position p) => geographic.toGeographic(Quaternion.Inverse(representation.gameObject.transform.rotation) * (Vector3) ((p - pos) / master.scale), radius);
    /// <summary> Takes a pos centered on (0, 0) and converts it to the respective geographic on the planet, respecting the planets rotation </summary>
    public geographic localPosToLocalGeo(position p) => geographic.toGeographic(Quaternion.Inverse(representation.gameObject.transform.rotation) * (Vector3) (p / master.scale), radius);
    public Vector3 localGeoToUnityPos(geographic g, double alt) {
        position c = representation.gameObject.transform.rotation * (Vector3) (g.toCartesian(radius + alt)).swapAxis();
        geographic gg = geographic.toGeographic(c, radius);
        return (Vector3) ((gg.toCartesian(radius + alt) + pos - master.currentPosition - master.referenceFrame) / master.scale).swapAxis();
    }


    private position calcRotWNutation()
    {
      double T = (master.time.julian - 2451545.0)/36525.0;
      double a0 = -0.641 * T;
      double d0 = -0.557 * T;

      double DegToRad = Math.PI / 180.0;

      double G = 0.0;
      double p1 = (876600.0 * 60.0 * 60.0 + 8640184.812866) * T;
      double p2 = 0.093104 * (T * T);
      double p3 = (0.0000062) * (T * T * T);     

        G = 67310.54841 + p1 + p2 - p3;
      
      double sec = G % 86400.0;

        double GST = sec / 240.0;

        double a3 = (GST + a0 * DegToRad * Math.Cos(d0 * DegToRad));

        representation.gameObject.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);
        representation.gameObject.transform.RotateAround(representation.gameObject.transform.position, representation.gameObject.transform.up, (float)(-a0 * DegToRad));
        representation.gameObject.transform.RotateAround(representation.gameObject.transform.position, representation.gameObject.transform.right, (float) (d0 * DegToRad));
        representation.gameObject.transform.RotateAround(representation.gameObject.transform.position, representation.gameObject.transform.up, (float)-a3);

        /*if (a3 > Math.PI)
        {
            Debug.Log("a3 greater than pi: " + a3);
            while (a3 > Math.PI)
            {
                a3 = a3 - Math.PI * 2;
            }
            Debug.Log("new a3: " + a3);

        }
        else if (a3 < -Math.PI)
            {
                Debug.Log("a3 less than negative pi: " + a3);
                while (a3 > -Math.PI)
                {
                    a3 = a3 + Math.PI * 2;
                }
                Debug.Log("new a3: " + a3);

            }

      (position, position, position) A = R3(a0 * DegToRad);
      (position, position, position) B = R1(-d0 * DegToRad);
      (position, position, position) C = R3(a3); // <---BEST APPROXIMATION

      (position, position, position) AB = position.mult2(A, B);
      (position, position, position) ABC = position.mult2(AB, C);

        double w = Math.Sqrt(1.0 + ABC.Item1.x + ABC.Item2.y + ABC.Item3.z) / 2.0;
        double w4 = (4.0 * w);
        double ax = (ABC.Item3.y - ABC.Item2.z) / w4;
        double ay = (ABC.Item1.z - ABC.Item3.x) / w4;
        double az = (ABC.Item2.x - ABC.Item1.y) / w4;*/



        //Debug.Log("p1: " + p1);

        //Debug.Log(new position(ax, ay, az));

        /*if (Math.Sign(ax) > 0)
        {
            Debug.Log("julian " + master.time.julian +"p1checker" + p1check + " gmst new" + GST * DegToRad + a0 * DegToRad * Math.Cos(d0 * DegToRad));
        }
        yt = az;
        p1check = GST * DegToRad + a0 * DegToRad * Math.Cos(d0 * DegToRad);
        */
        return new position(-1, -1, -1);

        
        /*
        Item 1 x y z
        Item 2 x y z
        Item 3 x y z
        */

    }

    private (position, position, position) R1(double x)
    {
      double ct = Math.Cos(x);
      double st = Math.Sin(x);
      return (new position (1,  0,   0),
              new position (0,  ct,  st),
              new position (0, -st,  ct));
    }

    private (position, position, position) R3(double x)
    {
      double ct = Math.Cos(x);
      double st = Math.Sin(x);
      return (new position (ct,   st,   0),
              new position (-st,  ct,   0),
              new position (0,    0,    1));
    }

    public rotationType getRotationType() => data.rotate;

    public override int GetHashCode() => name.GetHashCode();
}

public class planetData
{
    public double radius;
    public rotationType rotate;
    public readonly planetType pType;
    public Timeline positions;

    public planetData(double radius, rotationType rotate, string positionPath, double timestep, planetType pType)
    {
        this.radius = radius;
        this.pType = pType;
        this.rotate = rotate;
        positions = csvParser.loadPlanetCsv(positionPath, timestep);
    }

    public planetData(double radius, rotationType rotate, TextAsset positionAsset, double timestep, planetType pType)
    {
        this.radius = radius;
        this.pType = pType;
        this.rotate = rotate;
        positions = csvParser.loadPlanetCsv(positionAsset, timestep);
    }

    public planetData(double radius, rotationType rotate, Timeline positions, planetType pType)
    {
        this.radius = radius;
        this.pType = pType;
        this.rotate = rotate;
        this.positions = positions;
    }

}

public enum planetType : byte
{
    planet = 0,
    moon = 1
}

[Flags]
public enum rotationType : byte
{
    earth = 1,
    moon = 2,
    none = 4,
}
