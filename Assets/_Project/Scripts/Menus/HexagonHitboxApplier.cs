using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class HexagonHitboxApplier
{
    private const float AlphaThreshold = 0.01f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Apply();
        Apply();
    }

    private static void Apply()
    {
        foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
        {
            if (image.sprite == null) continue;

            Texture2D texture = image.sprite.texture;
            if (texture == null || !texture.isReadable) continue;

            image.alphaHitTestMinimumThreshold = AlphaThreshold;
        }

        // Full-rect child graphics (especially stretched labels) override the parent
        // Image's alpha hit test - only the button's own graphic should receive raycasts.
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            Graphic buttonGraphic = button.GetComponent<Graphic>();
            Graphic targetGraphic = button.targetGraphic as Graphic;

            foreach (Graphic child in button.GetComponentsInChildren<Graphic>())
            {
                if (child == buttonGraphic || child == targetGraphic) continue;
                child.raycastTarget = false;
            }
        }

        foreach (TextMeshProUGUI tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
        {
            tmp.raycastTarget = false;
        }
    }
}
