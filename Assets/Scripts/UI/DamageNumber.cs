using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [System.NonSerialized] public DamageInfo damageInfo;
    [System.NonSerialized] public float healingAmount = 0;
    [System.NonSerialized] public Vector3 spawnPosition;

    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;
    public Canvas canvas;
    public float fadeOutTime = 0.25f;

    public float lifetime = 0.5f;
    public float speed = 0.8f;
    public float speedRange = 0.3f;
    public float maxHorizontalDeviance = 1f;
    public float verticalTransitionSpeed = 1f;
    public float flatSlowPercentage = 0.1f;
    public float flatSlowSpeed = 0.1f;
    public float multipleNumbersSpeedMultiplier = 1f;

    private float timer = 0f;
    private Vector2 virtualPosition;
    private Vector3 anchor;
    private Vector2 direction;
    private bool fading = false;

    private Vector2 velocity;
    private Vector2 baseVelocity;
    private float targetYSpeed;

    [Header("Special Colours")]
    public Color32 critColor = new Color32(252, 82, 3, 255);
    public Color32 edenBoundColor = new Color32(252, 82, 3, 255);
    public Color32 poisonColor;
    public Color32 fireColor;
    public Color32 healingColor;

    public static List<DamageNumber> damageNumbers = new List<DamageNumber>();

    private int nearbyDamageNumbers;

    public void Awake()
    {
        damageNumbers.Add(this);

        nearbyDamageNumbers = GetNearbyDamageNumbers();
    }

    public void OnDestroy()
    {
        damageNumbers.Remove(this);
    }

    public void Start()
    {
        int number;
        if (healingAmount > 0)
        {
            number = Mathf.Max(Mathf.RoundToInt(healingAmount), 1);
            text.color = healingColor;
        }
        else
        {
            number = Mathf.Max(Mathf.RoundToInt(damageInfo.damage), 1);

            if (damageInfo.colorFlags.HasFlag(DamageNumberColorFlags.Poison))
            {
                text.color = poisonColor;
            }

            if (damageInfo.colorFlags.HasFlag(DamageNumberColorFlags.Burning))
            {
                text.color = fireColor;
            }

            if (damageInfo.colorFlags.HasFlag(DamageNumberColorFlags.EdenBoundActivated))
            {
                text.color = edenBoundColor;
            }

            if (damageInfo.colorFlags.HasFlag(DamageNumberColorFlags.IsCrit))
            {
                text.color = critColor;
            }
        }

        speed += speed * (nearbyDamageNumbers) * multipleNumbersSpeedMultiplier;

        velocity = new Vector2(Random.Range(-maxHorizontalDeviance, maxHorizontalDeviance), 1f).normalized;
        velocity *= (speed + Random.Range(-speedRange, speedRange));

        targetYSpeed = (speed + Random.Range(-speedRange, speedRange));

        baseVelocity = velocity;

        text.text = number.ToString();


        anchor = spawnPosition;
        SyncVirtualPosition();
    }

    public int GetNearbyDamageNumbers()
    {
        int numbers = 0;
        for (int i = 0; i < damageNumbers.Count; i++)
        {
            if (ReferenceEquals(damageNumbers[i], this))
            {
                continue;
            }
            if (Vector3.Distance(damageNumbers[i].text.transform.position, text.transform.position) < 10f)
            {
                numbers++;
            }
        }
        return numbers;
    }

    public void Update()
    {
        timer += Time.deltaTime;
        velocity.x -= (velocity.x - baseVelocity.x * 0.1f) * 0.1f * Time.deltaTime;

        velocity.y += verticalTransitionSpeed * Time.deltaTime / Mathf.Max(timer, 0.25f);

        virtualPosition += velocity * Time.deltaTime;
        SyncVirtualPosition();
        if (!fading && timer > lifetime)
        {
            fading = true;
            StartCoroutine(FadeOut());
        }
    }

    public void SyncVirtualPosition()
    {
        Vector3 screenPoint = GameManager.instance.mainCamera.WorldToScreenPoint(anchor);
        Vector3 viewportPoint = GameManager.instance.mainCamera.WorldToViewportPoint(anchor);

        if (viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1 && viewportPoint.z > 0)
        {
            text.enabled = true;
            text.transform.position = screenPoint + new Vector3(virtualPosition.x, virtualPosition.y, 0);
        }
        else
        {
            text.enabled = false;
        }
    }

    public IEnumerator FadeOut()
    {
        float alpha = 1;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime / fadeOutTime;
            canvasGroup.alpha = alpha;
            yield return null;
        }
        Destroy(gameObject);
    }
}