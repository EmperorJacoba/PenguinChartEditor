using UnityEngine;

public class FiveFretSelectionTabManager : MonoBehaviour
{
    [SerializeField] LeftOptionsPanelTab noteTab;
    [SerializeField] LeftOptionsPanelTab selectionTab;
    [SerializeField] LeftOptionsPanel leftOptionsPanel;

    void Update()
    {
        var selectionEmpty = Chart.GetActiveInstrument<FiveFretInstrument>().IsSelectionEmpty();
        if (noteTab.gameObject.activeInHierarchy != selectionEmpty)
            noteTab.gameObject.SetActive(selectionEmpty);

        if (selectionTab.gameObject.activeInHierarchy == selectionEmpty) 
            selectionTab.gameObject.SetActive(!selectionEmpty);

        var targetPanel = selectionTab ? LeftOptionsPanel.PanelType.noteOptionsFiveFret : LeftOptionsPanel.PanelType.noteSelectionFiveFret;
        if (leftOptionsPanel.currentPanel.panelType == LeftOptionsPanel.PanelType.noteOptionsFiveFret && !selectionEmpty)
        {
            leftOptionsPanel.SwitchPanel(LeftOptionsPanel.PanelType.noteSelectionFiveFret);
        }
        if (leftOptionsPanel.currentPanel.panelType == LeftOptionsPanel.PanelType.noteSelectionFiveFret && selectionEmpty)
        {
            leftOptionsPanel.SwitchPanel(LeftOptionsPanel.PanelType.noteOptionsFiveFret);
        }
    }
}