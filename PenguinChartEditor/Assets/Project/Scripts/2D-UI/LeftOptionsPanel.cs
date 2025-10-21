using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class LeftOptionsPanel : MonoBehaviour
{
    PanelType currentPanel = PanelType.tempoMapMain;

    public List<PanelConnector> panels;
    public enum PanelType
    {
        tempoMapMain = 0,
        stemVolumeEditor = 1
    }

    public void SwitchPanel(PanelType newPanel)
    {
        if (newPanel == currentPanel) return;

        // change panel displayed
    }
}

[System.Serializable]
public struct PanelConnector
{
    public LeftOptionsPanel.PanelType panelType;
    public GameObject panelObject;
}