using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class PromptDisplay : MonoBehaviour
{
    public TextMeshProUGUI text;
    public string promptString
    {
        set
        {
            text.text = prefix + (promptSizeOverride > 0 ? ("<size=" + promptSizeOverride.ToString() + ">") : "") + value + (promptSizeOverride > 0 ? "</size>" : "") + suffix;
        }
    }

    public string suffix;
    public string prefix;
    public float promptSizeOverride = -1f;
}
