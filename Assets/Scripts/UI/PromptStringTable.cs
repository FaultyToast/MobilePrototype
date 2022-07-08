using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.SmartFormat;
using System.Text.RegularExpressions;

[CreateAssetMenu(fileName = "New Combo Attack", menuName = "FracturedAssets/PromptStringTable", order = 1)]
public class PromptStringTable : ScriptableObject
{
    public enum PromptType
    {
        KeyboardMouse,
        Xbox,
        Playstation
    }

    public PromptType currentPromptType;

    [System.Serializable]
    public class PromptGroup
    {
        public string groupKey;
        public string keyboardMouseString;
        public string xboxString;
        public string playstationString;
    }

    public List<PromptGroup> promptGroups;
    public Dictionary<string, PromptGroup> promptGroupsDictionary = new Dictionary<string, PromptGroup>();
    public Dictionary<int, PromptString> promptStrings = new Dictionary<int, PromptString>();
    public bool updatePromptStrings;

    private PromptType? lastPromptType = null;


    public static PromptStringTable _instance;
    public static PromptStringTable instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<PromptStringTable>("PromptStrings/MainTable");
                InitializeInstance(_instance);
            }
            return _instance;
        }
    }

    public static void InitializeInstance(PromptStringTable instance)
    {
        for (int i = 0; i < instance.promptGroups.Count; i++)
        {
            instance.promptGroupsDictionary.TryAdd(instance.promptGroups[i].groupKey, instance.promptGroups[i]);

            instance.promptGroups[i].keyboardMouseString = Regex.Unescape(instance.promptGroups[i].keyboardMouseString);
            instance.promptGroups[i].playstationString = Regex.Unescape(instance.promptGroups[i].playstationString);
            instance.promptGroups[i].xboxString = Regex.Unescape(instance.promptGroups[i].xboxString);
        }
    }

    public static string GetString(string key)
    {
        PromptGroup promptGroup;
        if (instance.promptGroupsDictionary.TryGetValue(key, out promptGroup))
        {
            switch(instance.currentPromptType)
            {
                case PromptType.KeyboardMouse:
                    return promptGroup.keyboardMouseString;
                case PromptType.Playstation:
                    return promptGroup.playstationString;
                case PromptType.Xbox:
                    return promptGroup.xboxString;
            }
        }
        return "";
    }

    public void UpdatePromptType(PromptType promptType)
    {
        currentPromptType = promptType;
        lastPromptType = promptType;
        PromptString.UpdateAllPromptStrings();
    }


    public void OnValidate()
    {
        if (updatePromptStrings || lastPromptType == null || lastPromptType.Value != currentPromptType)
        {
            updatePromptStrings = false;
            InitializeInstance(this);
            PromptString.UpdateAllPromptStrings();
        }
        lastPromptType = currentPromptType;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
