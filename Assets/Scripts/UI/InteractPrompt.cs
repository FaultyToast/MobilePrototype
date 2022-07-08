using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor;

[ExecuteInEditMode]
public class InteractPrompt : MonoBehaviour
{
    public TextMeshProUGUI text;

    [SerializeField] private string _promptText;
    public string promptText
    {
        set
        {
            _promptText = value;
            UpdateText();
        }
        get
        {
            return _promptText;
        }
    }

    private string _buttonPromptString;
    public string buttonPromptString
    {
        get
        {
            return _buttonPromptString;
        }
        set
        {
            _buttonPromptString = value;
            UpdateText();
        }
    }

    private void Start()
    {
        text.enabled = false;
    }

    private void OnValidate()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        text.text = buttonPromptString + " - " + promptText;
    }
}
