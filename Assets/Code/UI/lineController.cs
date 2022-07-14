using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class lineController : MonoBehaviour
{
    private Dictionary<int, GameObject> captions = new Dictionary<int, GameObject>();
    private LineRenderer lr;
    public Color color {get {return lr.endColor;}}

    private void OnEnable() {this.lr = GetComponent<LineRenderer>();}

    public void drawLine(List<Vector3> pos, Color c)
    {
        clearLine();
        lr.positionCount = pos.Count;
        lr.SetPositions(pos.ToArray());
        lr.startColor = c;
        lr.endColor = c;
    }
    public void clearLine()
    {
        lr.positionCount = 0;
        foreach (GameObject go in captions.Values) Destroy(go);
        captions = new Dictionary<int, GameObject>();
    }

    public void setColor(Color c)
    {
        lr.startColor = c;
        lr.endColor = c;
    }

    public void addCaption(int pos, string text, int size)
    {
        GameObject go = GameObject.Instantiate(Resources.Load("Prefabs/text") as GameObject);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        go.transform.SetParent(GameObject.FindGameObjectWithTag("ui/canvas").transform, worldPositionStays: false);

        captions.Add(pos, go);
    }
    public void removeCaption(int pos)
    {
        Destroy(captions[pos]);
        captions.Remove(pos);
    }

    public Vector3 requestPosition(int index) => lr.GetPosition(index);

    public void rotateAround(float pitch, float roll, float yaw, Vector3 center)
    {
        Vector3[] newPos = new Vector3[lr.positionCount];
        for (int i = 0; i < lr.positionCount; i++)
        {
            Vector3 p = lr.GetPosition(i) - center;
            newPos[i] = uiHelper.vRotate(pitch, roll, yaw, p);
        }

        lr.SetPositions(newPos);
    }

    private void Update()
    {
        foreach (KeyValuePair<int, GameObject> kvp in captions)
        {
            if (kvp.Key > lr.positionCount) continue;

            kvp.Value.transform.position = Camera.main.WorldToScreenPoint(lr.GetPosition(kvp.Key));
        }
    }

    public void destroy() {
        Destroy(this.gameObject);
    }
}