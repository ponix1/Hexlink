using UnityEngine;
using TMPro;

// Fakes extruded 3D text by stacking copies of a TextMeshPro layer along +z,
// giving tile numbers physical depth. The authored frontText is the template.
public class Extruded3DText : MonoBehaviour
{
    // The authored TMP object; also the source of the text for all copies.
    [SerializeField] private TextMeshPro frontText;
    // Extrusion thickness = layerCount * layerDepth.
    [SerializeField] private int layerCount = 15;
    [SerializeField] private float layerDepth = 0.03f;
    [SerializeField] private Color frontColor = Color.white;
    [SerializeField] private Color backColor = new Color(0.3f, 0.3f, 0.3f);

    // Cached copies so SetText can update the whole stack at once.
    private TextMeshPro[] backLayers;

    private void Awake()
    {
        BuildLayers();
    }

    // Awake builds the stack: each copy is a sibling of frontText, pushed further
    // along +z and otherwise sharing its local transform.
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
            // NOTE: t is unused and this assignment is a no-op — the intended
            // backColor→frontColor gradient across layers is not applied yet.
            layerTmp.color = layerTmp.color;

            backLayers[i] = layerTmp;
        }

        frontText.color = frontColor;
    }

    // Set the text on the front layer and every extrusion copy together.
    public void SetText(string text)
    {
        frontText.text = text;
        foreach (TextMeshPro layer in backLayers)
        {
            layer.text = text;
        }
    }
}