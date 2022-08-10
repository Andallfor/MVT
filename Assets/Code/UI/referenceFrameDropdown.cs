using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class referenceFrameDropdown : MonoBehaviour, IPointerDownHandler
{
    public uiNavPanel panel;
    public void OnPointerDown(PointerEventData data) {
        panel.generatePossibleReferenceFrames();
    }
}
