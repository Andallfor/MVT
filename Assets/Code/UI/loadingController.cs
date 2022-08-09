using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class loadingController : MonoBehaviour
{
    public Slider progressBar;
    public TextMeshProUGUI info, loading;
    public static loadingController self;
    private Dictionary<float, string> stages;
    private float percent;

    void Awake() {
        self = this;
        end();
    }

    public static void start(Dictionary<float, string> stages) {
        self.GetComponent<RawImage>().enabled = true;
        for (int i = 0; i < self.transform.childCount; i++) self.transform.GetChild(i).gameObject.SetActive(true);
        self.gameObject.GetComponent<Canvas>().overrideSorting = false; // doesnt work unless we do this
        self.gameObject.GetComponent<Canvas>().overrideSorting = true;
        self.progressBar.value = 0;
        self.percent = 0;
        self.stages = stages;
        self.info.text = stages[stages.Min(x => x.Key)];
    }

    public static void addPercent(float percent) {
        self.percent += percent;
        if (self.percent > 1) end();
        else {
            self.progressBar.value = self.percent;
            float closest = 100;
            foreach (float key in self.stages.Keys) {
                if (key > self.percent) continue;
                if (Mathf.Abs(self.percent) - key < Mathf.Abs(self.percent - closest)) closest = key;
            }

            if (closest == 100) self.info.text = "Finalizing...";
            else self.info.text = self.stages[closest];
        }
    }

    public static void end() {
        self.GetComponent<RawImage>().enabled = false;
        for (int i = 0; i < self.transform.childCount; i++) self.transform.GetChild(i).gameObject.SetActive(false);
    }
}
