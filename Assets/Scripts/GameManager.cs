/*  GameManager.cs
 *  We’re upping the finesse: a flashy 3-2-1 countdown that fades, grows,
 *  shrinks, then hands control to our regular RoundLoop.  All done with a
 *  single coroutine so we keep Update() footprint at zero.  Nice and lean.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    /* ---------- Inspector hooks ---------- */
    [Header("Design-time references")]
    public RingBehaviour ringPrefab;
    public RectTransform ringParent;          // RingGrid
    [Range(3, 9)] public int ringCount = 3;   // tweak per difficulty

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text comboText;
    public TMP_Text countdownText;          // “3”, “2”, “1”, “GO”
    public GameObject gameOverPanel;
    public Image[] lifeIcons;              // three heart Images

    [Header("Timing (seconds)")]
    public float initialActiveTime = 1.5f;
    public float minActiveTime = 0.4f;
    public float timeDecayPerRound = 0.02f;

    [Header("Lives")]
    public int startingLives = 3;

    /* ---------- private runtime state ---------- */
    private readonly List<RingBehaviour> _rings = new();
    private float _currentActiveTime;
    private int _roundNumber;

    private int _score;
    private int _combo = 1;
    private int _lives;

    /* ---------- Unity flow ---------- */
    private void Awake()
    {
        _currentActiveTime = initialActiveTime;   // prime timer window
    }

    private void Start()
    {
        SpawnRings();          // grid is ready but we keep rings idle
        _lives = startingLives;
        UpdateHud();

        // Kick off the snazzy “3-2-1-GO” intro, then flow into gameplay
        StartCoroutine(StartSequence());
    }

    /* ---------- Countdown & game start ---------- */
    private IEnumerator StartSequence()
    {
        // We hide rings during countdown so players focus on the digits
        SetRingInteractivity(false);

        string[] sequence = { "3", "2", "1", "GO" };
        foreach (string token in sequence)
        {
            yield return RunCountdownBurst(token);
        }

        SetRingInteractivity(true);   // let rings accept taps
        StartCoroutine(RoundLoop());  // main game loop begins
    }

    /// <summary>
    /// Performs the “burst” animation for a single token (scale-fade).
    /// </summary>
    private IEnumerator RunCountdownBurst(string token)
    {
        const float burstTime = 0.6f;   // total time per digit
        const float growScale = 1.3f;   // overshoot growth factor

        countdownText.text = token;
        countdownText.gameObject.SetActive(true);

        // reset scale & alpha
        countdownText.rectTransform.localScale = Vector3.one;
        Color c = countdownText.color;
        c.a = 1f;
        countdownText.color = c;

        float t = 0f;
        while (t < burstTime)
        {
            t += Time.deltaTime;
            float norm = t / burstTime;          // 0 → 1

            // Phase 1: grow first 40 %, then shrink
            float scale = (norm < 0.4f)
                ? Mathf.Lerp(1f, growScale, norm / 0.4f)
                : Mathf.Lerp(growScale, 0.8f, (norm - 0.4f) / 0.6f);
            countdownText.rectTransform.localScale =
                new Vector3(scale, scale, 1f);

            // Fade out towards the end
            if (norm > 0.5f)
            {
                c.a = Mathf.Lerp(1f, 0f, (norm - 0.5f) * 2f);
                countdownText.color = c;
            }

            yield return null;
        }

        countdownText.gameObject.SetActive(false);
    }

    /* ---------- Ring factory ---------- */
    private void SpawnRings()
    {
        for (int i = 0; i < ringCount; i++)
        {
            RingBehaviour ring = Instantiate(ringPrefab, ringParent);
            ring.Init(this, i);
            _rings.Add(ring);
        }
    }

    private void SetRingInteractivity(bool state)
    {
        foreach (RingBehaviour r in _rings)
        {
            r.gameObject.SetActive(state);   // simplest: toggle active
        }
    }

    /* ---------- Main gameplay loop ---------- */
    private IEnumerator RoundLoop()
    {
        while (true)                           // endless until Game Over
        {
            _roundNumber++;

            int index = Random.Range(0, _rings.Count);
            RingBehaviour target = _rings[index];

            target.Activate(_currentActiveTime);
            yield return new WaitUntil(() => target.IsResolved);

            _currentActiveTime = Mathf.Max(
                minActiveTime,
                _currentActiveTime - timeDecayPerRound);
        }
    }

    /* ---------- Callbacks from RingBehaviour ---------- */
    public void OnRingTapped(RingBehaviour ring)
    {
        float speedRatio = 1f - (ring.Elapsed / _currentActiveTime);
        float speedBonus = Mathf.Lerp(1f, 2f, speedRatio);

        int basePoints = 10;
        int gained = Mathf.RoundToInt(basePoints * speedBonus * _combo);

        _score += gained;
        _combo = Mathf.Min(_combo + 1, 10);

        UpdateHud();
    }

    public void OnRingMissed(RingBehaviour ring)
    {
        _combo = 1;
        _score = Mathf.Max(0, _score - 5);
        _lives = Mathf.Max(0, _lives - 1);

        if (_lives == 0)
        {
            EndGame();
            return;
        }

        UpdateHud();
    }

    /* ---------- HUD upkeep ---------- */
    private void UpdateHud()
    {
        if (scoreText) scoreText.text = $"Score {_score}";
        if (comboText) comboText.text = $"Combo x{_combo}";

        if (lifeIcons != null && lifeIcons.Length > 0)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
                lifeIcons[i].enabled = i < _lives;
        }
    }

    /* ---------- End-of-round logistics ---------- */
    private void EndGame()
    {
        StopAllCoroutines();
        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
