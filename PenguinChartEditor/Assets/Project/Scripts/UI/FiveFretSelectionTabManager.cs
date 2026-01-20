using UnityEngine;

public class FiveFretSelectionTabManager : MonoBehaviour
{
    [SerializeField] private LeftOptionsPanelTab noteTab;
    [SerializeField] private LeftOptionsPanelTab selectionTab;
    [SerializeField] private LeftOptionsPanel leftOptionsPanel;

    private void Update()
    {
        var selectionEmpty = Chart.GetActiveInstrument<FiveFretInstrument>().IsNoteSelectionEmpty();
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