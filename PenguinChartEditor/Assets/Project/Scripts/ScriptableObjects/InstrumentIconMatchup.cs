using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class InstrumentIcons : ScriptableObject
{
    [SerializeField] List<Penguin.InstrumentIconMatchup.InstrumentIcon> icons;

    public Material GetInstrumentIcon(HeaderType instrumentID)
    {
        var instrumentType = InstrumentMetadata.GetInstrumentType(instrumentID);
        return icons.Where(x => x.instrumentID == instrumentType).Select(x => x.icon).First();
    }
}

namespace Penguin.InstrumentIconMatchup
{
    [System.Serializable]
    public struct InstrumentIcon
    {
        public InstrumentType instrumentID;
        public Material icon;
    }
}

