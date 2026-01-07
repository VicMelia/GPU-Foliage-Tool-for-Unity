using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSManager : MonoBehaviour
{

    public TextMeshProUGUI fpsText;

    float frequencyTime = 1f; //time that the fps will be updated
    float time;

    float totalTime;
    int frameCount;
    public int FPStarget = 120;

    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FPStarget;
    }

    // Update is called once per frame
    void Update()
    {

        time += Time.unscaledDeltaTime;
        totalTime += Time.unscaledDeltaTime;
        frameCount++;

        if(time >= frequencyTime) 
        {
            int frameRate = (int)(frameCount / time);
            //if(totalTime > 20f && frameRate <= 5) Application.Quit();
            fpsText.text = "FPS: " + frameRate.ToString();
            time -= frequencyTime;
            frameCount = 0;
        }

        
    }
}
