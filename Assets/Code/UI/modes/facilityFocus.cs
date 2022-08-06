using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

public static class facilityFocus
{
    public static planet parent {get; private set;}
    public static string facilityName {get; private set;}
    public static geographic sw;
    public static position pointCenteringOffset;

    public static Dictionary<string, facilityFocusRepresentation> representations = new Dictionary<string, facilityFocusRepresentation>();

    public static void enable(bool use, string facilityName) {
        facilityFocus.facilityName = facilityName;

        if (use) {
            // unload all facilites (done in facilityRepresentation.cs)
            parent = master.allFacilites.First(x => x.name == facilityName).facParent;

            SceneManager.LoadScene("facilityFocus", LoadSceneMode.Single);
        } else {
            
        }
    }

    public static void loadTerrain() {
        string path = $"C:/Users/leozw/Desktop/dteds/{facilityName}";
        string predictedHrt = Path.Combine(path, facilityName + ".hrt");
        string predictedPng = Path.Combine(path, facilityName + ".png");

        Texture2D tex = new Texture2D(10980, 10980);
        tex.LoadImage(File.ReadAllBytes(predictedPng));
        Material m = new Material(Resources.Load("Materials/planets/earth/earth") as Material);
        m.mainTexture = tex;

        meshDistributor<dtedBasedMesh> md = highResTerrain.readHRT(predictedHrt);
        md.drawAll(m, Resources.Load("Prefabs/PlanetMesh") as GameObject, new string[0], GameObject.FindGameObjectWithTag("facilityFocus/parent").transform);

        facilityFocus.sw = md.baseType.sw;
        facilityFocus.pointCenteringOffset = md.baseType.offset;
    }
}
