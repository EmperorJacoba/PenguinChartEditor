using UnityEngine;

public class SelectionTabSwitcher : MonoBehaviour
{
    [SerializeField] private LeftOptionsPanelTab noteTab;
    [SerializeField] private LeftOptionsPanelTab selectionTab;
    [SerializeField] private LeftOptionsPanel leftOptionsPanel;

    // can be transformed to happen on select/on deselect, but this method takes little resources and will work 100% of
    // the time, even when you forget to update this object
    private void Update()
    {
        var selectionEmpty = Chart.LoadedInstrument.IsNoteSelectionEmpty();
        if (noteTab.gameObject.activeInHierarchy != selectionEmpty)
            noteTab.gameObject.SetActive(selectionEmpty);

        if (selectionTab.gameObject.activeInHierarchy == selectionEmpty) 
            selectionTab.gameObject.SetActive(!selectionEmpty);

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