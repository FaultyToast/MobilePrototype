using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMaterials : MonoBehaviour
{
    [System.NonSerialized] public List<Material> characterMaterials;
    public List<SkinnedMeshRenderer> skinnedMaterialRenderers;

    public void Awake()
    {
        characterMaterials = new List<Material>();
        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedMaterialRenderers)
        {
            for (int i = 0; i < skinnedRenderer.materials.Length; i++)
            {
                characterMaterials.Add(skinnedRenderer.materials[i]);
            }
        }
    }
}
