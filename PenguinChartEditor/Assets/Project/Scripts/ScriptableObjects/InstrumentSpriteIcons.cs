using Penguin.InstrumentIconMatchup;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class InstrumentSpriteIcons : ScriptableObject
{
    [SerializeField] List<InstrumentIcon<Sprite>> icons;

    public Sprite GetInstrumentIcon(HeaderType instrumentID)
    {
        var instrumentType = InstrumentMetadata.GetInstrumentType(instrumentID);
        return icons.Where(x => x.instrumentID == instrumentType).Select(x => x.icon).First();
    }
}