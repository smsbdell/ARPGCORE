using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("References")]
    public DamagePopup popupPrefab;

    [Tooltip("Canvas where damage popups will be drawn. Should be a Screen Space - Overlay canvas.")]
    public Canvas targetCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // No DontDestroyOnLoad here to keep it simple in a single scene.
    }

    public void SpawnPopup(float damage, Vector3 worldPosition)
    {
        if (popupPrefab == null || targetCanvas == null || Camera.main == null)
            return;

        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        // Instantiate under the canvas
        DamagePopup popup = Instantiate(popupPrefab, targetCanvas.transform);
        popup.transform.position = screenPos;
        popup.Setup(damage);
    }
}
