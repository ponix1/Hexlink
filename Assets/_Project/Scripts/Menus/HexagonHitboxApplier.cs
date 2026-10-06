// HexagonHitboxApplier: scene-wide hexagon hitboxes - on every scene load, alpha-tests
// every readable sprite Image (transparent corners stop blocking clicks) and strips
// raycast targets from button child graphics and TMP text (past-bug notes inside Apply).

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class HexagonHitboxApplier
{
    // Just above zero: only (near-)fully transparent pixels are ignored.
    private const float AlphaThreshold = 0.01f;

    // Runtime-spawner pattern: UI is built at runtime, so re-run after each scene load.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Apply();
        Apply();
    }

    // Pass 1: threshold every readable sprite - the alpha test needs CPU pixel access,
    // so unreadable textures are skipped.
    private static void Apply()
    {
        foreach (Image image in Object.FindObjectsByType<Image>())
        {
            if (image.sprite == null) continue;

            Texture2D texture = image.sprite.texture;
            if (texture == null || !texture.isReadable) continue;

            image.alphaHitTestMinimumThreshold = AlphaThreshold;
        }

        // Full-rect child graphics (especially stretched labels) override the parent
        // Image's alpha hit test - only the button's own graphic should receive raycasts.
        foreach (Button button in Object.FindObjectsByType<Button>())
        {
            Graphic buttonGraphic = button.GetComponent<Graphic>();
            Graphic targetGraphic = button.targetGraphic as Graphic;

            foreach (Graphic child in button.GetComponentsInChildren<Graphic>())
            {
                if (child == buttonGraphic || child == targetGraphic) continue;
                child.raycastTarget = false;
            }
        }

        // Stretched full-rect TMP labels formed invisible rectangle hitboxes over the
        // hexagon buttons, bypassing the parent Image's alpha test - so no TMP raycasts.
        foreach (TextMeshProUGUI tmp in Object.FindObjectsByType<TextMeshProUGUI>())
        {
            tmp.raycastTarget = false;
        }
    }
}
