using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class GenericDeath : ActionState
{
   
    // Start is called before the first frame update
    public override void OnEnter()
    {
        base.OnEnter();
        Destroy(outer.gameObject);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        ExitState();
    }

    public override void OnExit()
    {
        base.OnExit();
        
    }
}
