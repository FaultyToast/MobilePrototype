using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/DisableDeath", order = 1)]
public class DisableDeath : EffectDeath
{
    public override void OnExit()
    {
        outer.gameObject.SetActive(false);
        foreach (Material mat in outer.characterMaster.characterMaterials)
        {
            mat.SetFloat("_Dissolve", 0);
        }
    }
}
