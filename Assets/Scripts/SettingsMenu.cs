using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public GameObject mainPanel;

    Resolution[] resolutions;
    void Start()
    {
        /*
        resolutions = Screen.resolutions;

        HashSet<string> resolutionSet = new HashSet<string>();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        resolutionDropdown.ClearOptions();

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;

            if (resolutionSet.Add(option))
            {
                options.Add(option);

                if (resolutions[i].width == Screen.width &&
                    resolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = options.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);

        resolutionDropdown.onValueChanged.RemoveAllListeners();

        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        */
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    public void SetFullscreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }

    /*
    public void SetResolution(int index)
    {
        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
    */

    public void BackToMain()
    {
        mainPanel.SetActive(true);
        gameObject.SetActive(false);
    }
}
