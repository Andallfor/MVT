using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

public static class facilityFocus
{
    public static bool useFacilityFocus {get; internal set;}
    public static planet parent {get; internal set;}
    public static string facility {get; internal set;}

    public static void enable(bool use, string facilityName) {
        useFacilityFocus = use;
        facility = facilityName;

        if (use) {
            // unload all facilites (done in facilityRepresentation.cs)
            // unload facility parent planet
            parent = master.allFacilites.First(x => x.name == facilityName).facParent;
            // offset player
            geographic g = csvParser.facilityLocations.First(x => x.Value == facilityName).Key;
            position offset = dtedReader.centerDtedPoint(g - new geographic(0.5, 0.5), g, new position(0, 0, 0), 0).swapAxis();

            master.currentPosition = offset;

            // look for applicable hrt and load it
            string path = $"C:/Users/leozw/Desktop/dteds/{facilityName}";
            string predictedHrt = Path.Combine(path, facilityName + ".hrt");
            string predictedPng = Path.Combine(path, facilityName + ".png");

            SceneManager.LoadScene("facilityFocus", LoadSceneMode.Single);
        } else {
            
        }
    }
}
