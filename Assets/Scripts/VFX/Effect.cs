using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect : MonoBehaviour, IAssetWithID
{
    public int assetID { get; set; }

    public EffectData effectData;
}
