using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
     
public class uiButtonPressDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {     
    public bool buttonPressed;
    public Button button;
     
    public void OnPointerDown(PointerEventData eventData){
        if (button.interactable) buttonPressed = true;
    }
     
    public void OnPointerUp(PointerEventData eventData){
        buttonPressed = false;
    }
}
