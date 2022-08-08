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
    public List<GameObject> stages;

    private int index;

    public void Start() {
        foreach (GameObject go in stages) go.SetActive(false);
        StartCoroutine(start());
    }

    private IEnumerator start() {
        while (true) {
            if (index - 1 < 0) stages.Last().SetActive(false);
            else stages[index - 1].SetActive(false);
            
            stages[index].SetActive(true);
            
            index++;
            if (index == stages.Count) index = 0;
            yield return new WaitForSeconds(0.25f);
        }
    }
}
