using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EditorMaterialUpdater : MonoBehaviour
{
    public string Description;

    [Header("Player Materials")]
    public Color playerColour;
    public Color playerHitFlashColour;
    public float playerFlashFresnel;
    public List<Material> playerMaterials;

    [Header("Enemy Materials")]
    [ColorUsage(true,true)]
    public Color enemyHighlightColour;
    public Color enemyHitFlashColour;
    public float enemyFlashFresnel;
    [Range(0,1)]
    public float enemyHighlight;
    public List<Material> enemyMaterials;


    private Color playerColourTemp;
    private Color playerHitFlashColourTemp;

    private Color enemyHighlightColourTemp;
    private Color enemyHitFlashColourTemp;

    private float playerFlashFresnelTemp;
    private float enemyFlashFresnelTemp;
    private float enemyHighlightTemp;

    private void Update()
    {
        //playerColour
        
        //Detects a change in the material Colours
        if (playerColour != playerColourTemp 
            || playerHitFlashColour != playerHitFlashColourTemp 
            || enemyHighlightColour != enemyHighlightColourTemp 
            || enemyHitFlashColour != enemyHitFlashColourTemp
            || playerFlashFresnel != playerFlashFresnelTemp
            || enemyFlashFresnel != enemyFlashFresnelTemp
            || enemyHighlight != enemyHighlightTemp)
        {
            foreach (Material mat in playerMaterials)
            {
                mat.SetColor("_PlayerColour", playerColour);
                mat.SetColor("_FlashColour", playerHitFlashColour);
                mat.SetFloat("_FlashFresnel", playerFlashFresnel);
            }

            foreach (Material mat in enemyMaterials)
            {
                mat.SetColor("_HighlightColour", enemyHighlightColour);
                mat.SetColor("_FlashColour", enemyHitFlashColour);
                mat.SetFloat("_FlashFresnel", enemyFlashFresnel);
                mat.SetFloat("_EnemyHighlight", enemyHighlight);
            }

            playerColourTemp = playerColour;
            playerHitFlashColourTemp = playerHitFlashColour;
            enemyHighlightColourTemp = enemyHighlightColour; 
            enemyHitFlashColourTemp = enemyHitFlashColour;
            playerFlashFresnelTemp = playerFlashFresnel;
            enemyFlashFresnelTemp = enemyFlashFresnel;
            enemyHighlightTemp = enemyHighlight;
        }
    }
}
