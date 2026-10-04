using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProgressResetter
{
    [MenuItem("Hexlink/Reset Level 1 Progress")]
    public static void ResetLevel1()
    {
        PlayerPrefs.DeleteKey("Hexlink.Complete.L-1");
        PlayerPrefs.DeleteKey("Hexlink.TutorialDone");
        PlayerPrefs.DeleteKey("Hexlink.Best.Instr.L-1");
        PlayerPrefs.DeleteKey("Hexlink.Best.Cycles.L-1");
        PlayerPrefs.DeleteKey("Hexlink.Best.Procs.L-1");
        PlayerPrefs.DeleteKey("Hexlink.Best.Sum.L-1");
        PlayerPrefs.Save();

        int completeAfter = PlayerPrefs.GetInt("Hexlink.Complete.L-1", -1);
        int tutorialAfter = PlayerPrefs.GetInt("Hexlink.TutorialDone", -1);
        Debug.Log($"ProgressResetter: post-delete Complete={completeAfter} TutorialDone={tutorialAfter}");

        string solutions = Path.Combine(Application.persistentDataPath, "solutions", "L-1.json");
        if (File.Exists(solutions))
        {
            File.Delete(solutions);
            Debug.Log("ProgressResetter: deleted L-1 solutions file.");
        }

        Debug.Log("ProgressResetter: Level 1 progress reset - the tutorial will play again.");
    }
}
