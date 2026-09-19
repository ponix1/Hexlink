using UnityEngine;
using TMPro;

public class InfoTabMetrics : MonoBehaviour
{
    [SerializeField] private GridController gridController;
    [SerializeField] private TextMeshProUGUI liveLabel;
    [SerializeField] private TextMeshProUGUI bestLabel;

    private void Start()
    {
        if (gridController != null)
        {
            gridController.OnGridChanged += Refresh;
        }
        Refresh();
        RefreshBests();
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

    public void RecordWin()
    {
        if (gridController == null || PuzzleSelection.SelectedPuzzle == null) return;

        gridController.GetMetrics(out int instructions, out int cycles, out int processors);
        PuzzleRecords.Submit(PuzzleSelection.SelectedPuzzle.puzzleID, instructions, cycles, processors);
        RefreshBests();
    }

    private void RefreshBests()
    {
        if (bestLabel == null) return;

        if (PuzzleSelection.SelectedPuzzle == null)
        {
            bestLabel.text = "";
            return;
        }

        PuzzleRecords.TryGet(PuzzleSelection.SelectedPuzzle.puzzleID, out int instructions, out int cycles, out int processors);
        bestLabel.text = $"Best {Format(instructions)}  \u00B7  {Format(cycles)}  \u00B7  {Format(processors)}";
    }

    private static string Format(int value)
    {
        return value == int.MaxValue ? "\u2014" : value.ToString();
    }
}
