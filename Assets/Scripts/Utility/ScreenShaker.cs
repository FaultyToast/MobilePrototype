using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class ScreenShaker : MonoBehaviour
{
    CinemachineFreeLook camera;

    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<CinemachineFreeLook>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartScreenShake(float time, float amplitude, float frequency)
    {
        StartCoroutine(ScreenShake(time, amplitude, frequency));
    }

    IEnumerator ScreenShake(float time, float amplitude, float frequency)
    {
        float timeElapsed = 0;

        while (timeElapsed < time)
        {
            Noise(amplitude, frequency);
            timeElapsed += Time.deltaTime;

            yield return null;
        }
        Noise(0 , 0);
    }

    void Noise(float amplitude, float frequency)
    {
        camera.GetRig(0).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = amplitude;
        camera.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = amplitude;
        camera.GetRig(2).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = amplitude;
        camera.GetRig(0).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = frequency;
        camera.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = frequency;
        camera.GetRig(2).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = frequency;
    }
}
