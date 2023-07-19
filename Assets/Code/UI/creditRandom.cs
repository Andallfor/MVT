using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class creditRandom : MonoBehaviour
{
    public TextMeshProUGUI t;
    private string[] names = new string[5] {"Aditya Dutt", "Aman Garg", "Arya Kazemnia", "Leo Wang", "Zoe Schoeneman-Frye"};

    public void randomize() {
        for (int i = 0; i < names.Length; i++) {
            string temp = names[i];
            int randomIndex = Random.Range(i, names.Length);
            names[i] = names[randomIndex];
            names[randomIndex] = temp;
        }

        t.text = $"{names[0]}, {names[1]}, {names[2]}, {names[3]}, and {names[4]}";
    }
}
