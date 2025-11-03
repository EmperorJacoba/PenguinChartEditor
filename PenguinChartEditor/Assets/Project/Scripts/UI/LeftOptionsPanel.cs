using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class LeftOptionsPanel : MonoBehaviour
{
    PanelConnector currentPanel;

    public List<PanelConnector> panels;
    public enum PanelType
    {
        tempoMapMain = 0,
        stemVolumeEditor = 1
    }

    void Awake()
    {
        currentPanel = panels[0];
    }

    public bool SwitchPanel(PanelType newPanel)
    {
        if (newPanel == currentPanel.panelType) return false;

        var newPanelConnector = panels.Where(item => item.panelType == newPanel).ToList()[0];
        var oldPanelConnector = panels[panels.IndexOf(currentPanel)];

        oldPanelConnector.panelObject.SetActive(false);
        newPanelConnector.panelObject.SetActive(true);

        newPanelConnector.correspondingTab.SwitchToActive();
        oldPanelConnector.correspondingTab.SwitchToInactive();
        currentPanel = newPanelConnector;
        return true;
    }
}

[System.Serializable]
public struct PanelConnector
{
    public LeftOptionsPanel.PanelType panelType;
    public GameObject panelObject;
    public LeftOptionsPanelTab correspondingTab;
}