/*  GameManager.cs
 *  This is the central brains of TappR.
 *  We keep it heavily commented — as if our whole dev team is hovering
 *  over one keyboard and I’m narrating exactly why each line exists.
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
    [Tooltip("Our ring prefab — lives inside Assets/Prefabs/Ring")]
    public RingBehaviour ringPrefab;

    [Tooltip("Parent that owns the dynamic ring instances (RingGrid)")]
    public RectTransform ringParent;

    [Tooltip("How many rings do we spawn for this session (3/6/9).")]
    public int ringCount = 3;

    [Header("UI")]
    public TMP_Text scoreText;          // “Score 0”
    public TMP_Text comboText;          // “Combo x1”
    public TMP_Text livesText;          // optional, if we prefer text over hearts
    public GameObject gameOverPanel;    // the blackout panel with a Restart button
    public Image[] lifeIcons;           // drag three white hearts here in order

    [Header("Timing (seconds)")]
    public float initialActiveTime = 1.5f;   // first ring stays lit this long
    public float minActiveTime = 0.4f;   // hard floor so it never becomes impossible
    public float timeDecayPerRound = 0.02f;  // how aggressively we ramp up speed

    [Header("Lives")]
    public int startingLives = 3;            // tweak for difficulty experiments

    /* ---------- private runtime state ---------- */
    private readonly List<RingBehaviour> _rings = new(); // pooled rings
    private float _currentActiveTime;
    private int _roundNumber;

    private int _score;
    private int _combo = 1; // multiplier starts at 1×
    private int _lives;

    /* ---------- Unity flow ---------- */
    private void Awake()
    {
        _currentActiveTime = initialActiveTime;   // prime the pump
    }

    private void Start()
    {
        SpawnRings();

        _lives = startingLives;  // stock up on hearts
        UpdateHud();             // show the crew our zeroed scoreboard

        StartCoroutine(RoundLoop());  // and… ACTION!
    }

    /* ---------- Ring factory ---------- */
    private void SpawnRings()
    {
        for (int i = 0; i < ringCount; i++)
        {
            // Clone + parent so GridLayout does the positioning for us
            RingBehaviour ring = Instantiate(ringPrefab, ringParent);
            ring.Init(this, i);        // let the ring know who’s boss
            _rings.Add(ring);
        }
    }

    /* ---------- Main gameplay loop ---------- */
    private IEnumerator RoundLoop()
    {
        // This coroutine never exits — we kill it manually on Game Over.
        while (true)
        {
            _roundNumber++;

            // 1) Pick a random ring
            int index = Random.Range(0, _rings.Count);
            RingBehaviour target = _rings[index];

            // 2) Tell that ring to light up for X seconds
            target.Activate(_currentActiveTime);

            // 3) Yield until the ring reports “I’ve been tapped OR timed-out”
            yield return new WaitUntil(() => target.IsResolved);

            // 4) Speed curve — shrink the active window next round
            _currentActiveTime = Mathf.Max(minActiveTime,
                                           _currentActiveTime - timeDecayPerRound);
        }
    }

    /* ---------- Callbacks from RingBehaviour ---------- */
    public void OnRingTapped(RingBehaviour ring)
    {
        // 🎯  Nailed it!  Let’s dish out points.
        //     Bonus scales with quickness; ring.Elapsed == reaction time.
        float speedRatio = 1f - (ring.Elapsed / _currentActiveTime); // 0..1
        float speedBonus = Mathf.Lerp(1f, 2f, speedRatio);           // 1× to 2×

        int basePoints = 10;
        int gained = Mathf.RoundToInt(basePoints * speedBonus * _combo);

        _score += gained;
        _combo = Mathf.Min(_combo + 1, 10); // gently cap the runaway multiplier

        UpdateHud();
    }

    public void OnRingMissed(RingBehaviour ring)
    {
        // 😬  Either the timer expired or player fat-fingered the wrong ring.
        _combo = 1;                              // reset multiplier
        _score = Mathf.Max(0, _score - 5);       // soft slap on the wrist

        _lives = Mathf.Max(0, _lives - 1);       // pluck a heart
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
        // Keep it simple: update whatever widgets the designer wired up.
        if (scoreText) scoreText.text = $"Score {_score}";
        if (comboText) comboText.text = $"Combo x{_combo}";
        if (livesText) livesText.text = $"Lives {_lives}";

        if (lifeIcons != null && lifeIcons.Length > 0)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                bool alive = i < _lives;
                lifeIcons[i].enabled = alive;          // show or hide each heart
            }
        }
    }

    /* ---------- End-of-round logistics ---------- */
    private void EndGame()
    {
        // Freeze gameplay and celebrate/commiserate with a Game Over card
        StopAllCoroutines();               // halts RoundLoop cleanly
        gameOverPanel.SetActive(true);     // fade in overlay
    }

    // Hooked to the Restart button in the Game Over panel
    public void RestartGame()
    {
        // Hard reset: just re-load the active scene — quick & tidy.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
