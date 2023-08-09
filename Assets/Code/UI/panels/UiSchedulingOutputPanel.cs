using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class UiSchedulingOutputPanel : MonoBehaviour
{
    // Start is called before the first frame update
    public RawImage output, key;
    private Texture2D outputimage, keyimage;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void generate(){
        outputimage = new Texture2D(550, 280);
        Color background = new Color(.702f, .706f, .761f, 1f);

        for(int i = 0; i< 550; i++){
            for(int j=0; j<280; j++){
                outputimage.SetPixel(i, j, background);
            }
        }
        outputimage.Apply();
        for(int i=0; i<80; i++){
            for(int j=0; j<200; i++){
                keyimage.SetPixel(i,j, background);
            }
        }

        output.texture=outputimage;

    }
}
