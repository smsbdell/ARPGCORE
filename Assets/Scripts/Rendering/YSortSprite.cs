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

    [Tooltip("Multiplier for converting world Y to sorting order. Higher value = finer separation.")]
    public float sortFactor = 100f;

    [Tooltip("Clamp so the computed order never goes below this value (useful to keep sprites above ground).")]
    public int minimumSortingOrder = 1000;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSortingOrder();
    }

    private void LateUpdate()
    {
        if (!isDynamic)
            return;

        UpdateSortingOrder();
    }

    public void UpdateSortingOrder()
    {
        // Lower Y => higher order (in front), Higher Y => lower order (behind)
        int order = sortingOffset + Mathf.RoundToInt(-transform.position.y * sortFactor);
        order = Mathf.Max(order, minimumSortingOrder);
        _spriteRenderer.sortingOrder = order;
    }
}