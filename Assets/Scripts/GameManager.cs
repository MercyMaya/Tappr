using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// GameManager orchestrates the entire round-based flow of TappR.
/// We (the devs) treat it as the traffic cop: deciding which ring lights up,
/// how long it stays active, and when rounds speed up.  Nothing flashy yet,
/// but this scaffold lets us fill in each TODO incrementally while keeping
/// compile errors at bay.
/// </summary>
public class GameManager : MonoBehaviour
{
    // -------------- Inspector bindings -----------------
    [Header("Design-time references")]
    [Tooltip("Prefab for our neon ring.")]
    public RingBehaviour ringPrefab;

    [Tooltip("Parent transform that holds ring instances (should be RingGrid).")]
    public Transform ringParent;

    [Tooltip("How many ring positions should be live this session (3/6/9).")]
    public int ringCount = 3;

    // -------------- UI references ----------------------
    [Header("UI")]
    public TMP_Text scoreText;          // drag ScoreText here
    public TMP_Text comboText;          // drag ComboText here

    // -------------- Runtime values ---------------------
     private readonly List<RingBehaviour> _rings = new();
    private int _score;
    private int _combo = 1;

    // -------------- Gameplay tuning --------------------
    [Header("Timing (seconds)")]
    public float initialActiveTime = 1.5f;   // generous window on first round
    public float minActiveTime = 0.4f;       // cap difficulty
    public float timeDecayPerRound = 0.02f;  // how quickly we speed up

    private float _currentActiveTime;
    private int _roundNumber;

    // ---------------------------------------------------
    private void Awake()
    {
        // Cache the starting active-time so we can reset on restart.
        _currentActiveTime = initialActiveTime;
    }

    private void Start()
    {
        SpawnRings();
        UpdateHud();
        StartCoroutine(RoundLoop());
    }

    /// <summary>
    /// Instantiates the desired number of rings at runtime.
    /// We keep references so we can enable/disable them later.
    /// </summary>
    private void SpawnRings()
    {
        for (int i = 0; i < ringCount; i++)
        {
            RingBehaviour ring = Instantiate(ringPrefab, ringParent);
            ring.Init(this, i);   // pass index for identification
            _rings.Add(ring);
        }
    }

    /// <summary>
    /// Core game coroutine – each iteration lights exactly one ring,
    /// waits for tap or timeout, then proceeds to the next round.
    /// </summary>
    private IEnumerator RoundLoop()
    {
        while (true)  // Endless for now; we’ll exit based on lives later
        {
            _roundNumber++;

            // Choose a random ring to activate
            int index = Random.Range(0, _rings.Count);
            RingBehaviour activeRing = _rings[index];
            activeRing.Activate(_currentActiveTime);

            // Wait until that ring tells us it's done (tap or timeout)
            yield return new WaitUntil(() => activeRing.IsResolved);

            // Speed curve – shrink the active window each round
            _currentActiveTime = Mathf.Max(minActiveTime, _currentActiveTime - timeDecayPerRound);
        }
    }

    /// <summary>
    /// Called by RingBehaviour when the player taps successfully.
    /// We'll flesh out score, combo, and lives in the next milestone.
    /// </summary>
    public void OnRingTapped(RingBehaviour ring)
    {
        Debug.Log($"✅ Ring {ring.RingIndex} tapped on round {_roundNumber}");
         
        // ---------- scoring ----------
        float speedBonus = Mathf.Lerp(1f, 2f,   // maps 0 → 1 to a 1x→2x bonus
        (_currentActiveTime - ring.Elapsed) / _currentActiveTime);
        
        int basePoints = 10;
        int gained = Mathf.RoundToInt(basePoints * speedBonus * _combo);
        
        _score += gained;
        
        // ---------- combo ----------
        _combo = Mathf.Min(_combo + 1, 10); // cap at 10× so it doesn't explode
        
        UpdateHud();
    }

    /// <summary>
    /// Called by RingBehaviour when the ring timed out un-tapped.
    /// </summary>
    public void OnRingMissed(RingBehaviour ring)
    {
        Debug.Log($"❌ Missed ring {ring.RingIndex} on round {_roundNumber}");
        _combo = 1;          // reset combo
        _score = Mathf.Max(0, _score - 5);  // small penalty
        
        UpdateHud();
    }

        private void UpdateHud()
    {
        if (scoreText) scoreText.text = $"Score {_score}";
        if (comboText) comboText.text = $"Combo x{_combo}";
    }

}
