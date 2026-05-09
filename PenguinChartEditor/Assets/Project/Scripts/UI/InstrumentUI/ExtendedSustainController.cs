using UnityEngine;
using UnityEngine.UI;

public class ExtendedSustainController : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(UpdateExtendedSustain);
        toggle.isOn = Chart.settings.ExtSustains;
    }

    private void UpdateExtendedSustain(bool mode) => Chart.settings.ExtSustains = mode;
    public void SetExtendedSustains(bool mode)
    {
        Chart.settings.ExtSustains = mode;
        toggle.isOn = mode;
    }
}