using UnityEngine;

[System.Serializable]
public class MapObjectSpawnRule
{
    [Tooltip("Label for this rule (e.g. Grass, Shrub, Tree, Ruin).")]
    public string name;

    [Tooltip("One of these prefabs will be chosen at random when this rule spawns.")]
    public GameObject[] prefabs;

    [Tooltip("Chance per tile that this rule will spawn (before density multiplier).")]
    [Range(0f, 1f)] public float spawnChance = 0.1f;

    [Tooltip("If true, spawn near the tile center with small jitter; if false, full-tile random position.")]
    public bool alignToTileCenter = true;

    [Tooltip("Maximum +/- jitter applied on X/Y around the chosen position.")]
    public Vector2 positionJitter = new Vector2(0.15f, 0.15f);

    [Tooltip("Random uniform scale between these values.")]
    public float minScale = 1f;
    public float maxScale = 1f;

    [Tooltip("Randomize Z rotation (for top-down sprites).")]
    public bool randomRotation = false;
}

public class MapGenerator : MonoBehaviour
{
    [Header("Ground")]
    [Tooltip("Prefab with a SpriteRenderer for the ground tile (e.g., 1x1 world units).")]
    public GameObject groundTilePrefab;

    [Tooltip("Number of tiles horizontally.")]
    public int width = 50;

    [Tooltip("Number of tiles vertically.")]
    public int height = 50;

    [Tooltip("World size of each tile (assuming the sprite is 1x1 units by default).")]
    public float tileSize = 1f;

    [Tooltip("If true, the map will be centered around (0,0). If false, it starts at (0,0) and grows positive.")]
    public bool centerOnZero = true;

    [Tooltip("Sorting order used for ground tiles (should be lower than Y-sorted objects).")]
    public int groundSortingOrder = -1000;

    [Header("Boundaries")]
    [Tooltip("Create simple box colliders around the edges so the player can't walk off the map.")]
    public bool createBoundaryColliders = true;

    [Tooltip("Thickness of the boundary colliders.")]
    public float boundaryThickness = 1f;

    [Tooltip("Layer index for the boundary colliders (optional).")]
    public int boundaryLayer = 0;

    [Header("Object Spawning")]
    [Tooltip("Enable random placement of objects (grass, shrubs, trees, ruins, etc.).")]
    public bool spawnObjects = true;

    [Tooltip("Global multiplier applied to each rule's spawnChance.")]
    [Range(0f, 5f)] public float objectDensityMultiplier = 1f;

    [Tooltip("No objects will be spawned within this radius of (0,0). Useful for keeping the player start clear.")]
    public float clearRadiusAroundOrigin = 3f;

    [Tooltip("Spawn rules for decorative / obstacle objects.")]
    public MapObjectSpawnRule[] objectSpawnRules;

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (groundTilePrefab == null)
        {
            Debug.LogError("MapGenerator: groundTilePrefab is not assigned.");
            return;
        }

        // Clear existing children if you regenerate
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        Vector2 originOffset = Vector2.zero;

        if (centerOnZero)
        {
            float totalWidth = width * tileSize;
            float totalHeight = height * tileSize;
            originOffset = new Vector2(-totalWidth * 0.5f + tileSize * 0.5f,
                                       -totalHeight * 0.5f + tileSize * 0.5f);
        }

        // Spawn ground tiles and (optionally) objects
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 tilePosition = new Vector3(
                    originOffset.x + x * tileSize,
                    originOffset.y + y * tileSize,
                    0f
                );

                GameObject tile = Instantiate(groundTilePrefab, tilePosition, Quaternion.identity, transform);
                tile.name = $"Ground_{x}_{y}";

                // Force ground tiles behind everything else (no Y-sort for ground)
                var srs = tile.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in srs)
                {
                    sr.sortingOrder = groundSortingOrder;
                }

                if (spawnObjects)
                {
                    TrySpawnObjectsAt(tilePosition);
                }
            }
        }

        if (createBoundaryColliders)
        {
            CreateBoundaries(originOffset);
        }
    }

    private void TrySpawnObjectsAt(Vector3 tileCenter)
    {
        if (objectSpawnRules == null || objectSpawnRules.Length == 0)
            return;

        // Optionally keep an area around (0,0) empty
        if (clearRadiusAroundOrigin > 0f)
        {
            Vector2 pos2D = new Vector2(tileCenter.x, tileCenter.y);
            if (pos2D.magnitude < clearRadiusAroundOrigin)
                return;
        }

        for (int ruleIndex = 0; ruleIndex < objectSpawnRules.Length; ruleIndex++)
        {
            var rule = objectSpawnRules[ruleIndex];
            if (rule == null || rule.prefabs == null || rule.prefabs.Length == 0)
                continue;

            float effectiveChance = rule.spawnChance * objectDensityMultiplier;
            if (effectiveChance <= 0f)
                continue;

            if (Random.value > effectiveChance)
                continue;

            // Choose a prefab
            GameObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
            if (prefab == null)
                continue;

            // Determine spawn position
            Vector3 spawnPos = tileCenter;

            if (rule.alignToTileCenter)
            {
                spawnPos += new Vector3(
                    Random.Range(-rule.positionJitter.x, rule.positionJitter.x),
                    Random.Range(-rule.positionJitter.y, rule.positionJitter.y),
                    0f
                );
            }
            else
            {
                // Full-tile random position
                float half = tileSize * 0.5f;
                spawnPos += new Vector3(
                    Random.Range(-half, half),
                    Random.Range(-half, half),
                    0f
                );
            }

            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

            // Ensure non-zero scale (defensive clamp)
            float min = Mathf.Max(0.01f, rule.minScale);
            float max = Mathf.Max(min, rule.maxScale);
            float scale = Random.Range(min, max);
            obj.transform.localScale = new Vector3(scale, scale, 1f);

            // Optional random rotation (top-down z-axis)
            if (rule.randomRotation)
            {
                float angle = Random.Range(0f, 360f);
                obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // Note: we do NOT touch sortingOrder here. YSortSprite on the prefab (if present)
            // will control render order for props, same as player/monsters.
        }
    }

    private void CreateBoundaries(Vector2 originOffset)
    {
        float totalWidth = width * tileSize;
        float totalHeight = height * tileSize;

        float left = originOffset.x - tileSize * 0.5f;
        float right = originOffset.x + totalWidth - tileSize * 0.5f;
        float bottom = originOffset.y - tileSize * 0.5f;
        float top = originOffset.y + totalHeight - tileSize * 0.5f;

        void CreateBoundary(string name, Vector2 center, Vector2 size)
        {
            GameObject boundary = new GameObject(name);
            boundary.transform.SetParent(transform, false);
            boundary.transform.position = center;

            BoxCollider2D col = boundary.AddComponent<BoxCollider2D>();
            col.size = size;

            boundary.layer = boundaryLayer;
        }

        // Bottom
        CreateBoundary(
            "Boundary_Bottom",
            new Vector2((left + right) * 0.5f, bottom - boundaryThickness * 0.5f),
            new Vector2(totalWidth + 2f * boundaryThickness, boundaryThickness)
        );

        // Top
        CreateBoundary(
            "Boundary_Top",
            new Vector2((left + right) * 0.5f, top + boundaryThickness * 0.5f),
            new Vector2(totalWidth + 2f * boundaryThickness, boundaryThickness)
        );

        // Left
        CreateBoundary(
            "Boundary_Left",
            new Vector2(left - boundaryThickness * 0.5f, (bottom + top) * 0.5f),
            new Vector2(boundaryThickness, totalHeight + 2f * boundaryThickness)
        );

        // Right
        CreateBoundary(
            "Boundary_Right",
            new Vector2(right + boundaryThickness * 0.5f, (bottom + top) * 0.5f),
            new Vector2(boundaryThickness, totalHeight + 2f * boundaryThickness)
        );
    }
}