using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    private RawImage overlay;
    private GameObject tmpGo, src, overlayGo, holder;
    private string text, group;
    private objectNameType type;
    private int priority = 0; // higher is better
    private float opacity = 1, opacityTarget = 1, maxOpacity = 1;
    private bool isCovered = false;
    private float cachedSortingDistance;
    
    public bool isHidden {get; private set;}
    public bool isOpacityChanging {get => opacityTarget == opacity;}

    public objectName(GameObject src, objectNameType type, string text, string group = "unsorted") {
        this.text = text;
        this.type = type;
        this.group = group;
        this.src = src;

        holder = resLoader.createPrefab("empty", GameObject.FindGameObjectWithTag("ui/bodyName").transform);
        holder.name = text + " bodyName";

        // group is just used for sorting, type actually determines what the text looks like
        tmpGo = resLoader.createPrefab("bodyName");
        tmpGo.transform.SetParent(holder.transform, false);
        tmpGo.name = text + " bodyName";
        tmp = tmpGo.GetComponent<TextMeshProUGUI>();

        tmp.text = text;

        // now get what it should look like
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.rectTransform.pivot = new Vector2(-0.2f, 0.5f);
        string overlayPath = "";
        switch (type) {
            case objectNameType.planet:
                priority = 30;
                tmp.fontSize = 25;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold | FontStyles.Italic;
                overlayPath = "Prefabs/ui/overlayCircle";
                maxOpacity = 0.9f;
                break;
            case objectNameType.moon:
                priority = 25;
                tmp.fontSize = 25;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold | FontStyles.Italic;
                overlayPath = "Prefabs/ui/overlayCircle";
                maxOpacity = 0.9f;
                break;
            case objectNameType.satellite:
                priority = 20;
                tmp.fontSize = 20;
                overlayPath = "Prefabs/ui/overlayHexagon";
                maxOpacity = 0.75f;
                break;
            case objectNameType.antenna:
                priority = 0;
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
                overlayPath = "Prefabs/ui/overlaySquare";
                maxOpacity = 0.75f;
                break;
            case objectNameType.facility:
                priority = 10;
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
                overlayPath = "Prefabs/ui/overlaySquare";
                maxOpacity = 0.75f;
                break;
        }

        overlayGo = GameObject.Instantiate(Resources.Load<GameObject>(overlayPath), Vector3.zero, Quaternion.identity, holder.transform);
        overlay = overlayGo.GetComponent<RawImage>();
        overlay.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        overlay.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        overlay.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        overlay.rectTransform.anchoredPosition = new Vector2(0, 0);
        if (trailRenderer.trailColors.ContainsKey(text)) {
            overlay.color = trailRenderer.trailColors[text];
        }

        tmp.autoSizeTextContainer = true;
        tmp.rectTransform.sizeDelta = tmp.GetPreferredValues();

        register(priority);
        register(type);
        register(group);

        opacity = maxOpacity;
    }

    /// <summary> Check if text is hidden by another physical object </summary>
    public bool isObscured(int layerMask = (1 << 6) | (1 << 7)) {
        if (type == objectNameType.planet || type == objectNameType.moon) return false;
        bool r = Physics.Linecast(general.camera.transform.position, src.transform.position, layerMask);
        return r;
    }

    public void tryDraw() {
        updateCovers();

        // lerp opacity to opacityTarget
        if (opacityTarget != opacity) {
            if (Math.Abs(opacityTarget - opacity) < 0.01f) opacity = opacityTarget;
            else {
                float change = (opacityTarget - opacity) * 30f * UnityEngine.Time.deltaTime;
                opacity += change;
            }
        }

        // update covers depends on all text boxes being in their correct positions
        if (isCovered || isObscured()) {
            isHidden = true;
            opacityTarget = 0;
            if (opacity < 0.01f) hide();
        } else show();

        if (tmp.color.a != opacity) {
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, opacity);
            overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, opacity);
        }

        Vector3 screenPoint = general.camera.WorldToScreenPoint(src.transform.position);
        if (!isOnScreen(screenPoint)) hide();

        // unfortunately we have to keep names synced always
        Vector2 p;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiHelper.canvasRect, screenPoint, null, out p);

        tmpGo.transform.localPosition = p;
        overlayGo.transform.localPosition = p;

        cachedSortingDistance = Vector3.Distance(general.camera.transform.position, src.transform.position);
    }

    public void hide() {
        if (!tmp.enabled && isHidden) return;
        isHidden = true;

        tmpGo.SetActive(false);
        overlayGo.SetActive(false);
        tmp.enabled = false;
    }

    public void show() {
        if (tmp.enabled && !isHidden) return;
        opacityTarget = 1;
        isHidden = false;

        tmpGo.SetActive(true);
        overlayGo.SetActive(true);
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

        // higher priority gets rendered above lower priority
        toCheck.Sort((a, b) => {
            if (a.priority == b.priority) {
                if (a.text == master.requestReferenceFrame().name) return 1;
                if (b.text == master.requestReferenceFrame().name) return -1;

                float am = a.cachedSortingDistance;
                float bm = b.cachedSortingDistance;
                if (am == bm) return 0;
                if (am < bm) return 1;
                return -1;
            }
            if (a.priority < b.priority) return -1;
            return 1;
        });

        // i dislike this part. a lot
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
                if (cr.Overlaps(tr)) {
                    //toCheck.RemoveAt(j);
                    seen.Add(target.text);
                    target.isCovered = true;

                    target.opacityTarget = 0;
                } else {
                    target.isCovered = false;
                    target.opacityTarget = target.maxOpacity;
                }
            }
        }
    }

    private bool isOnScreen(Vector3 v) {
        if (v.z <= 0) return false;

        if (v.x < 0 || v.y < 0) return false;
        if (v.x > Screen.width || v.y > Screen.height) return false;

        return true;
    }
}

public enum objectNameType {
    planet, moon, satellite, facility, antenna
}
