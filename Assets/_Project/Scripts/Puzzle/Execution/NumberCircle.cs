// NumberCircle — the numeric value token that sits on hexes during execution.
// Instantiated from the "Node" prefab at runtime; all motion/feedback is tweened
// by coroutines that ExecutionEngine drives.
using System.Collections;
using UnityEngine;
using TMPro;

// Pure visual token: holds a coord + value and a 3D text label. The engine decides
// where it moves; Spawn/MoveTo/pulse coroutines are animation only, no logic.
public class NumberCircle : MonoBehaviour
{
    [SerializeField] private float valueFontSize = 50f;
    [SerializeField] private float labelHeightOffset = -0.285f;

    private Extruded3DText labelText;
    // Logical board position, kept in sync with the engine's occupancy dict.
    private HexCoord coord;
    private int value;
    // Rest scale captured at Setup; every tween scales relative to it.
    private Vector3 targetScale;

    public HexCoord Coord => coord;
    public int Value => value;

    // Build the value label: strip the label prefab down to its extruded TMP text and
    // mount it in a counter-scale holder (details inline below).
    public void Setup(HexCoord startCoord, GameObject labelPrefab, float labelWorldScale)
    {
        coord = startCoord;
        targetScale = transform.localScale;

        Renderer discRenderer = GetComponentInChildren<Renderer>();
        Vector3 labelPosition = transform.position + Vector3.up * 0.08f;
        if (discRenderer != null)
        {
            labelPosition = discRenderer.bounds.center + Vector3.up * (discRenderer.bounds.extents.y + 0.02f);
        }
        labelPosition += Vector3.up * labelHeightOffset;

        GameObject labelObj = Instantiate(labelPrefab);
        labelText = labelObj.GetComponentInChildren<Extruded3DText>();

        // The label prefab is the full placed-tile visual (hexagon mesh + text).
        // Keep only the extruded text - destroy every renderer that isn't TMP-based.
        foreach (MeshRenderer meshRenderer in labelObj.GetComponentsInChildren<MeshRenderer>())
        {
            if (meshRenderer.GetComponent<TextMeshPro>() != null) continue;

            if (meshRenderer.gameObject == labelText.gameObject)
            {
                Destroy(meshRenderer);
                Destroy(meshRenderer.GetComponent<MeshFilter>());
            }
            else
            {
                Destroy(meshRenderer.gameObject);
            }
        }

        // The disc has non-uniform scale (thin Y); a rotated child under it would shear.
        // Cancel the disc's scale with an intermediate holder so the text renders uniformly.
        GameObject textHolder = new GameObject("ValueText");
        textHolder.transform.SetParent(transform, false);
        Vector3 discScale = transform.localScale;
        textHolder.transform.localScale = new Vector3(
            1f / Mathf.Max(discScale.x, 0.0001f),
            1f / Mathf.Max(discScale.y, 0.0001f),
            1f / Mathf.Max(discScale.z, 0.0001f));
        textHolder.transform.SetPositionAndRotation(labelPosition, Quaternion.identity);

        Transform textTransform = labelText.transform;
        textTransform.SetParent(textHolder.transform, false);
        textTransform.localPosition = Vector3.zero;
        textTransform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        textTransform.localScale = Vector3.one * labelWorldScale;

        foreach (TextMeshPro tmp in textTransform.GetComponentsInChildren<TextMeshPro>())
        {
            tmp.fontSize = valueFontSize;
        }

        if (labelObj != textTransform.gameObject)
        {
            Destroy(labelObj);
        }
    }

    // Logical position only — the visual move happens via MoveTo.
    public void SetCoord(HexCoord newCoord)
    {
        coord = newCoord;
    }

    public void SetValue(int newValue)
    {
        value = newValue;
        if (labelText != null) labelText.SetText(value.ToString());
    }

    // Pop-in: scale up from zero.
    public IEnumerator SpawnAnimation(float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            transform.localScale = targetScale * Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
    }

    // Smoothstep position tween to a world position.
    public IEnumerator MoveTo(Vector3 worldTarget, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            transform.position = Vector3.Lerp(start, worldTarget, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }

    // Brief bump when a merge result lands; ends back at rest scale.
    public IEnumerator MergePulse(float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float bump = 1f + 0.25f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = targetScale * bump;
            yield return null;
        }
        transform.localScale = targetScale;
    }

    // Triple-bounce celebration; deliberately ends 25% larger than rest scale.
    public IEnumerator WinPulse(float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float bump = 1f + 0.5f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f));
            transform.localScale = targetScale * bump;
            yield return null;
        }
        transform.localScale = targetScale * 1.25f;
    }
}
