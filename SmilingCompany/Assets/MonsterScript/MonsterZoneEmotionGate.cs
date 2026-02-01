using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonsterZoneEmotionGate : MonoBehaviour
{
    public enum RuleMode
    {
        RequireOne, // must match one emotion
        BlockOne    // must NOT be one emotion
    }

    [Header("Rule Mode")]
    public RuleMode mode = RuleMode.RequireOne;

    [Header("Rule: Require / Block")]
    public Emotion4 requiredEmotion = Emotion4.Smile; // used in RequireOne
    public Emotion4 blockedEmotion = Emotion4.Smile;  // used in BlockOne

    [Header("Timing")]
    public float angerFillTime = 1.0f; // seconds to fill from 0->1
    public float attackDelay = 0.2f;

    [Header("Refs")]
    public GameManager gameManager;
    public MonoBehaviour emotionProviderBehaviour; // must implement IEmotionProvider4
    public Animator monsterAnimator;
    public Slider angerSlider;

    [Header("UI (optional)")]
    public TMP_Text infoText;
    public string zoneDisplayName = "STAFF";
    [TextArea] public string ruleHint = "";
    public bool showUIOnlyWhenInside = true;

    [Header("Animator Params")]
    public string attackTriggerName = "Attack";

    [Header("Behavior")]
    public bool resetAngerWhenSafe = true;

    private IEmotionProvider4 emotionProvider;
    private bool playerInside = false;
    private bool isAttacking = false;
    private float anger01 = 0f;
    private Coroutine dieRoutine;

    void Awake()
    {
        emotionProvider = emotionProviderBehaviour as IEmotionProvider4;

        // Debug: check if provider is assigned and valid
        if (emotionProviderBehaviour == null)
            Debug.LogWarning($"[{gameObject.name}] emotionProviderBehaviour is NOT assigned!");
        else if (emotionProvider == null)
            Debug.LogWarning($"[{gameObject.name}] emotionProviderBehaviour does NOT implement IEmotionProvider4!");
        else
            Debug.Log($"[{gameObject.name}] EmotionProvider connected: {emotionProviderBehaviour.name}");

        if (angerSlider != null)
        {
            angerSlider.minValue = 0f;
            angerSlider.maxValue = 1f;
            angerSlider.value = 0f;
            angerSlider.gameObject.SetActive(false);
        }

        if (infoText != null && showUIOnlyWhenInside)
            infoText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (emotionProvider == null)
        {
            UpdateInfoText("NO INPUT", "--", GetRuleText());
            return;
        }

        if (!playerInside)
        {
            if (showUIOnlyWhenInside)
            {
                if (infoText != null) infoText.gameObject.SetActive(false);
            }
            else
            {
                UpdateInfoText("OUTSIDE", emotionProvider.Current.ToString(), GetRuleText());
            }
            return;
        }

        if (infoText != null && showUIOnlyWhenInside)
            infoText.gameObject.SetActive(true);

        if (isAttacking)
        {
            UpdateInfoText("ATTACK", emotionProvider.Current.ToString(), GetRuleText());
            return;
        }

        var current = emotionProvider.Current;
        bool safe = IsSafe(current);

        if (!safe)
        {
            anger01 += Time.deltaTime / Mathf.Max(angerFillTime, 0.0001f);
            anger01 = Mathf.Clamp01(anger01);

            if (angerSlider != null)
            {
                angerSlider.gameObject.SetActive(true);
                angerSlider.value = anger01;
            }

            // Alert sound gets louder as anger increases
            AudioManager.Instance?.SetMonsterAlertIntensity(anger01);

            float timeLeft = Mathf.Max(0f, angerFillTime * (1f - anger01));
            UpdateInfoText($"IN ZONE · KILL IN {timeLeft:0.00}s", current.ToString(), GetRuleText());

            if (anger01 >= 1f)
                TriggerAttack();
        }
        else
        {
            if (resetAngerWhenSafe)
            {
                anger01 = 0f;
                if (angerSlider != null)
                {
                    angerSlider.value = 0f;
                    angerSlider.gameObject.SetActive(false);
                }
                AudioManager.Instance?.StopMonsterAlert();
            }

            UpdateInfoText("IN ZONE · SAFE", current.ToString(), GetRuleText());
        }
    }

    private bool IsSafe(Emotion4 current)
    {
        return mode switch
        {
            RuleMode.RequireOne => current == requiredEmotion,
            RuleMode.BlockOne => current != blockedEmotion,
            _ => true
        };
    }

    private string GetRuleText()
    {
        return mode switch
        {
            RuleMode.RequireOne => $"Required: {requiredEmotion}",
            RuleMode.BlockOne => $"Forbidden: {blockedEmotion}",
            _ => "Rule: --"
        };
    }

    private void UpdateInfoText(string status, string current, string ruleLine)
    {
        if (infoText == null) return;

        if (!string.IsNullOrWhiteSpace(ruleHint))
        {
            infoText.text =
                $"{zoneDisplayName}\n" +
                $"{ruleHint}\n" +
                $"Status: {status}\n" +
                $"{ruleLine}\n" +
                $"Current: {current}";
        }
        else
        {
            infoText.text =
                $"{zoneDisplayName}\n" +
                $"Status: {status}\n" +
                $"{ruleLine}\n" +
                $"Current: {current}";
        }
    }

    private void TriggerAttack()
    {
        if (isAttacking) return;
        isAttacking = true;

        if (monsterAnimator != null && !string.IsNullOrEmpty(attackTriggerName))
            monsterAnimator.SetTrigger(attackTriggerName);

        if (dieRoutine != null) StopCoroutine(dieRoutine);
        dieRoutine = StartCoroutine(DieAfterDelay());
    }

    private IEnumerator DieAfterDelay()
    {
        yield return new WaitForSeconds(attackDelay);

        string ruleDesc = (mode == RuleMode.RequireOne)
            ? $"need {requiredEmotion}"
            : $"forbid {blockedEmotion}";

        gameManager?.Die($"{transform.root.name}: failed emotion check ({ruleDesc})");

        // reset
        isAttacking = false;
        anger01 = 0f;

        if (angerSlider != null)
        {
            angerSlider.value = 0f;
            angerSlider.gameObject.SetActive(false);
        }

        if (infoText != null && showUIOnlyWhenInside)
            infoText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{gameObject.name}] OnTriggerEnter: {other.name}, tag: {other.tag}");

        if (!other.CompareTag("Player")) return;

        Debug.Log($"[{gameObject.name}] Player ENTERED zone");
        playerInside = true;
        isAttacking = false;
        anger01 = 0f;

        if (infoText != null && showUIOnlyWhenInside)
            infoText.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        isAttacking = false;
        anger01 = 0f;

        if (angerSlider != null)
        {
            angerSlider.value = 0f;
            angerSlider.gameObject.SetActive(false);
        }

        AudioManager.Instance?.StopMonsterAlert();

        if (infoText != null && showUIOnlyWhenInside)
            infoText.gameObject.SetActive(false);
    }
}
