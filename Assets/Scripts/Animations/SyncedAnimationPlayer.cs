using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SyncedAnimationPlayer : NetworkBehaviour
{
    private Animator animator;

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayAnimation(string animToPlay, float transitionLength, string layer, float offset)
    {
        if (animator == null)
        {
            return;
        }
        PlayAnimation(animToPlay, transitionLength, animator.GetLayerIndex(layer), offset);
    }

    // Plays animations
    public void PlayAnimation(string animToPlay, float transitionLength, int layer, float offset)
    {
        if (animator == null)
        {
            return;
        }

        //transitions between curently playing anims
        if (layer >= 0)
        {
            animator.CrossFade(animToPlay, transitionLength, layer, offset);
            CmdPlayAnimationSynced(animToPlay, transitionLength, layer, offset);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPlayAnimationSynced(string animtoplay, float transitionlength, int layer, float offset)
    {
        RpcPlayAnimationSynced(animtoplay, transitionlength, layer, offset);
    }

    [ClientRpc]
    public void RpcPlayAnimationSynced(string animtoplay, float transitionlength, int layer, float offset)
    {
        if (!isLocalPlayer)
        {
            animator.CrossFade(animtoplay, transitionlength, layer, offset);
        }
    }
}
