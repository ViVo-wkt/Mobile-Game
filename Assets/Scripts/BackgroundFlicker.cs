using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BackgroundFlicker : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("Assign the UI Image object here.")]
    public Image targetImage;
    
    [Tooltip("The default look (e.g., Light On).")]
    public Sprite mainSprite;
    
    [Tooltip("The secondary look (e.g., Light Off/Dim).")]
    public Sprite flickerSprite;

    [Header("Timing Settings")]
    [Tooltip("Minimum time between flickers (in seconds).")]
    public float minInterval = 0.05f;
    
    [Tooltip("Maximum time between flickers (in seconds).")]
    public float maxInterval = 0.2f;

    [Header("Behavior")]
    [Tooltip("If true, the flicker happens in short bursts.")]
    public bool useBurstMode = true;

    private void Start()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage != null)
        {
            targetImage.sprite = mainSprite;
            StartCoroutine(FlickerRoutine());
        }
        else
        {
            Debug.LogError("BackgroundFlicker: No Image component found!");
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // 1. Wait for a "stable" period (Light is ON)
            // If using Burst Mode, we wait longer here to simulate the light being mostly functional
            float waitTime = useBurstMode ? Random.Range(0.5f, 2.0f) : Random.Range(minInterval, maxInterval);
            targetImage.sprite = mainSprite;
            yield return new WaitForSeconds(waitTime);

            // 2. Trigger the Flicker (Switch to OFF/Alternative sprite)
            targetImage.sprite = flickerSprite;

            // 3. Wait for a very short "glitch" duration
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }
}