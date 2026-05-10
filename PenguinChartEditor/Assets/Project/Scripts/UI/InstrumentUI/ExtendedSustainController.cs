using UnityEngine;
using UnityEngine.UI;

public class ExtendedSustainController : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(UpdateExtendedSustain);
        toggle.isOn = Chart.settings.ExtendedSustains;
    }

    private void UpdateExtendedSustain(bool mode) => Chart.settings.ExtendedSustains = mode;
    public void SetExtendedSustains(bool mode)
    {
        Chart.settings.ExtendedSustains = mode;
        toggle.isOn = mode;
    }
}