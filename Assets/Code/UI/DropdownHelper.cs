using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
public class DropdownHelper : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    int dropdownvalue;

    string m_Message;

    int planct;

    int satct;

    int facct;

    public body refframset;


    public void Start() {
        dropdown.onValueChanged.AddListener(valueChangeCallback);
    }

    public void initdropdown() {
        dropdown = GetComponent<TMP_Dropdown>();
        ////Clear the old options of the Dropdown menu
        dropdown.ClearOptions();
        ////Add the options created in the List above
        List<string> dropOptions = master.allSatellites.Select(x => x.name).ToList();
        dropOptions.AddRange(master.allPlanets.Select(x => x.name));
        //dropOptions.AddRange(master.allFacilites.Select(x => x.name));
        dropdown.AddOptions(dropOptions);
        
        Debug.Log("check1");
    }
    
    private void valueChangeCallback(int value) {
        
        Debug.Log("Iran");
        dropdownvalue=dropdown.value;
        m_Message = dropdown.options[dropdownvalue].text;
        //Debug.Log(m_Message);
        for (int i = 0; i < master.allPlanets.Count; i++) {
            if(master.allPlanets[i].name==m_Message){
                refframset=master.allPlanets[i];
            }
            else{
                for (int v = 0; v < master.allSatellites.Count; v++){
                    if(master.allSatellites[v].name == m_Message){
                        refframset=master.allSatellites[v];
                    }
                    else{
                        for (int c = 0; c < master.allFacilites.Count; c++){
                            if(master.allFacilites[c].name == m_Message){
                                //refframset=master.allFacilites[c];
                        }
                        }

                }

            }

                }

            }

           
           master.setReferenceFrame(refframset);
        }
        
    }


