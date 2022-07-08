using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;

public static class DotManager
{
    public static DotDef[] dotDefs;

    public static void Initialize()
    {
        List<DotDef> dotList = new List<DotDef>();

        FieldInfo[] fields = typeof(DotDefs).GetFields();

        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].FieldType == typeof(DotDef))
            {
                DotDef dotDef = (DotDef)fields[i].GetValue(null);
                dotDef.assetID = i;
                if (dotDef.associatedBuff != null)
                {
                    dotDef.associatedBuff.associatedDot = dotDef;
                }

                dotList.Add(dotDef);
            }
        }

        dotDefs = dotList.ToArray();
    }

}

public static class DotDefs
{
    public static DotDef Burning = new DotDef(3f, 0.3f, Buffs.Burning, DamageType.Generic, DamageNumberColorFlags.Burning);
    public static DotDef Poison = new DotDef(3f, 0.6f, Buffs.Poison, DamageType.Generic, DamageNumberColorFlags.Poison, 0.003f);
}

public class DotDef : object, IAssetWithID
{
    public DotDef(float damagePerSecond, float tickInterval, BuffDef associatedBuff, DamageType damageType = DamageType.None, DamageNumberColorFlags color = DamageNumberColorFlags.None, float minPercent = 0)
    {
        this.damagePerSecond = damagePerSecond;
        this.tickInterval = tickInterval;
        this.associatedBuff = associatedBuff;
        this.color = color;
        this.damageType = damageType;
        this.percent = minPercent;
    }

    public float percent = 0;
    public float damagePerSecond;
    public float tickInterval;
    public BuffDef associatedBuff = null;
    public DamageType damageType = DamageType.None;
    public DamageNumberColorFlags color = DamageNumberColorFlags.None;
    public int assetID { get; set; }
}