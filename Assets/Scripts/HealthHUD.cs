using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    public Image[] HeartIcons;
    public Sprite FullHeart;
    public Sprite EmptyHeart; 

    public void UpdateDisplay(float currentHealth)
    {
        for (int i = 0; i < HeartIcons.Length; i++)
        {
            // NEW LOGIC:
            // Heart 1 (i=0) checks if Health > 0
            // Heart 2 (i=1) checks if Health > 25
            // Heart 3 (i=2) checks if Health > 50
            // Heart 4 (i=3) checks if Health > 75
            float threshold = i * 25f;

            bool isActive = currentHealth > threshold;

            if (EmptyHeart != null)
            {
                HeartIcons[i].sprite = isActive ? FullHeart : EmptyHeart;
                HeartIcons[i].enabled = true; 
            }
            else
            {
                HeartIcons[i].enabled = isActive;
            }
        }
    }
}