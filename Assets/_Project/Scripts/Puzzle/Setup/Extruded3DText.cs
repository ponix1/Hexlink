using UnityEngine;
using TMPro;

public class Extruded3DText : MonoBehaviour
{
    [SerializeField] private TextMeshPro frontText;
    [SerializeField] private int layerCount = 5;
    [SerializeField] private float layerDepth = 0.03f;
    [SerializeField] private Color frontColor = Color.white;
    [SerializeField] private Color backColor = new Color(0.3f, 0.3f, 0.3f);

    private TextMeshPro[] backLayers;

    private void Awake()
    {
        BuildLayers();
    }

    private void BuildLayers()
    {
        backLayers = new TextMeshPro[layerCount];

        for (int i = 0; i < layerCount; i++)
        {
            GameObject layerObj = Instantiate(frontText.gameObject, frontText.transform.parent);
            layerObj.name = $"TextLayer_{i}";

            float depthOffset = layerDepth * (i + 1);
            layerObj.transform.localPosition = frontText.transform.localPosition + new Vector3(0f, 0f, depthOffset);
            layerObj.transform.localRotation = frontText.transform.localRotation;
            layerObj.transform.localScale = frontText.transform.localScale;

            TextMeshPro layerTmp = layerObj.GetComponent<TextMeshPro>();
            float t = (i + 1) / (float)layerCount;
            layerTmp.color = layerTmp.color;

            backLayers[i] = layerTmp;
        }

        frontText.color = frontColor;
    }

    public void SetText(string text)
    {
        frontText.text = text;
        foreach (TextMeshPro layer in backLayers)
        {
            layer.text = text;
        }
    }
}