using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationTrigger : MonoBehaviour
{
    public CharacterMaster characterMaster;

    Transform rupturePoint;
    Transform comicPoint;
    Transform bladePoint;
    Transform spatialPoint;

    private void Start()
    {
        rupturePoint = characterMaster.childLocator.GetChild("BackUIPointRupture");
        comicPoint = characterMaster.childLocator.GetChild("BackUIPointCosmic");
        bladePoint = characterMaster.childLocator.GetChild("BladePoint");
    }

    public enum BuffType
    {
        TakeCosmicBlade,
        DestroyCosmicBlades,
        RuptureFire,
        TriggerSpatialSlash,
    }

    public void AnimationTrigger(BuffType buff) 
    {
        //switch(buff)
        //{
        //    case default:
        //        
        //        break;
        //}
    }

}
