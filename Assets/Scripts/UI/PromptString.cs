using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteInEditMode]
public class PromptString : MonoBehaviour
{
    public TextMeshProUGUI text;
    public string promptStringKey;
    public string prefix;
    public string suffix;

    private float promptUpdateTimer;
    [System.NonSerialized] public bool textDirty = false;
    private bool updateText = false;

    public int promptSizeOverride = -1;

    public void Start()
    {
        RegisterPromptString(this);
        UpdateString();
    }

    public void OnValidate()
    {
        RegisterPromptString(this);
        textDirty = true;
        promptUpdateTimer = 0.25f;
    }

    public void Update()
    {
        if (textDirty)
        {
            UpdateString();
            textDirty = false;
        }
    }

    public static Dictionary<int, PromptString> allPromptStrings = new Dictionary<int, PromptString>();

    public static void RegisterPromptString(PromptString promptString)
    {
        allPromptStrings.TryAdd(promptString.gameObject.GetInstanceID(), promptString);
    }

    public static void RemovePromptString(PromptString promptString)
    {
        allPromptStrings.Remove(promptString.gameObject.GetInstanceID());
    }

    public static void UpdateAllPromptStrings()
    {
        foreach(PromptString promptString in allPromptStrings.Values)
        {
            promptString.textDirty = true;
        }
    }

    public void OnDestroy()
    {
        RemovePromptString(this);
    }

    public void UpdateString()
    {
        if (text != null)
        {
            bool flickerText = false;
            if (text.gameObject.activeSelf)
            {
                flickerText = true;
                text.gameObject.SetActive(false);
            }
            text.SetText(prefix + (promptSizeOverride > 0 ? "<size=" + promptSizeOverride.ToString() + ">" : "") + PromptStringTable.GetString(promptStringKey) + (promptSizeOverride > 0 ? "</size>" : "") + suffix);
            if (flickerText)
            {
                text.gameObject.SetActive(true);
            }

            text.Rebuild(UnityEngine.UI.CanvasUpdate.Layout);
            text.RecalculateClipping();
            text.SetAllDirty();
            text.SetLayoutDirty();
            text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            text.UpdateMeshPadding();
            text.UpdateVertexData();
            text.UpdateFontAsset();
            text.Rebuild(UnityEngine.UI.CanvasUpdate.LatePreRender);
            text.richText = true;
        }
    }
}
