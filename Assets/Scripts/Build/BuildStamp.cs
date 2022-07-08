using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BuildStamp : MonoBehaviour
{
    public static BuildStamp instance;
    public TextMeshProUGUI text;

    private void Awake()
    {
        // Remove ASAP
        Destroy(gameObject);
        if (instance == null)
        {
            instance = this;
        }
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        text.text = Application.version;
    }
}
