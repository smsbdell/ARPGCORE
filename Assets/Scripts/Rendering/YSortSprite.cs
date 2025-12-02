using UnityEngine;

/// <summary>
/// Sorts a SpriteRenderer's order based on its world-space Y position so that lower objects
/// render in front of higher ones. The base sorting order comes from the SpriteRenderer's
/// existing value, preserving any manual layer separation between ground, decals, and actors.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSortSprite : MonoBehaviour
{
    private const int DefaultBaseLayerOffset = 100000;

    [Tooltip("If true, the sorting order will be updated every frame (for moving objects like the player). " +
             "If false, it will be set once on Start (for static props like trees/rocks).")]
    public bool isDynamic = true;

    [Tooltip("Offset applied to the computed sorting order. Use this to bump certain sprites in front/behind others.")]
    public int sortingOffset = 0;

    [Tooltip("Multiplier for converting world Y to sorting order. Higher values create wider spacing between orders.")]
    public float sortFactor = 100f;

    [Tooltip("Large positive offset added before applying Y-based sorting to keep orders away from zero/negative values.")]
    public int baseLayerOffset = DefaultBaseLayerOffset;

    [Tooltip("Base sorting order taken from the SpriteRenderer at startup. Use this to set the ground/character layer separation.")]
    public int baseSortingOrder = 0;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Capture any manually authored sorting order so designers can keep ground or decals on specific layers.
        if (baseSortingOrder == 0)
            baseSortingOrder = _spriteRenderer.sortingOrder;

        if (baseLayerOffset == 0)
            baseLayerOffset = DefaultBaseLayerOffset;

        UpdateSortingOrder();
    }

    private void Reset()
    {
        isDynamic = true;
        sortingOffset = 0;
        sortFactor = 100f;
        baseLayerOffset = DefaultBaseLayerOffset;
        baseSortingOrder = 0;
    }

    private void OnValidate()
    {
        if (sortFactor <= 0f)
            sortFactor = 100f;

        if (baseLayerOffset <= 0)
            baseLayerOffset = DefaultBaseLayerOffset;
    }

    private void LateUpdate()
    {
        if (!isDynamic)
            return;

        UpdateSortingOrder();
    }

    public void UpdateSortingOrder()
    {
        // Use the world-space transform position rather than renderer bounds so large ground
        // sprites do not gain artificial priority from their size. This keeps characters in
        // front of terrain regardless of sprite height while still sorting front-to-back
        // based on Y location.
        float yContribution = -transform.position.y * sortFactor;
        int order = baseLayerOffset + baseSortingOrder + sortingOffset + Mathf.RoundToInt(yContribution);
        _spriteRenderer.sortingOrder = order;
    }
}
