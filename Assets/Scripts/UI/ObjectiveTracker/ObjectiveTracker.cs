using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectiveTracker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI subObjectiveText;
    public TextMeshProUGUI globalLevelTracker;
    private bool objectiveActive = false;

    public void Awake()
    {
        ClearObjective();
    }

    public void ClearObjective()
    {
        subObjectiveText.text = "";
        objectiveText.text = "Proceed";
        objectiveActive = false;
        subObjectiveText.enabled = false;
    }

    public void ClearSubObjective()
    {
        subObjectiveText.text = "";
    }

    public void SetObjective(string text)
    {
        objectiveText.text = text;
        objectiveActive = true;
        subObjectiveText.enabled = true;
    }

    public void SetSubObjective(string text)
    {
        subObjectiveText.text = "- " + text;
    }
}
