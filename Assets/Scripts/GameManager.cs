/*  GameManager.cs
 *  Central command for TappR.
 *  Responsibilities:
 *    • snazzy 3-2-1-GO intro
 *    • ring spawning / activation
 *    • scoring, combo, lives
 *    • game-over flow
 *
 *  Newest tweaks:
 *    – inter-round pause so misses are obvious
 *    – countdown comment cleanup (no meta chatter)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Buffers.Text;

public class GameManager : MonoBehaviour
{

    /* ---------- Session accent colour ---------- */
    // Exposed read-only so rings can query it.
    public Color AccentColor { get; private set; }
    /* ---------- Inspector hooks ---------- */
    [Header("Design-time references")]
    [Tooltip("Our neon ring prefab (already carries RingBehaviour).")]
    public RingBehaviour ringPrefab;

    [Tooltip("Parent RectTransform that owns ring instances (RingGrid).")]
    public RectTransform ringParent;

    [Tooltip("How many rings live this session (3 / 6 / 9).")]
    [Range(3, 9)] public int ringCount = 3;

    [Header("UI widgets")]
    public TMP_Text scoreText;         // “Score 0”
    public TMP_Text comboText;         // “Combo x1”
    public TMP_Text countdownText;     // “3…2…1…GO”
    public GameObject gameOverPanel;     // blackout overlay with restart button

    [Tooltip("We drag our three heart sprites here.")]
    public Image[] lifeIcons;         // ❤❤❤

    [Header("Timing (seconds)")]
    public float initialActiveTime = 1.5f;   // generous on first round
    public float minActiveTime = 0.4f;   // safety floor
    public float timeDecayPerRound = 0.02f;  // how aggressively we speed up
    public float interRoundPause = 0.25f;  // tiny breather each round

    [Header("Lives")]
    public int startingLives = 3;             // three strikes, we’re out

    /* ---------- Runtime state ---------- */
    private readonly List<RingBehaviour> _rings = new(); // pooled references
    private float _currentActiveTime;
    private int _roundNumber;

    private int _score;
    private int _combo = 1;
    private int _lives;

    /* ---------- Unity lifecycle ---------- */
    private void Awake()
    {
        // Prime our first active-window length so we can shrink from there.
        _currentActiveTime = initialActiveTime;
    }

    private void Start()
    {
        SpawnRings();          // grid is now full of idle rings
        _lives = startingLives;
        UpdateHud();

        // --- Choose a neon accent for this run ---
        AccentColor = PickRandomAccent();   // HDR-ready (intensity > 1)

        // Kick off the 3-2-1 countdown coroutine; gameplay follows.
        StartCoroutine(StartSequence());
    }




    /// <summary>Returns a spicy HDR colour each session.</summary>
    private Color PickRandomAccent()
    {
        // Classic “neon” palette in normalized 0-1 RGB (we’ll multiply for HDR)
        Color[] palette =
        {
            new (0.0f, 1.0f, 1.0f), // cyan
            new (1.0f, 0.2f, 1.0f), // magenta
            new (0.2f, 1.0f, 0.4f), // lime
            new (1.0f, 0.9f, 0.2f), // yellow
            new (1.0f, 0.4f, 0.0f)  // orange
        };

Color baseC = palette[Random.Range(0, palette.Length)];

        // Multiply by >1 to push into HDR so Bloom pops (intensity 2)
        return baseC * 2f;
    }




    /* ---------- 3-2-1 intro ---------- */
    private IEnumerator StartSequence()
    {
        SetRingInteractivity(false);   // nobody taps before we say GO

        string[] seq = { "3", "2", "1", "GO" };
        foreach (string token in seq)
            yield return RunCountdownBurst(token);

        SetRingInteractivity(true);    // green light
        StartCoroutine(RoundLoop());   // off we go
    }

    /// <summary>
    /// Performs the grow-shrink-fade burst for a single countdown token.
    /// </summary>
    private IEnumerator RunCountdownBurst(string token)
    {
        const float burstTime = 0.6f;   // total time per digit
        const float growScale = 1.3f;   // cute overshoot factor

        countdownText.text = token;
        countdownText.gameObject.SetActive(true);

        // Reset scale and full opacity
        countdownText.rectTransform.localScale = Vector3.one;
        Color c = countdownText.color; c.a = 1f; countdownText.color = c;

        float t = 0f;
        while (t < burstTime)
        {
            t += Time.deltaTime;
            float n = t / burstTime;           // 0 → 1

            /* Phase 1 – inflate slightly, then deflate */
            float scale = (n < .4f)
                ? Mathf.Lerp(1f, growScale, n / .4f)
                : Mathf.Lerp(growScale, 0.8f, (n - .4f) / .6f);

            countdownText.rectTransform.localScale =
                new Vector3(scale, scale, 1f);

            /* Phase 2 – fade out in the second half */
            if (n > .5f)
            {
                c.a = Mathf.Lerp(1f, 0f, (n - .5f) * 2f);
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
            ring.Init(this, i);   // each ring knows who to report to
            _rings.Add(ring);
        }
    }

    private void SetRingInteractivity(bool state)
    {
        // Easiest switch: toggle active state.  This helps us keep the hierarchy simple.
        foreach (RingBehaviour r in _rings)
            r.gameObject.SetActive(state);
    }

    /* ---------- Main gameplay coroutine ---------- */
    private IEnumerator RoundLoop()
    {
        // Runs forever until we call EndGame()
        while (true)
        {
            _roundNumber++;

            // Pick one ring at random to light up
            int idx = Random.Range(0, _rings.Count);
            RingBehaviour ring = _rings[idx];

            ring.Activate(_currentActiveTime);
            yield return new WaitUntil(() => ring.IsResolved);

            // Tiny pause so hits vs. misses register clearly
            yield return new WaitForSeconds(interRoundPause);

            // Shrink the active window for next round
            _currentActiveTime = Mathf.Max(
                minActiveTime,
                _currentActiveTime - timeDecayPerRound);
        }
    }

    /* ---------- Ring callbacks ---------- */
    public void OnRingTapped(RingBehaviour ring)
    {
        // Quickness bonus scales 1×–2×
        float speedRatio = 1f - (ring.Elapsed / _currentActiveTime);
        float speedBonus = Mathf.Lerp(1f, 2f, speedRatio);

        int gained = Mathf.RoundToInt(10 * speedBonus * _combo);
        _score += gained;

        _combo = Mathf.Min(_combo + 1, 10);  // cap runaway multiplier

        UpdateHud();
    }

    public void OnRingMissed(RingBehaviour ring)
    {
        _combo = 1;                           // reset multiplier
        _score = Mathf.Max(0, _score - 5);    // gentle sting

        _lives = Mathf.Max(0, _lives - 1);    // pluck a heart
        if (_lives == 0) { EndGame(); return; }

        UpdateHud();
    }

    /* ---------- HUD upkeep ---------- */
    private void UpdateHud()
    {
        if (scoreText) scoreText.text = $"Score {_score}";
        if (comboText) comboText.text = $"Combo x{_combo}";

        for (int i = 0; i < lifeIcons.Length; i++)
            lifeIcons[i].enabled = i < _lives;
    }

    /* ---------- Game-over logistics ---------- */
    private void EndGame()
    {
        StopAllCoroutines();            // freeze RoundLoop cleanly
        gameOverPanel.SetActive(true);  // reveal overlay
    }

    public void RestartGame() =>
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
