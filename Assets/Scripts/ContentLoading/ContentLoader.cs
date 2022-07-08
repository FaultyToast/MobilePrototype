using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

public static class ContentLoader
{
    public static T[] LoadContent<T>(string folder, Type sourceClass) where T : UnityEngine.Object
    {
        Dictionary<string, T> namedObjects = new Dictionary<string, T>();
        T[] resourcesObjects = Resources.LoadAll<T>(folder);

        foreach (T asset in resourcesObjects)
        {
            namedObjects.Add(asset.name, asset);
        }

        List<T> objects = new List<T>();
        foreach (FieldInfo fieldInfo in sourceClass.GetFields())
        {
            if (fieldInfo.FieldType == typeof(T))
            {
                string name = fieldInfo.Name;
                T foundObject;
                namedObjects.TryGetValue(name, out foundObject);
                if (foundObject != null)
                {
                    fieldInfo.SetValue(null, foundObject);
                    objects.Add(foundObject);
                    if (foundObject is IInitializableObject)
                    {
                        (foundObject as IInitializableObject).Initialize();
                    }
                }
                else
                {
                    Debug.LogWarning("Could not find object in resources: " + fieldInfo.Name);
                }
            }
        }

        return objects.ToArray();
    }

    public static T[] LoadContentPassive<T>(string folder, Type sourceClass) where T : UnityEngine.Object
    {
        Dictionary<string, FieldInfo> namedFields = new Dictionary<string, FieldInfo>();
        T[] resourcesObjects = Resources.LoadAll<T>(folder);

        foreach (FieldInfo fieldInfo in sourceClass.GetFields())
        {
            if (fieldInfo.FieldType == typeof(T))
            {
                namedFields.Add(fieldInfo.Name, fieldInfo);
            }
        }

        foreach (T asset in resourcesObjects)
        {
            FieldInfo foundField;
            namedFields.TryGetValue(asset.name, out foundField);
            if (foundField != null)
            {
                foundField.SetValue(null, asset);
            }
        }

        return resourcesObjects;
    }

    public static T[] LoadContent<T>(string folder) where T : UnityEngine.Object
    {
        T[] resourcesObjects = Resources.LoadAll<T>(folder);
        return resourcesObjects;
    }

    public static T[] LoadContentWithID<T>(string folder, Type sourceClass, bool passive = false) where T : UnityEngine.Object, IAssetWithID
    {
        T[] resourcesObjects;
        if (passive)
        {
            resourcesObjects = LoadContentPassive<T>(folder, sourceClass);
        }
        else
        {
            resourcesObjects = LoadContent<T>(folder, sourceClass);
        }
        IDContent(resourcesObjects);
        return resourcesObjects;
    }

    public static T[] LoadContentWithID<T>(string folder) where T : UnityEngine.Object, IAssetWithID
    {
        T[] resourcesObjects = LoadContent<T>(folder);
        IDContent(resourcesObjects);
        return resourcesObjects;
    }

    public static void IDContent(IAssetWithID[] assets)
    {
        for (int i = 0; i < assets.Length; i++)
        {
            assets[i].assetID = i;
        }
    }
}
