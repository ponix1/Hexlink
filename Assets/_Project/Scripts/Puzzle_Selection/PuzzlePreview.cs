using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PuzzlePreview
{
    private const string SceneName = "Puzzle_Select";
    private const float HexSize = 1f;
    private const float SpacingMultiplier = 1.06f;
    private static readonly Vector3 PreviewOffset = new Vector3(1000f, 0f, 0f);

    private static Transform previewRoot;
    private static Camera previewCamera;
    private static RawImage previewImage;
    private static readonly List<GameObject> spawnedTiles = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Setup();
        Setup();
    }

    private static void Setup()
    {
        if (SceneManager.GetActiveScene().name != SceneName) return;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
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
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.fieldOfView = 45f;
        cameraObj.AddComponent<PuzzlePreviewRotator>();

        Hide();
    }

    public static void Show(PuzzleData puzzle)
    {
        if (previewImage == null || puzzle == null) return;
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
        float scaleFactor = (2f * HexSize) / rawPointToPoint;
        float spacing = HexSize * SpacingMultiplier;

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (HexCellData cell in puzzle.LayoutCells)
        {
            Vector3 pos = cell.coordinate.ToWorldPosition(spacing);
            GameObject tile = Object.Instantiate(prefab, pos + PreviewOffset, prefab.transform.rotation);
            tile.transform.localScale = Vector3.one * scaleFactor;
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

        Vector3 center = (min + max) * 0.5f + PreviewOffset;
        float extent = Mathf.Max(max.x - min.x, max.z - min.z) + 3f * spacing;
        PuzzlePreviewRotator rotator = previewCamera.GetComponent<PuzzlePreviewRotator>();
        rotator.SetTarget(center, extent * 1.4f);
    }

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

public class PuzzlePreviewRotator : MonoBehaviour
{
    private const float YawSpeed = 12f;
    private const float Pitch = 55f;

    private Vector3 center;
    private float distance = 10f;
    private float yaw = 30f;

    public void SetTarget(Vector3 orbitCenter, float orbitDistance)
    {
        if (float.IsNaN(orbitCenter.x) || float.IsNaN(orbitDistance) || orbitDistance <= 0f) return;
        center = orbitCenter;
        distance = orbitDistance;
    }

    private void Update()
    {
        if (float.IsNaN(center.x) || float.IsNaN(distance)) return;

        yaw += YawSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(Pitch, yaw, 0f);
        transform.position = center - transform.forward * distance;
    }
}
