using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

[CreateAssetMenu(fileName = "NewBuffDef", menuName = "FracturedAssets/BuffDef", order = 1)]
public class BuffDef : ScriptableObject, IAssetWithID
{
    public BuffDef()
    {
        
    }
    public BuffDef(bool stackable)
    {
        this.stackable = stackable;
    }

    public bool stackable = true;
    [System.NonSerialized] public UnityEvent<CharacterMaster> onBuffChanged = new UnityEvent<CharacterMaster>();
    [System.NonSerialized] public DotDef associatedDot;

    public int assetID { get; set; }
 
    public float defaultTime = 1f;

    public BuffType buffType = BuffType.Generic;
    public bool refreshAllTimersOnAddition = false;

    public GameObject buffEffect;

    public string description;

    public float[] genericFloats;
    [TextArea]
    public string genericFloatsDescription;

    public enum BuffType
    {
        Generic,
        Buff,
        Debuff,
        Curse
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class BuffInfoAttribute : Attribute {
    public BuffDef.BuffType buffType;
    public bool stackable = true;
    public float defaultTime;
    public string description;
}
