using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text.RegularExpressions;

public class jplScenario : IScenario {
    protected override IEnumerator _generate() {
        representationData rd = new representationData("planet", "defaultMat");
        representationData frd = new representationData("facility", "defaultMat");

        var data = DBReader.getData();

        double prevTime = master.time.julian;
        double startTime = Time.strDateToJulian(data["EarthTest"].epoch);
        master.time.addJulianTime(Time.strDateToJulian(data["EarthTest"].epoch) - prevTime);

        double EarthMu = 398600.0;
        double moonMu = 4900.0;
        double sunMu = 132712e+6;
        double marsMu = 4.2828373716854781E+04;
        double epoch = 2459945.5000000;

        List<satellite> earthSats = new List<satellite>();
        List<satellite> moonSats = new List<satellite>();

        planet earth = new planet("Earth", new planetData(6356.75, rotationType.earth, new Timeline(149548442.3703442, 1.638666603580831e-02, 3.094435789048925e-03, 2.514080725047589e+02, 2.130422595065601e+02, 3.556650570783973e+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "earthTex"));
        planet moon = new planet("Luna", new planetData(1738.1, rotationType.none, new Timeline(3.809339850602024E+05, 5.715270081780017E-02, 5.100092074801750, 2.355576097735322E+02, 4.145906044051883E+01, 1.102875277317696E+02, 1, epoch, EarthMu), planetType.moon), new representationData("planet", "moonTex"), earth);
        planet mercury = new planet("Mercury", new planetData(2439.7, rotationType.none, new Timeline(5.790908989105575E+07, 2.056261098757466E-01, 7.003572595914431, 2.919239643694458E+01, 4.830139915269011E+01, 3.524475388676764E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "mercuryTex"));
        planet venus = new planet("Venus", new planetData(6051.8, rotationType.none, new Timeline(1.082084649394783E+08, 6.763554426926404E-03, 3.394414241082599, 5.465872015572022E+01, 7.661682984113415E+01, 1.894020065218014E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "venusTex"));
        planet jupiter = new planet("Jupiter", new planetData(71492, rotationType.none, new Timeline(7.784060565591711E+08, 4.849069877916937E-02, 1.303574691375196, 2.734684496159951E+02, 1.005141062239384E+02, 3.583753533457323E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "jupiterTex"));
        planet saturn = new planet("Saturn", new planetData(60268, rotationType.none, new Timeline(1.432942173696950E+09, 5.347693294083503E-02, 2.486204025043723, 3.351085660472628E+02, 1.135934765410469E+02, 2.425433471952230E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "saturnTex"));
        planet uranus = new planet("Uranus", new planetData(25559, rotationType.none, new Timeline(2.884647050482038E+09, 4.382306037308097E-02, 7.711154256612637E-01, 9.259562572483993E+01, 7.406386852962497E+01, 2.449588470478398E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "uranusTex")); 
        planet neptune = new planet("Neptune", new planetData(24764, rotationType.none, new Timeline(4.533353284339735E+09, 1.474287247824008E-02, 1.768865936090221, 2.519545559747149E+02, 1.317418592957744E+02, 3.314896534126764E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "neptuneTex"));
        planet pluto = new planet("Pluto", new planetData(1188.3, rotationType.none, new Timeline(5.921110734220912E+09, 2.497736459257524E-01, 1.734771133563329E+01, 1.140877773351901E+02, 1.104098121556590E+02, 4.784240505983976E+01, 1, epoch, sunMu), planetType.planet), new representationData("planet", "moonTex"));
        
        planet mars = new planet("Mars", new planetData(3389.92, rotationType.none, new Timeline(2.279254603773820E+08, 9.344918986180577E-02, 1.847925718684767, 2.866284864604108E+02, 4.948907666319641E+01, 1.014635329635219E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "marsTex"));
        planet phobos = new planet("Phobos", new planetData(13.1, rotationType.none, new Timeline(9.377954455101617E+03, 1.494832784194627E-02, 2.714060115370244E+01, 3.746859017440811E+01, 8.502149665357845E+01, 2.360124366197488E+02, 1, epoch, marsMu), planetType.planet), new representationData("planet", "marsTex"), mars);
        planet deimos = new planet("Deimos", new planetData(7.8, rotationType.none, new Timeline(2.345960371672593E+04, 2.647108425766069E-04, 2.451161814535570E+01, 8.634724237995439E+00, 7.993040904198153E+01, 2.743515837687091E+02, 1, epoch, marsMu), planetType.planet), new representationData("planet", "marsTex"), mars);

        List<string> facs = new List<string>();
        List<facility> earthfacs = new List<facility>();

        foreach (KeyValuePair<string, dynamic> x in data["EarthTest"].satellites)
        {
            var dict = data["EarthTest"].satellites[x.Key];
            if (x.Key == "My Satellite") continue;

            double start = 0, stop = 0;
            if (dict["TimeInterval_start"] is string) start = Double.Parse(dict["TimeInterval_start"], System.Globalization.NumberStyles.Any);
            else start = (double)dict["TimeInterval_start"];

            if (dict["TimeInterval_stop"] is string) stop = Double.Parse(dict["TimeInterval_stop"], System.Globalization.NumberStyles.Any);
            else stop = (double)dict["TimeInterval_stop"];

            if (dict["Type"] == "Satellite")
            {
                satellite sat = null;

                double A = 0, E = 0, I = 0, RAAN = 0, W = 0, M = 0;

                if (dict["SemimajorAxis"] is string) A = Double.Parse(dict["SemimajorAxis"], System.Globalization.NumberStyles.Any);
                else A = (double)dict["SemimajorAxis"];
                if (dict["Eccentricity"] is string) E = Double.Parse(dict["Eccentricity"], System.Globalization.NumberStyles.Any);
                else E = (double)dict["Eccentricity"];
                if (dict["Inclination"] is string) I = Double.Parse(dict["Inclination"], System.Globalization.NumberStyles.Any);
                else I = (double)dict["Inclination"];
                if (dict["Arg_of_Perigee"] is string) W = Double.Parse(dict["Arg_of_Perigee"], System.Globalization.NumberStyles.Any);
                else W = (double)dict["Arg_of_Perigee"];
                if (dict["RAAN"] is string) RAAN = Double.Parse(dict["RAAN"], System.Globalization.NumberStyles.Any);
                else RAAN = (double)dict["RAAN"];
                if (dict["MeanAnomaly"] is string) M = Double.Parse(dict["MeanAnomaly"], System.Globalization.NumberStyles.Any);
                else M = (double)dict["MeanAnomaly"];

                if (dict["CentralBody"] == "Moon")
                {
                    sat = new satellite(x.Key, new satelliteData(new Timeline(A, E, I, W, RAAN, M, 1, Time.strDateToJulian(dict["OrbitEpoch"]), moonMu)), rd, moon);
                    moonSats.Add(sat);
                }
                else if (dict["CentralBody"] == "Earth")
                {
                    sat = new satellite(x.Key, new satelliteData(new Timeline(A, E, I, W, RAAN, M, 1, Time.strDateToJulian(dict["OrbitEpoch"]), EarthMu)), rd, earth);
                    earthSats.Add(sat);
                }

                if (dict["user_provider"] == "user" || dict["user_provider"] == "user/provider") linkBudgeting.users.Add(x.Key, (false, startTime + start, startTime + stop));
            }
            else if (dict["Type"] == "Facility")
            {
                double lat = 0, lon = 0, alt = 0;
                if (dict["Lat"] is string) lat = Double.Parse(dict["Lat"], System.Globalization.NumberStyles.Any);
                else lat = (double)dict["Lat"];
                if (dict["Long"] is string) lon = Double.Parse(dict["Long"], System.Globalization.NumberStyles.Any);
                else lon = (double)dict["Long"];
                if (dict["AltitudeConstraint"] is string) alt = Double.Parse(dict["AltitudeConstraint"], System.Globalization.NumberStyles.Any);
                else alt = (double)dict["AltitudeConstraint"];

                string facilityName = Regex.Replace(x.Key, @"[\d]", String.Empty);

                if (master.fac2ant.ContainsKey(facilityName)) master.fac2ant[facilityName].Add(x.Key);
                else master.fac2ant[facilityName] = new List<string> { x.Key };


                if (dict["CentralBody"] == "Moon")
                {
                    if (!facs.Contains(facilityName))
                    {
                        facility fd = new facility(facilityName, moon, new facilityData(facilityName, new geographic(lat, lon), alt, new List<antennaData>(), new Time(startTime + start), new Time(startTime + stop)), frd);
                        facs.Add(facilityName);
                        if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(facilityName, (true, startTime + start, startTime + stop));
                    }
                }
                else if (dict["CentralBody"] == "Earth")
                {
                    if (!facs.Contains(facilityName))
                    {
                        facility fd = new facility(facilityName, earth, new facilityData(facilityName, new geographic(lat, lon), alt, new List<antennaData>(), new Time(startTime + start), new Time(startTime + stop)), frd);
                        facs.Add(facilityName);
                        earthfacs.Add(fd);
                        if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(facilityName, (true, startTime + start, startTime + stop));
                    }
                }
            }
        }
        master.relationshipSatellite[earth] = earthSats;
        master.relationshipSatellite[moon] = moonSats;
        master.relationshipFacility[earth] = earthfacs;

        yield return new WaitForSeconds(0.1f);
        loadingController.addPercent(0.11f);
        loadingController.addPercent(1);

        metadata.timeStart = startTime;
        metadata.importantBodies = new Dictionary<string, body>() {
            {earth.name, earth},
            {moon.name, moon}
        };
    }
}
