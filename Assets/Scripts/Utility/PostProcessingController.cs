using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingController : MonoBehaviour
{
    public Volume volume;
    public Vignette vignette;
    public ColorAdjustments colorAdjustment;

    bool flicker = false;

    public float maxLowHpDesaturation = -50f;

    // Start is called before the first frame update
    void Start()
    {
        volume = GetComponent<Volume>();
        volume.profile.TryGet<Vignette>(out vignette);
        volume.profile.TryGet<ColorAdjustments>(out colorAdjustment);
    }

    public void DamageVignette()
    {
        flicker = true;
        StartCoroutine(VignetteFlicker(0, 0.2f, 0.15f));
    }

    IEnumerator VignetteFlicker(float a, float b, float totalTime)
    {
        float timer = 0f;
        while (timer < totalTime)
        {
            float lerp = Mathf.Lerp(a, b, timer / totalTime);
            vignette.intensity.value = lerp;
            timer += Time.deltaTime;
            yield return null;
        }

        if(flicker == true)
        {
            flicker = false;
            StartCoroutine(VignetteFlicker(b, 0, totalTime));
            yield return null;
        }
    }

    private void Update()
    {
        if (GameManager.instance.localPlayerCharacter == null)
        {
            return;
        }
        float lerp = Mathf.InverseLerp((GameManager.instance.localPlayerCharacter.characterHealth.maxHealth) * 0.2f, (GameManager.instance.localPlayerCharacter.characterHealth.maxHealth) * 0.3f, GameManager.instance.localPlayerCharacter.characterHealth.health);
        float value = Mathf.Lerp(-50, 0, lerp);

        if(colorAdjustment != null)
        {
            colorAdjustment.saturation.value = Mathf.MoveTowards(colorAdjustment.saturation.value, value, 1f);

        }
    }
}
