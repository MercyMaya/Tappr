using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Controls a single neon ring: lighting up, waiting, and reporting
/// whether the player tapped it in time.  All rings share this script,
/// keeping GameManager lean.
/// </summary>
[RequireComponent(typeof(Button))]

public class RingBehaviour : MonoBehaviour
{

    // How long since Activate() started – read by GameManager for speed bonus
    public float Elapsed { get; private set; }

    // Public read-only so GameManager can inspect status
    public bool IsResolved { get; private set; }
    public int RingIndex { get; private set; }

    // Internal refs
    private Image _image;
    private Button _button;
    private GameManager _gm;
    // Unique material instance so we can tint emission per ring
    private Material _matInstance;

    private Coroutine _activeRoutine;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleTap);


                // Clone the shared material so each ring has its own emission colour
        if (_image.material != null)
                    {
            _matInstance = Instantiate(_image.material);
            _image.material = _matInstance;
                    }


        // Start inactive / dim
        SetRingActive(false);
    }

    /// <summary>Called once from GameManager after instantiation.</summary>
    public void Init(GameManager gm, int index)
    {
        _gm = gm;
        RingIndex = index;
    }

    /// <summary>
    /// Lights the ring for the given duration.  While active, a tap counts
    /// as success; if time expires first, we report a miss.
    /// </summary>
    public void Activate(float duration)
    {
        // Reset state
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        IsResolved = false;

        _activeRoutine = StartCoroutine(ActivateRoutine(duration));
    }

    private IEnumerator ActivateRoutine(float duration)
    {
        SetRingActive(true);

        float timer = 0f;
        while (timer < duration && !IsResolved)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!IsResolved)
        {
            // Time expired; notify miss
            _gm.OnRingMissed(this);
        }

        SetRingActive(false);
        IsResolved = true;
    }

    private void HandleTap()
    {
        if (IsResolved) return; // Already dealt with

        IsResolved = true;
        SetRingActive(false);

        _gm.OnRingTapped(this);
    }

    private void SetRingActive(bool state)
    {
                if (state)
                    {
                        // ACTIVE: bright accent with bloom
                        if (_matInstance)
                            {
                _matInstance.SetColor("_EmissionColor", _gm.AccentColor);
                _matInstance.SetColor("_BaseColor", _gm.AccentColor);
                            }
                        else
                            {
                _image.color = _gm.AccentColor; // fallback if no material
                            }
                    }
                else
                    {
                        // IDLE: dim grey, zero emission
            Color idle = new(0.2f, 0.2f, 0.2f, 1f);
                        if (_matInstance)
                            {
                _matInstance.SetColor("_EmissionColor", Color.black);
                _matInstance.SetColor("_BaseColor", idle);
                            }
                        else
                            {
                _image.color = idle;
                            }
                    }
    }
}
