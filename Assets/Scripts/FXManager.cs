using UnityEngine;

/// <summary>
/// Central FX hub: plays SFX, spawns particles, shakes camera.
/// Lives on a single GameObject in the scene (we’ll name it FXManager).
/// </summary>
public class FXManager : MonoBehaviour
{
    public static FXManager I { get; private set; }

    [Header("Audio clips")]
    public AudioClip tapClip;
    public AudioClip missClip;

    [Header("Particles")]
    public ParticleSystem hitSparkPrefab;   // small radial burst

    [Header("Shake")]
    public Camera mainCam;
    private Vector3 _camStartPos;

    private AudioSource _audio;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        _audio = GetComponent<AudioSource>();
        _camStartPos = mainCam.transform.localPosition;
    }

    /* ---------- Public helpers ---------- */
    public void PlayTap(Vector3 worldPos)
    {
        _audio.PlayOneShot(tapClip, 0.9f);
             if (hitSparkPrefab)
                    {
                        // normal cyan/accent spark
            Instantiate(hitSparkPrefab, worldPos, Quaternion.identity, transform);
                    }
    }

    public void PlayMiss(Vector3 worldPos)
    {
        _audio.PlayOneShot(missClip, 0.9f);
            // Re-use the same prefab but tint it deep red for a “miss” flash
    if (hitSparkPrefab)
                {
            var spark = Instantiate(hitSparkPrefab, worldPos, Quaternion.identity, transform);
            var main = spark.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.2f, 0.2f));
                }
    }

    public void Shake(float amplitude, float duration) =>
        StartCoroutine(ShakeRoutine(amplitude, duration));

    /* ---------- Shake coroutine ---------- */
    private System.Collections.IEnumerator ShakeRoutine(float amp, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            Vector3 rand = (Vector3)Random.insideUnitCircle * amp;
            mainCam.transform.localPosition = _camStartPos + rand;
            yield return null;
        }
        mainCam.transform.localPosition = _camStartPos;
    }
}
