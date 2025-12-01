using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    public CharacterStats playerStats;
    public PlayerProgression playerProgression;

    [Header("UI")]
    public Slider healthSlider;
    public Slider xpSlider;
    public TMP_Text levelText; // TextMeshPro-compatible

    private void Start()
    {
        if (playerStats == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerStats = playerObj.GetComponent<CharacterStats>();
            }
        }

        if (playerProgression == null && playerStats != null)
        {
            playerProgression = playerStats.GetComponent<PlayerProgression>();
        }
    }

    private void Update()
    {
        if (playerStats != null && healthSlider != null)
        {
            healthSlider.maxValue = playerStats.maxHealth;
            healthSlider.value = playerStats.currentHealth;
        }

        if (playerProgression != null && xpSlider != null)
        {
            xpSlider.maxValue = playerProgression.xpToNextLevel;
            xpSlider.value = playerProgression.currentXP;
        }

        if (playerProgression != null && levelText != null)
        {
            levelText.text = $"Lv {playerProgression.level}";
        }
    }
}
