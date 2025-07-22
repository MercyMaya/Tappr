using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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

    // -------------- Runtime collections ----------------
    private readonly List<RingBehaviour> _rings = new();

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
        // TODO: Add score, combo, particles, etc.
    }

    /// <summary>
    /// Called by RingBehaviour when the ring timed out un-tapped.
    /// </summary>
    public void OnRingMissed(RingBehaviour ring)
    {
        Debug.Log($"❌ Missed ring {ring.RingIndex} on round {_roundNumber}");
        // TODO: decrement lives or apply penalty
    }
}
