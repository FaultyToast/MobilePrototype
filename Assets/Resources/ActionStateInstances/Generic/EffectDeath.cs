using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System;
using Mirror;

[CreateAssetMenu(fileName = "EffectDeath", menuName = "ActionStates/EffectDeath", order = 1)]
public class EffectDeath : ActionState
{
    public float dissolveTime;
    public float particleTime;
    public Effect deathEffectPrefab;
    float timer;
    bool spawnParts = false;
    public bool disableBody = true;
    // Start is called before the first frame update
    public override void OnEnter()
    {
        base.OnEnter();
        timer = 0;
        isOccupied = true;
        characterMovement.velocity.x = 0;
        characterMovement.velocity.z = 0;
        PlayAnimationCrossFade("Body","Death", 0.25f, true);

        if (disableBody)
        {
            outer.GetComponent<HitboxGroup>().enabled = false;
            outer.GetComponent<KinematicCharacterController.KinematicCharacterMotor>().enabled = false;
            outer.GetComponent<Collider>().enabled = false;
        }
    }

    public override void Update()
    {
        base.Update();
        timer += Time.deltaTime;
        timer = Mathf.Min(timer, dissolveTime);
        foreach (Material mat in outer.characterMaster.characterMaterials)
        {
            mat.SetFloat("_Dissolve", Mathf.InverseLerp(0, dissolveTime, timer));
        }

        if(fixedAge > particleTime && !spawnParts)
        {
            SpawnEffect();
            spawnParts = true;
        }

        if (fixedAge > dissolveTime)
        {
            ExitState();
        }
    }

    public void SpawnEffect()
    {
        //foreach (SkinnedMeshRenderer skinnedMesh in outer.characterMaster.skinnedMaterialRenderers)
        //{
        //    Transform transform = skinnedMesh.rootBone;
        //    if (transform != null)
        //    {
        //        GameObject effect;
        //
        //        effect = EffectManager.CreateEffect(deathEffectPrefab, outer.characterMaster.modelPivot.position, null, false);
        //
        //        VisualEffect deathEffect = effect.GetComponent<VisualEffect>();
        //
        //        deathEffect.SetMesh("ModelMesh", skinnedMesh.sharedMesh);
        //        deathEffect.SetVector3("RootPosition", transform.position);
        //        deathEffect.SetVector3("RootAngles", transform.eulerAngles);
        //        deathEffect.SetVector3("RootScale", transform.localScale);
        //    }
        //}

        //This kills performance
        //Transform[] bones = outer.characterMaster.skinnedMaterialRenderers[0].bones;
        //
        //for (int i = 0; i < bones.Length - 1; i++)
        //{
        //    for (int j = 0; j < bones[i].childCount; j++)
        //    {
        //        if (bones[i].GetChild(j))
        //        {
        //            GameObject effect;
        //            if (isAuthority)
        //            {
        //                effect = EffectManager.CreateSimpleEffect(deathEffectPrefab, outer.characterMaster.modelPivot.position, null, false);
        //            }
        //            else
        //            {
        //                effect = EffectManager.CreateSimpleEffect(deathEffectPrefab, outer.characterMaster.modelPivot.position, null, false);
        //            }
        //
        //            VisualEffect deathEffect = effect.GetComponent<VisualEffect>();
        //
        //            deathEffect.SetVector3("LineStart", bones[i].position);
        //            deathEffect.SetVector3("LineEnd", bones[i].GetChild(j).position);
        //        }
        //    }
        //}
    }

    public override void OnExit()
    {
        base.OnExit();
        Destroy(outer.gameObject);
    }
}
