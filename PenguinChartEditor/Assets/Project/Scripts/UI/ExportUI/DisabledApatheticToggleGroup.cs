using System;
using UnityEngine;
using UnityEngine.UI;

// ToggleGroups LIE and say toggles aren't on when gameObject.activeInHierarchy = false. I really don't care for that. I detest it, in fact.
public class DisabledApatheticToggleGroup : MonoBehaviour
{
    private ToggleGroup underlyingGroup;

    [HideInInspector] public Toggle activeToggle;

    private void Awake()
    {
        underlyingGroup = GetComponent<ToggleGroup>();
    }

    private void Update()
    {
        activeToggle = underlyingGroup.GetFirstActiveToggle();
    }
}