using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlsMenu : MonoBehaviour
{
    public GameObject mainPanel;

    public void BackToMain()
    {
        mainPanel.SetActive(true);
        gameObject.SetActive(false);
    }
}
