using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public class uiNavPanel : MonoBehaviour
{
    public TMP_Dropdown referenceFrameSelector;
    public TextMeshProUGUI userPos, povPos, focusPlanet, surfaceView, overview, showTrails;
    public uiButtonPressDetector w, s, a, d, plus, minus;
    public Button planetFocusButton, uiMapButton, zoomInto;

    private body refFrame;
    private Transform t;
    private double radius;

    public void Awake() {
        master.onFinalSetup += (s, e) => generatePossibleReferenceFrames();

        referenceFrameSelector.onValueChanged.AddListener(setReferenceFrame);

        general.onStatusChange += updateLabels;
        general.onTrailChange += updateLabels;
        master.time.onChange += ((s, e) => {
            povPos.text = $"[{refFrame.pos.x.ToString("G2")}, {refFrame.pos.y.ToString("G2")}, {refFrame.pos.z.ToString("G2")}]";
        });
    }

    public void moveY(int multi) {
        Vector3 foward = general.camera.transform.forward;
        if (planetFocus.usePlanetFocus) planetFocus.movementOffset += (float) master.scale * 0.75f * general.camera.transform.up * planetFocus.zoom / 40f * UnityEngine.Time.deltaTime * (float) multi;
        else master.currentPosition += foward * 5f * (float) master.scale * UnityEngine.Time.deltaTime * (float) multi;
    }

    public void moveX(int multi) {
        Vector3 right = general.camera.transform.right;
        if (planetFocus.usePlanetFocus) planetFocus.movementOffset += (float) master.scale * 0.75f * right * planetFocus.zoom / 40f * UnityEngine.Time.deltaTime * (float) multi;
        else if (planetOverview.usePlanetOverview) planetOverview.rotationalOffset += 90f * (float) multi * UnityEngine.Time.deltaTime * Mathf.Deg2Rad;
        else master.currentPosition += right * 5f * (float) master.scale * UnityEngine.Time.deltaTime * (float) multi;
    }

    public void changeZoom(int multi) {
        if (planetOverview.usePlanetOverview) {
            general.camera.orthographicSize -= (float) multi * UnityEngine.Time.deltaTime * 2.5f;
            general.camera.orthographicSize = Math.Max(2, Math.Min(20, general.camera.orthographicSize));
        } else if (planetFocus.usePlanetFocus) {
            if (planetFocus.usePoleFocus) {
                float change = (float) (master.scale) * multi * UnityEngine.Time.deltaTime;
                master.scale -= change;
                planetFocus.update();
                master.requestPositionUpdate();
            } else {
                planetFocus.zoom -= multi * planetFocus.zoom * UnityEngine.Time.deltaTime;
                planetFocus.zoom = Mathf.Max(Mathf.Min(planetFocus.zoom, 90), 7f);
            }
        } else master.currentPosition += ((position) (t.position - general.camera.transform.position).normalized) * master.scale * 5f * UnityEngine.Time.deltaTime * multi;
    }

    public void Update() {
        if (Input.GetMouseButton(0)) {
            if (w.buttonPressed) moveY(1);
            if (s.buttonPressed) moveY(-1);
            if (d.buttonPressed) moveX(-1);
            if (a.buttonPressed) moveX(1);

            if (plus.buttonPressed) changeZoom(1);
            if (minus.buttonPressed) changeZoom(-1);
        }

        userPos.text = $"[{Math.Round(master.currentPosition.x, 2)}, {Math.Round(master.currentPosition.y, 2)}, {Math.Round(master.currentPosition.z, 2)}]";
    }

    public void generatePossibleReferenceFrames() {
        referenceFrameSelector.options = new List<TMP_Dropdown.OptionData>();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (planet p in master.allPlanets) {
            if (!p.positions.exists(master.time)) continue;
            options.Add(new TMP_Dropdown.OptionData(p.name));
        }
        foreach (satellite s in master.allSatellites) {
            if (!s.positions.exists(master.time)) continue;
            options.Add(new TMP_Dropdown.OptionData(s.name));
        }

        referenceFrameSelector.options = options;

        int index = options.IndexOf(options.First(x => x.text == master.requestReferenceFrame().name));
        referenceFrameSelector.SetValueWithoutNotify(index);

        refFrame = master.requestReferenceFrame();

        if (refFrame is planet) {
            planet p = (planet) refFrame;
            t = p.representation.gameObject.transform;
            radius = p.radius;
        }
        else if (refFrame is satellite) {
            satellite s = (satellite) refFrame;
            t = s.representation.gameObject.transform;
            radius = 5;
        }
    }

    public void setReferenceFrame(int i) {
        string frame = referenceFrameSelector.options[i].text;

        body b = null;
        if (master.allPlanets.Exists(x => x.name == frame)) b = master.allPlanets.First(x => x.name == frame);
        else if (master.allSatellites.Exists(x => x.name == frame)) b = master.allSatellites.First(x => x.name == frame);

        refFrame = b;
        master.setReferenceFrame(b);
        master.currentPosition = new position(0, 0, 0);
        master.requestPositionUpdate();

        resetFocus();
    }

    public void resetFocus() {
        if (planetOverview.usePlanetOverview) general.camera.orthographicSize = 5;
        else if (planetFocus.usePlanetFocus) {
            planetFocus.reset();
            if (planetFocus.usePoleFocus) {
                master.scale = 50;
                master.currentPosition = planetFocus.focus.rotateLocalGeo(new geographic(-90, 0), 0);
                planetFocus.focus.representation.forceHide = true;
            }
            else {
                master.scale = planetFocus.focus.radius / (0.6 * -general.defaultCameraPosition.z);
                master.requestPositionUpdate();
                general.camera.transform.LookAt(planetFocus.focus.representation.gameObject.transform.position);
            }
        } else {
            if (refFrame is planet) {
                planet p = (planet) refFrame;
                t = p.representation.gameObject.transform;
                radius = p.radius;
            }
            else if (refFrame is satellite) {
                satellite s = (satellite) refFrame;
                t = s.representation.gameObject.transform;
                radius = 5;
            }

            master.currentPosition = new position(0, 0, -radius * 1.5);
            master.requestPositionUpdate();

            general.camera.transform.localEulerAngles = new Vector3(0, 0, 0);

            if (planetOverview.usePlanetOverview) planetOverview.enable(false);
            if (planetFocus.usePlanetFocus) planetFocus.enable(false);
            if (uiMap.useUiMap) uiMap.map.toggle(false);

            if (general.showingTrails) {
                foreach (planet p in master.allPlanets) p.tr.enable();
                trailRenderer.drawAllSatelliteTrails(master.allSatellites);
            }
        }
    }

    public void focus() {
        if (refFrame is planet) {
            planetOverview.enable(false);
            uiMap.map.toggle(false);
            master.requestScaleUpdate();
            planetFocus.enable(!planetFocus.usePlanetFocus);
            general.pt.unload();
            general.plt.clear();
            master.clearAllLines();
            general.notifyStatusChange();
            general.notifyTrailsChange();
        }
    }

    public void updateLabels(object sender, EventArgs e) {
        plus.button.interactable = true;
        minus.button.interactable = true;
        zoomInto.interactable = true;
        w.button.interactable = true;
        a.button.interactable = true;
        s.button.interactable = true;
        d.button.interactable = true;
        planetFocusButton.interactable = true;
        uiMapButton.interactable = true;

        if (planetFocus.usePlanetFocus) focusPlanet.text = "Unfocus Planet";
        else focusPlanet.text = "Focus Planet";

        if (uiMap.useUiMap) {
            surfaceView.text = "3D View";
            plus.button.interactable = false;
            minus.button.interactable = false;
            zoomInto.interactable = false;
            w.button.interactable = false;
            a.button.interactable = false;
            s.button.interactable = false;
            d.button.interactable = false;
        }
        else surfaceView.text = "2D Surface View";

        if (planetOverview.usePlanetOverview) {
            overview.text = "Hide Overview";
            w.button.interactable = false;
            s.button.interactable = false;
        }
        else overview.text = "Show Overview";

        if (general.showingTrails) showTrails.text = "Hide Trails";
        else showTrails.text = "Show Trails";

        if (master.requestReferenceFrame() is satellite) {
            planetFocusButton.interactable = false;
            uiMapButton.interactable = false;
        }
    }

    public void surface() {
        if (refFrame is planet) {
            planetOverview.enable(false);
            planetFocus.enable(false);
            master.requestScaleUpdate();
            uiMap.map.toggle(!uiMap.useUiMap);
            master.clearAllLines();

            general.notifyStatusChange();
        }
    }

    public void pOverview() {
        uiMap.map.toggle(false);
        planetFocus.enable(false);
        master.requestScaleUpdate();
        planetOverview.enable(!planetOverview.usePlanetOverview);
        master.clearAllLines();

        general.notifyStatusChange();

        master.requestPositionUpdate();
    }

    public void trails() {
        foreach (planet p in master.allPlanets) p.tr.enable(!general.showingTrails);
        if (!general.showingTrails) trailRenderer.drawAllSatelliteTrails(master.allSatellites);
        else foreach (satellite s in master.allSatellites) s.tr.disable();

        general.showingTrails = !general.showingTrails;
        general.notifyTrailsChange();
    }
}
