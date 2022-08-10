using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class uiInfoPanel : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public TextMeshProUGUI bodyText, headerText;

    private List<TMP_Dropdown.OptionData> bodyOptions = new List<TMP_Dropdown.OptionData>();

    public void Awake() {
        master.onFinalSetup += ((sender, e) => {
            List<string> names = new List<string>();
            foreach (planet p in master.allPlanets) names.Add(p.name);
            foreach (satellite s in master.allSatellites) names.Add(s.name);

            names.Sort();

            foreach (string name in names) bodyOptions.Add(new TMP_Dropdown.OptionData(name));

            dropdown.options = bodyOptions;

            viewpoint();
        });

        dropdown.onValueChanged.AddListener(select);
    }

    public void mvt() {
        dropdown.gameObject.SetActive(false);
        headerText.text = "About: Mission Visualization Toolkit (MVT)";
        bodyText.text = "Missions Visualization Toolkit (MVT) is a lightweight web tool and application version of the Systems Toolkit (STK) expediting the testing of various �What-If� or hypothetical Lunar network scenarios, specifically the NASA Requirements Analysis Cycle (RAC) studies, impacting Artemis missions 3, 4, 5, and 8. ";
    }

    public void scenario() {
        dropdown.gameObject.SetActive(false);
        headerText.text = "About: Artemis Missions";
        bodyText.text = "The Artemis Missions are America's attempt at putting people nack on the moon, as well landing the first woman on the moon. They will all land near the lunar south pole where there is ice and experiments will be conducted.";
    }

    public void viewpoint() {select(master.requestReferenceFrame().name);}

    public void bodies() {select(tidbits.bodyInfo.Keys.ToList()[Random.Range(0, tidbits.bodyInfo.Count)]);}

    private void select(string name) {
        dropdown.gameObject.SetActive(true);
        dropdown.value = bodyOptions.IndexOf(bodyOptions.First(x => x.text == name));
        headerText.text = "About:";
        bodyText.text = tidbits.bodyInfo[name];
    }

    private void select(int index) {select(bodyOptions[index].text);}
}
