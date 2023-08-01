using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class objectName {
    public static List<objectName> allNames = new List<objectName>();
    public static Dictionary<objectNameType, List<objectName>> namesByType = new Dictionary<objectNameType, List<objectName>>();
    public static Dictionary<string, List<objectName>> namesByGroup = new Dictionary<string, List<objectName>>() {
        {"", new List<objectName>()}
    };

    private bool isInAllNames = false;
    private TextMeshProUGUI tmp;
    private GameObject tmpGo, src;
    private string text, group;
    private objectNameType type;
    
    public bool isHidden {get; private set;}

    public objectName(GameObject src, objectNameType type, string text, string group = "unsorted") {
        this.text = text;
        this.type = type;
        this.group = group;
        this.src = src;

        register(type);
        register(group);

        // group is just used for sorting, type actually determines what the text looks like
        tmpGo = resLoader.createPrefab("bodyName");
        tmpGo.transform.SetParent(GameObject.FindGameObjectWithTag("ui/bodyName").transform, false);
        tmp = tmpGo.GetComponent<TextMeshProUGUI>();

        tmp.text = text;
        tmp.autoSizeTextContainer = true;

        // now get what it should look like
        switch (type) {
            case objectNameType.planet:
                tmp.fontSize = 25;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold | FontStyles.Italic;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.rectTransform.pivot = new Vector2(-0.05f, 0.5f);
                break;
            case objectNameType.satellite:
                tmp.fontSize = 20;
                break;
            case objectNameType.antenna:
            case objectNameType.facility:
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
                break;
        }
    }

    /// <summary> Check if text is hidden by another physical object </summary>
    public bool isObscured(int layerMask = (1 << 6) | (1 << 7)) {
        if (type == objectNameType.planet) return false;
        bool r = Physics.Linecast(general.camera.transform.position, src.transform.position, layerMask);
        return r;
    }

    public void tryDraw() {
        if (master.requestReferenceFrame().name == text || !src.activeSelf || isObscured()) {
            hide();
            return;
        }
        show();

        Vector3 screenPoint = general.camera.WorldToScreenPoint(src.transform.position);
        if (screenPoint.z < 0) {
            hide();
            return;
        }

        Vector2 p;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiHelper.canvasRect, screenPoint, null, out p);

        tmpGo.transform.localPosition = p;
    }

    public void hide() {
        if (isHidden) return;
        isHidden = true;

        tmp.enabled = false;
    }

    public void show() {
        if (!isHidden) return;
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
}

public enum objectNameType {
    planet, satellite, facility, antenna
}
