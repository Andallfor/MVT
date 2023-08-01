using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class objectName {
    public static List<objectName> allNames = new List<objectName>();
    public static Dictionary<objectNameType, List<objectName>> namesByType = new Dictionary<objectNameType, List<objectName>>();
    public static Dictionary<string, List<objectName>> namesByGroup = new Dictionary<string, List<objectName>>() {
        {"", new List<objectName>()}
    };
    public static SortedDictionary<int, List<objectName>> namesByPriority = new SortedDictionary<int, List<objectName>>();

    private static int lastFrameUpdated;

    private bool isInAllNames = false;
    private TextMeshProUGUI tmp;
    private GameObject tmpGo, src;
    private string text, group;
    private objectNameType type;
    private int priority = 0; // higher is better
    private float opacity = 1;
    private bool isCovered = false;
    
    public bool isHidden {get; private set;}

    public objectName(GameObject src, objectNameType type, string text, string group = "unsorted") {
        this.text = text;
        this.type = type;
        this.group = group;
        this.src = src;

        // group is just used for sorting, type actually determines what the text looks like
        tmpGo = resLoader.createPrefab("bodyName");
        tmpGo.transform.SetParent(GameObject.FindGameObjectWithTag("ui/bodyName").transform, false);
        tmpGo.name = text + " bodyName";
        tmp = tmpGo.GetComponent<TextMeshProUGUI>();

        tmp.text = text;

        // now get what it should look like
        switch (type) {
            case objectNameType.planet:
                priority = 30;
                tmp.fontSize = 25;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold | FontStyles.Italic;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.rectTransform.pivot = new Vector2(-0.05f, 0.5f);
                break;
            case objectNameType.satellite:
                priority = 20;
                tmp.fontSize = 20;
                break;
            case objectNameType.antenna:
                priority = 0;
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
                break;
            case objectNameType.facility:
                priority = 10;
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
                break;
        }

        tmp.autoSizeTextContainer = true;
        tmp.rectTransform.sizeDelta = tmp.GetPreferredValues();

        register(priority);
        register(type);
        register(group);
    }

    /// <summary> Check if text is hidden by another physical object </summary>
    public bool isObscured(int layerMask = (1 << 6) | (1 << 7)) {
        if (type == objectNameType.planet) return false;
        bool r = Physics.Linecast(general.camera.transform.position, src.transform.position, layerMask);
        return r;
    }

    public void tryDraw() {
        updateCovers();

        // update covers depends on all text boxes being in their correct positions
        if (!src.activeSelf || isObscured()) hide();
        else if (isCovered) {
            isHidden = true;
            if (opacity < 0.25f) hide();
            else tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, opacity);
        } else show();

        Vector3 screenPoint = general.camera.WorldToScreenPoint(src.transform.position);
        if (screenPoint.z < 0) hide();

        Vector2 p;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiHelper.canvasRect, screenPoint, null, out p);

        tmpGo.transform.localPosition = p;
    }

    public void hide() {
        if (!tmp.enabled && isHidden) return;
        isHidden = true;

        tmp.enabled = false;
    }

    public void show() {
        if (tmp.color.a != opacity) tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, opacity);

        if (tmp.enabled && !isHidden) return;
        isHidden = false;

        tmp.enabled = true;
    }

    public void destroy() {
        GameObject.Destroy(tmpGo);
    }

    private void register() {
        allNames.Add(this);
        isInAllNames = true;
    }

    private void register(objectNameType type) {
        if (!isInAllNames) register();
        if (!namesByType.ContainsKey(type)) namesByType[type] = new List<objectName>();

        namesByType[type].Add(this);
    }

    private void register(string group) {
        if (!isInAllNames) register();
        if (!namesByGroup.ContainsKey(group)) namesByGroup[group] = new List<objectName>();

        namesByGroup[group].Add(this);
    }

    private void register(int priority) {
        if (!isInAllNames) register();
        if (!namesByPriority.ContainsKey(priority)) namesByPriority[priority] = new List<objectName>();

        namesByPriority[priority].Add(this);
    }

    /// <summary> Hide text that is hidden behind other text </summary>
    public static void updateCovers() {
        // dont over update as this is costly
        int frame = UnityEngine.Time.frameCount;
        if (frame == lastFrameUpdated) return;
        lastFrameUpdated = frame;

        // these names may or may not be covered
        List<objectName> toCheck = new List<objectName>(allNames.Count);
        foreach (objectName name in allNames) {
            // only check objectNames that are visible
            if (!name.isHidden || (name.isHidden && name.isCovered)) toCheck.Add(name);
        }

        // higher priority gets rendered above lower priority TODO: consider distance to camera as well?
        toCheck.Sort((a, b) => {
            if (a.priority == b.priority) return 0;
            if (a.priority < b.priority) return -1;
            return 1;
        });

        // TODO: i fucking give up fuck this shit i hate it so much it works barely so im not fucking touching this anymore
        List<string> seen = new List<string>();

        // iterate backwards because we remove values and dont want to have to offset index
        for (int i = toCheck.Count - 1; i >= 0; i--) {
            i = Math.Min(i, toCheck.Count - 1);
            if (i <= 0) break;

            objectName current = toCheck[i];
            if (seen.Contains(current.text)) continue;
            current.isCovered = false;
            Rect cr = uiHelper.rectToWorld(current.tmp.rectTransform);
            seen.Add(current.text);

            //toCheck.RemoveAt(i);
            for (int j = toCheck.Count - 1; j >= 0; j--) {
                objectName target = toCheck[j];

                if (seen.Contains(target.text)) continue;

                Rect tr = uiHelper.rectToWorld(target.tmp.rectTransform);
                // cant do this optimization because this operation needs to be deterministic, otherwise it causes flickering
                // maybe we can cache what it covers and then just re-set them? to simulate it running
                //if (target.isHidden && !target.isHiddenFromCovered) target.isCovered = false;
                if (cr.Overlaps(tr)) {
                    //toCheck.RemoveAt(j);
                    seen.Add(target.text);
                    target.isCovered = true;

                    // get area of which they intersect, this is used to control the opacity of the text
                    float x1 = Mathf.Min(tr.xMax, cr.xMax);
                    float x2 = Mathf.Max(tr.xMin, cr.xMin);
                    float y1 = Mathf.Min(tr.yMax, cr.yMax);
                    float y2 = Mathf.Max(tr.yMin, cr.yMin);
                    float area = (x1 - x2) * (y1 - y2);

                    float percent = 10f * (area / (cr.width * cr.height));
                    percent = Mathf.Min(percent, 1);
                    target.opacity = 1f - percent;
                } else {
                    target.isCovered = false;
                    target.opacity = 1;
                }
            }
        }
    }
}

public enum objectNameType {
    planet, satellite, facility, antenna
}
