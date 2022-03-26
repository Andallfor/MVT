using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Concurrent;

public class planetRepresentation : MonoBehaviour
{
    private float _r;
    private TextMeshProUGUI shownName;
    private planetType pType;
    private GameObject canvas;
    private GameObject planetParent;
    private MeshRenderer mr;
    public Collider hitbox;

    private representationData data;
    public bool forceHide = false, forceDisable = false;

    // initalization
    public void init(string name, double radius, planetType pType, representationData data)
    {
        this.gameObject.name = name;
        this.setRadius(radius);
        this.data = data;
        this.pType = pType;
        this.canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        this.mr = this.GetComponent<MeshRenderer>();
        this.hitbox = this.GetComponent<Collider>();
        
        this.shownName = Instantiate(Resources.Load("Prefabs/bodyName") as GameObject).GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(this.canvas.transform, false);
        shownName.fontSize = 25;
        shownName.text = name;
        shownName.fontStyle = FontStyles.SmallCaps | FontStyles.Bold | FontStyles.Italic;

        planetParent = GameObject.FindGameObjectWithTag("planet/parent");
    }

    public static planetRepresentation createPlanet(string name, double radius, planetType pType, representationData data)
    {
        GameObject go = Instantiate(data.model);   
        go.GetComponent<MeshRenderer>().material = data.material;
        go.transform.parent = GameObject.FindGameObjectWithTag("planet/parent").transform;
        planetRepresentation pr = go.GetComponent<planetRepresentation>();
        pr.init(name, radius, pType, data);
        return pr;
    }

    public jsonPlanetRepresentationStruct requestJsonFile()
    {
        return new jsonPlanetRepresentationStruct() {
            modelPath = this.data.modelPath,
            materialPath = this.data.materialPath};
    }

    // updating shown values
    public void setPosition(position pos)
    {
        bool endDisable = false;
        if (planetOverview.usePlanetOverview && pType == planetType.moon)
        {
            endDisable = false;
            shownName.gameObject.SetActive(false);
            return;
        }

        // scale position
        Vector3 p = new Vector3(
            (float) (pos.x / master.scale),
            (float) (pos.y / master.scale),
            (float) (pos.z / master.scale));

        if (Vector3.Distance(p, Vector3.zero) > 1000f) endDisable = false; // hide if too far away
        else
        {
            endDisable = true;
            gameObject.transform.localPosition = p;

            // scale far away planets so they can be seen better
            float distance = Vector3.Distance(Vector3.zero, this.gameObject.transform.position);
            float scale = 0.01f * distance + 0;
            float r = Mathf.Max(Mathf.Min(this.gameObject.transform.localScale.x, _r), scale);
            gameObject.transform.localScale = new Vector3(r, r, r);
        }

        // position the name of the planet so that it is over the displayed position
        // rotate point since this is the localPosition point, and does not account for possible
        // rotations of its parent
        Vector3 rot = planetParent.transform.rotation.eulerAngles * Mathf.Deg2Rad;
        Vector3 rotatedPoint = uiHelper.vRotate(rot.y, rot.x, rot.z, p);
        uiHelper.drawTextOverObject(shownName.gameObject, rotatedPoint);

        if (forceDisable && gameObject.activeSelf) gameObject.SetActive(false);
        else if (!forceDisable && gameObject.activeSelf != endDisable) gameObject.SetActive(endDisable);
        if (forceHide && mr.enabled) mr.enabled = false;
        else if (!forceHide && !mr.enabled) mr.enabled = true;
    }
    public Vector3 rotate(position p)
    {
        this.transform.localEulerAngles = new Vector3((float) p.y, (float) p.x, 0);
        return new Vector3(this.transform.localEulerAngles.x % 360,
                           this.transform.localEulerAngles.y % 360,
                           this.transform.localEulerAngles.z % 360);
    }
    public void setRadius(double radius)
    {
        _r = ((float) Math.Max((radius * 2) / master.scale, 0.05));
        gameObject.transform.localScale = new Vector3(_r, _r, _r);
    }
}

