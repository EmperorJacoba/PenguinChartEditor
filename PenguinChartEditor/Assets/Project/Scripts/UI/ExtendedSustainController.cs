using UnityEngine;
using UnityEngine.UI;

public class ExtendedSustainController : MonoBehaviour
{
    [SerializeField] Toggle toggle;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(UpdateExtendedSustain);
        toggle.isOn = UserSettings.ExtSustains;
    }

    void UpdateExtendedSustain(bool mode) => UserSettings.ExtSustains = mode;
}