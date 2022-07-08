using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildLocator : MonoBehaviour
{
    public TransformKeyPair[] transformKeyPairs;
    private Dictionary<string, Transform> children;

    public void Awake()
    {
        children = new Dictionary<string, Transform>();
        for(int i = 0; i < transformKeyPairs.Length; i++)
        {
            children.Add(transformKeyPairs[i].key, transformKeyPairs[i].transform);
        }
    }

    public Transform GetChild(string name)
    {
        Transform newTransform;
        children.TryGetValue(name, out newTransform);
        return newTransform;
    }

    [System.Serializable]
    public class TransformKeyPair
    {
        public string key;
        public Transform transform; 
    }
}
