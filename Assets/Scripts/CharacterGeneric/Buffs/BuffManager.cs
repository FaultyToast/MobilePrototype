using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
public static class BuffManager
{
    public static BuffDef[] buffs;
    public static BuffDef[] curses;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        List<BuffDef> buffList = new List<BuffDef>();
        List<BuffDef> curseList = new List<BuffDef>();

        FieldInfo[] fields = typeof(Buffs).GetFields();

        Dictionary<string, BuffDef> namedObjects = new Dictionary<string, BuffDef>();
        BuffDef[] resourcesObjects = Resources.LoadAll<BuffDef>("BuffDefs");
        foreach (BuffDef asset in resourcesObjects)
        {
            namedObjects.Add(asset.name, asset);
        }

        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].FieldType == typeof(BuffDef))
            {
                BuffDef buffDefToFill;
                string name = fields[i].Name;

                BuffDef foundBuffDef;
                namedObjects.TryGetValue(name, out foundBuffDef);
                if (foundBuffDef != null)
                {
                    buffDefToFill = foundBuffDef;
                }
                else
                {
                    buffDefToFill = ScriptableObject.CreateInstance<BuffDef>();
                    BuffInfoAttribute BuffInfoAttribute = fields[i].GetCustomAttribute<BuffInfoAttribute>();

                    if (BuffInfoAttribute != null)
                    {
                        buffDefToFill.stackable = BuffInfoAttribute.stackable;
                        buffDefToFill.defaultTime = BuffInfoAttribute.defaultTime;
                        buffDefToFill.buffType = BuffInfoAttribute.buffType;
                        buffDefToFill.description = BuffInfoAttribute.description;
                        buffDefToFill.name = name;
                    }
                }

                buffDefToFill.assetID = i;
                fields[i].SetValue(null, buffDefToFill);
                buffList.Add(buffDefToFill);

                if (buffDefToFill.buffType == BuffDef.BuffType.Curse)
                {
                    curseList.Add(buffDefToFill);
                }
            }
        }

        buffs = buffList.ToArray();
        curses = curseList.ToArray();

        DotManager.Initialize();
    }
    
    public static int[] GetEmptyBuffArray()
    {
        return new int[buffs.Length];
    }
}
