using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    public CharacterStats targetStats;
    public Slider slider;
    public Vector3 worldOffset = new Vector3(0f, 0.8f, 0f);

    private void Awake()
    {
        // Auto-wire stats if not assigned.
        if (targetStats == null)
        {
            targetStats = GetComponentInParent<CharacterStats>();
        }

        // Optional: auto-wire slider if not assigned.
        if (slider == null)
        {
            slider = GetComponentInChildren<Slider>();
        }
    }

    private void OnEnable()
    {
        RefreshImmediate();
    }

    private void LateUpdate()
    {
        RefreshImmediate();
    }

    private void RefreshImmediate()
    {
        if (targetStats == null || slider == null)
            return;

        // Drive the values every frame
        slider.minValue = 0f;
        slider.maxValue = targetStats.maxHealth;
        slider.value = targetStats.currentHealth;

        // Keep the bar above the target
        transform.position = targetStats.transform.position + worldOffset;
    }
}
