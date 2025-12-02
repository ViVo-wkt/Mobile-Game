using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    [Tooltip("Drag the 4 Heart Images from the hierarchy here, ordered from first to last.")]
    public Image[] HeartIcons;

    [Tooltip("Sprite to use when the heart is full")]
    public Sprite FullHeart;
    [Tooltip("Sprite to use when the heart is empty (optional)")]
    public Sprite EmptyHeart; 

    public void UpdateDisplay(float currentHealth)
    {
        // We assume Max Health is 100, so each heart represents 25 HP.
        for (int i = 0; i < HeartIcons.Length; i++)
        {
            // Calculate the threshold for this specific heart (25, 50, 75, 100)
            float threshold = (i + 1) * 25f;

            bool isActive = currentHealth >= threshold;

            if (EmptyHeart != null)
            {
                // If we have an empty sprite, swap the sprite
                HeartIcons[i].sprite = isActive ? FullHeart : EmptyHeart;
                HeartIcons[i].enabled = true; 
            }
            else
            {
                // Otherwise, just hide/show the image
                HeartIcons[i].enabled = isActive;
            }
        }
    }
}