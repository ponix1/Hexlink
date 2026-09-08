using System.Collections;
using UnityEngine;
using TMPro;

public class NumberCircle : MonoBehaviour
{
    [SerializeField] private float valueFontSize = 50f;
    [SerializeField] private float labelHeightOffset = -0.285f;

    private Extruded3DText labelText;
    private HexCoord coord;
    private int value;
    private Vector3 targetScale;

    public HexCoord Coord => coord;
    public int Value => value;

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

    public void SetCoord(HexCoord newCoord)
    {
        coord = newCoord;
    }

    public void SetValue(int newValue)
    {
        value = newValue;
        if (labelText != null) labelText.SetText(value.ToString());
    }

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
