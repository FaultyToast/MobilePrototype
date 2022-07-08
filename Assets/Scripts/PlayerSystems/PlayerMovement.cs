using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : CharacterMovement
{
    [Header("Player Movement Variables")]
    public float doubleJumpForce = 50f;

    private bool hasDoubleJumped = false;

    protected override void JumpFailed()
    {
        if (!hasDoubleJumped && canMove && jumpingAllowed)
        {
            hasDoubleJumped = true;
            Jump(doubleJumpForce, "Flip");
        }
    }

    protected override void OnLanded()
    {
        base.OnLanded();
        hasDoubleJumped = false;
    }

    protected override void Update()
    {
        if (isLocalPlayer)
        {
            base.Update();
        }
    }
}
