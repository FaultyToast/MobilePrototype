using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickerHitboxGroup : MonoBehaviour
{
    // Start is called before the first frame update
    private HitboxGroup hitboxGroup;
    public float flickerInterval = 0.3f;
    void Start()
    {
        hitboxGroup = GetComponent<HitboxGroup>();
        StartCoroutine(Flicker());
    }

    public IEnumerator Flicker()
    {
        while (true)
        {
            hitboxGroup.enabled = false;
            yield return new WaitForSeconds(flickerInterval * 0.5f);
            hitboxGroup.enabled = true;
            yield return new WaitForSeconds(flickerInterval * 0.5f);
        }
    }

}
