using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class sceneController
{
    public static bool alreadyStarted = false;

    public static void prepareScene(Scene scene, LoadSceneMode mode) {
        if (scene.name == "main") prepareMain();
        else if (scene.name == "facilityFocus") prepareFacilityFocus();
    }

    public static IEnumerator mainLoop;

    private static void prepareMain() {
        // recreate all representations
        uiHelper.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.camera = Camera.main;
        general.planetParent = GameObject.FindGameObjectWithTag("planet/parent");

        foreach (planet p in master.allPlanets) p.representation.regenerate();
        foreach (satellite s in master.allSatellites) s.representation.regenerate();
        foreach (facility f in master.allFacilities) f.representation.regenerate();

        general.camera.gameObject.GetComponent<controller>().startMainLoop();

        planetFocus.instance.toggle(false);
    }

    private static void prepareFacilityFocus() {
        facilityFocus.loadTerrain();

        uiHelper.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.camera = Camera.main;

        general.camera.gameObject.GetComponent<facilityFocusController>().init();
    }
}
