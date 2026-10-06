// InfoTabMetrics - live "Instr / Cycles / Procs" counter label above the grid,
// refreshed from GridController.OnGridChanged.
using UnityEngine;
using TMPro;

public class InfoTabMetrics : MonoBehaviour
{
    [SerializeField] private GridController gridController;
    [SerializeField] private TextMeshProUGUI liveLabel;

    private void Start()
    {
        if (gridController != null)
        {
            gridController.OnGridChanged += Refresh;
        }
        Refresh();

        // Remove a "BestLabel" left over from an older scene layout.
        Transform stale = FindDeep(transform, "BestLabel");
        if (stale != null)
        {
            Destroy(stale.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (gridController != null)
        {
            gridController.OnGridChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (liveLabel == null || gridController == null) return;

        gridController.GetMetrics(out int instructions, out int cycles, out int processors);
        liveLabel.text = $"Instr {instructions}  \u00B7  Cycles {cycles}  \u00B7  Procs {processors}";
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
