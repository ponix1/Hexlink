// Live 3D preview for Puzzle_Select: spawns the real hex tiles offscreen for
// the hovered puzzle and renders them, via a private orbiting camera, into a
// right-half RawImage with a transparent clear over the scene backdrop.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PuzzlePreview
{
    // Only this scene gets a preview.
    private const string SceneName = "Puzzle_Select";
    // Same hex metrics as HexGridSpawner so the preview matches the real board.
    private const float HexSize = 1f;
    private const float SpacingMultiplier = 1.06f;
    // Parks the preview +1000 on X, far away from the live scene geometry.
    private static readonly Vector3 PreviewOffset = new Vector3(1000f, 0f, 0f);

    // Offscreen parent for the preview camera and the current tiles.
    private static Transform previewRoot;
    // Renders only the preview tiles into the RawImage's texture.
    private static Camera previewCamera;
    // Right-half RawImage; alpha 0 hides it, white shows it.
    private static RawImage previewImage;
    // Current preview tiles; cleared and rebuilt on every Show/Hide.
    private static readonly List<GameObject> spawnedTiles = new List<GameObject>();

    // Hook every scene load so re-entering Puzzle_Select rebuilds the preview.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Setup();
        Setup();
    }

    // Build panel/texture/camera once per Puzzle_Select entry; no-op elsewhere.
    private static void Setup()
    {
        if (SceneManager.GetActiveScene().name != SceneName) return;

        Canvas canvas = UIRoot.FindSceneCanvas();
        if (canvas == null) return;
        if (canvas.transform.Find("PuzzlePreview") != null) return;

        RenderTexture texture = new RenderTexture(1024, 1024, 24);

        GameObject panel = new GameObject("PuzzlePreview", typeof(RectTransform), typeof(RawImage));
        panel.transform.SetParent(canvas.transform, false);
        previewImage = panel.GetComponent<RawImage>();
        previewImage.texture = texture;
        previewImage.raycastTarget = false;
        previewImage.color = new Color(1f, 1f, 1f, 0f);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        // Cover the right half of the canvas.
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(1f, 1f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        previewRoot = new GameObject("PuzzlePreviewRoot").transform;
        previewRoot.position = PreviewOffset;

        GameObject cameraObj = new GameObject("PuzzlePreviewCamera");
        cameraObj.transform.SetParent(previewRoot, false);
        previewCamera = cameraObj.AddComponent<Camera>();
        previewCamera.targetTexture = texture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        // Transparent clear: the scene backdrop shows through the preview.
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.fieldOfView = 45f;
        cameraObj.AddComponent<PuzzlePreviewRotator>();

        // Start hidden until a card is hovered.
        Hide();
    }

    // Rebuild the tile layout for the hovered puzzle and aim the camera at it.
    public static void Show(PuzzleData puzzle)
    {
        if (previewImage == null || puzzle == null) return;
        // Empty layout: keep the previous preview instead of showing nothing.
        if (puzzle.LayoutCells == null || puzzle.LayoutCells.Count == 0)
        {
            Debug.LogWarning($"PuzzlePreview: '{puzzle.puzzleTitle}' has no layout cells - keeping previous preview.");
            return;
        }

        foreach (GameObject tile in spawnedTiles)
        {
            Object.Destroy(tile);
        }
        spawnedTiles.Clear();

        GameObject prefab = Resources.Load<GameObject>("Pentagon 1");
        if (prefab == null)
        {
            Debug.LogError("PuzzlePreview: 'Pentagon 1' prefab not found in Resources.");
            return;
        }

        MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>();
        float rawPointToPoint = Mathf.Max(meshFilter.sharedMesh.bounds.size.x, meshFilter.sharedMesh.bounds.size.y);
        // Scale so the mesh's point-to-point width equals the in-game hex size.
        float scaleFactor = (2f * HexSize) / rawPointToPoint;
        float spacing = HexSize * SpacingMultiplier;

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (HexCellData cell in puzzle.LayoutCells)
        {
            Vector3 pos = cell.coordinate.ToWorldPosition(spacing);
            GameObject tile = Object.Instantiate(prefab, pos + PreviewOffset, prefab.transform.rotation);
            tile.transform.localScale = Vector3.one * scaleFactor;
            // Strip colliders: offscreen tiles never need physics.
            foreach (Collider collider in tile.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(collider);
            }
            tile.transform.SetParent(previewRoot, true);
            spawnedTiles.Add(tile);

            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        previewImage.color = Color.white;

        // Frame the layout: orbit its center at a distance from its extent.
        Vector3 center = (min + max) * 0.5f + PreviewOffset;
        float extent = Mathf.Max(max.x - min.x, max.z - min.z) + 3f * spacing;
        PuzzlePreviewRotator rotator = previewCamera.GetComponent<PuzzlePreviewRotator>();
        rotator.SetTarget(center, extent * 1.4f);
    }

    // Fade the image out and clear the tiles.
    public static void Hide()
    {
        if (previewImage == null) return;
        previewImage.color = new Color(1f, 1f, 1f, 0f);

        foreach (GameObject tile in spawnedTiles)
        {
            Object.Destroy(tile);
        }
        spawnedTiles.Clear();
    }
}

// Orbits the preview camera around the layout: fixed pitch, ever-increasing yaw.
public class PuzzlePreviewRotator : MonoBehaviour
{
    private const float YawSpeed = 12f;
    private const float Pitch = 55f;

    private Vector3 center;
    private float distance = 10f;
    private float yaw = 30f;

    // Ignore invalid targets (NaN / non-positive) instead of breaking the orbit.
    public void SetTarget(Vector3 orbitCenter, float orbitDistance)
    {
        if (float.IsNaN(orbitCenter.x) || float.IsNaN(orbitDistance) || orbitDistance <= 0f) return;
        center = orbitCenter;
        distance = orbitDistance;
    }

    // Keep a fixed pitch while yaw spins; stay on the orbit sphere.
    private void Update()
    {
        if (float.IsNaN(center.x) || float.IsNaN(distance)) return;

        yaw += YawSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(Pitch, yaw, 0f);
        transform.position = center - transform.forward * distance;
    }
}
