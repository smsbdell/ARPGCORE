using UnityEngine;

/// <summary>
/// Sorts a SpriteRenderer's order based on its Y position, so objects lower on the screen
/// render in front of objects higher up (classic top-down sorting).
/// Attach this to the player, monsters, and any dynamic or static props that should obey Y-sorting.
/// Ground tiles should NOT use this; they should stay at a fixed low sortingOrder.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSortSprite : MonoBehaviour
{
    [Tooltip("If true, the sorting order will be updated every frame (for moving objects like the player). " +
             "If false, it will be set once on Start (for static props like trees/rocks).")]
    public bool isDynamic = true;

    [Tooltip("Offset applied to the computed sorting order. Use this to bump certain sprites in front/behind others.")]
    public int sortingOffset = 0;

    [Tooltip("Multiplier for converting position to sorting order. For screen-space sorting a value around 1 is typical.")]
    public float sortFactor = 1f;

    private const int DefaultMinimumSortingOrder = 100000;

    [Tooltip("Base sorting order offset. Use a large value to keep world-space sorting clear of zero without clamping together.")]
    public int minimumSortingOrder = DefaultMinimumSortingOrder;

    [Tooltip("Use the camera's screen-space Y position to sort. Helps prevent far-off world positions from collapsing into the clamp.")]
    public bool useScreenSpaceSorting = false;

    [Tooltip("Only update sorting while the renderer is visible on screen.")]
    public bool onlySortWhenVisible = true;

    [Tooltip("Camera used for screen-space sorting. If left null, Camera.main is used.")]
    public Camera sortingCamera;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (sortingCamera == null)
            sortingCamera = Camera.main;

        UpdateSortingOrder();
    }

    private void Reset()
    {
        isDynamic = true;
        sortingOffset = 0;
        sortFactor = 1f;
        minimumSortingOrder = DefaultMinimumSortingOrder;
        useScreenSpaceSorting = false;
        onlySortWhenVisible = true;
        sortingCamera = null;
    }

    private void OnValidate()
    {
        if (minimumSortingOrder < DefaultMinimumSortingOrder)
            minimumSortingOrder = DefaultMinimumSortingOrder;

        if (sortFactor <= 0f)
            sortFactor = 1f;
    }

    private void LateUpdate()
    {
        if (!isDynamic)
            return;

        UpdateSortingOrder();
    }

    public void UpdateSortingOrder()
    {
        if (onlySortWhenVisible && !_spriteRenderer.isVisible)
            return;

        float yContribution;

        if (useScreenSpaceSorting && sortingCamera != null)
        {
            // Screen space Y: bottom of the screen should be in front of the top.
            Vector3 screenPos = sortingCamera.WorldToScreenPoint(transform.position);
            yContribution = (sortingCamera.pixelHeight - screenPos.y) * sortFactor;
        }
        else
        {
            // Fallback to world Y based sorting.
            yContribution = -transform.position.y * sortFactor;
        }

        int order = minimumSortingOrder + sortingOffset + Mathf.RoundToInt(yContribution);
        _spriteRenderer.sortingOrder = order;
    }
}