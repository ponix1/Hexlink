// SceneTransition: fade-to-black scene changes. The load runs asynchronously behind
// the fade, and the scene only swaps once the overlay is fully opaque — the hitch
// that a normal LoadScene causes on the frame it activates is never on screen.
// The overlay canvas sits on its own child object that is deactivated between
// transitions, so it can never be picked up as a parent by runtime UI spawners.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    // Fade lengths. The in-fade overlaps the async load, so most scene changes
    // feel like nothing happened. ReducedMotion skips the theatre almost entirely.
    private const float FadeInDuration = 0.08f;
    private const float FadeOutDuration = 0.12f;
    private const float ReducedMotionDuration = 0.01f;

    // One instance for the whole session, created on first use. The root object
    // only ever holds this component; the canvas lives on the FadeOverlay child.
    private static SceneTransition instance;

    private GameObject overlayRoot;
    private Image overlayImage;

    // Entry point. Call this instead of SceneManager.LoadScene.
    public static void To(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (instance == null) CreateInstance();
        instance.StartCoroutine(instance.Transition(sceneName));
    }

    // Build the singleton plus its overlay entirely in code, so no scene has to
    // carry it and nothing needs wiring by hand in the Inspector.
    private static void CreateInstance()
    {
        GameObject root = new GameObject("SceneTransition");
        instance = root.AddComponent<SceneTransition>();
        DontDestroyOnLoad(root);

        instance.overlayRoot = new GameObject("FadeOverlay");
        instance.overlayRoot.transform.SetParent(root.transform, false);

        Canvas canvas = instance.overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        CanvasScaler scaler = instance.overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(instance.overlayRoot.transform, false);
        instance.overlayImage = imageObject.AddComponent<Image>();
        instance.overlayImage.color = Color.black;
        // Stretch to fill the canvas — a default RectTransform is only 100x100,
        // which fades a square in the middle of the screen instead of the view.
        RectTransform imageRT = imageObject.GetComponent<RectTransform>();
        imageRT.anchorMin = Vector2.zero;
        imageRT.anchorMax = Vector2.one;
        imageRT.offsetMin = Vector2.zero;
        imageRT.offsetMax = Vector2.zero;
        // Eats stray clicks while the fade is up so nothing can stack a second
        // transition (or hit buttons in the outgoing scene) mid-fade.
        instance.overlayImage.raycastTarget = true;
        instance.overlayRoot.SetActive(false);
    }

    private IEnumerator Transition(string sceneName)
    {
        // A second request while the overlay is up is almost certainly a double click.
        if (overlayRoot.activeSelf) yield break;
        overlayRoot.SetActive(true);

        float fadeIn = GameOptions.ReducedMotion ? ReducedMotionDuration : FadeInDuration;

        // Kick the load off straight away so it overlaps the fade-in. Activation is
        // held back until the overlay is opaque; progress stops at 0.9 while held.
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        if (load == null)
        {
            // Bad scene name — Unity already logged it. Recover rather than stick
            // the player behind a black screen.
            overlayRoot.SetActive(false);
            yield break;
        }
        load.allowSceneActivation = false;

        yield return Fade(0f, 1f, fadeIn);

        // Wait until loading is actually finished (0.9 == done-but-held).
        while (load.progress < 0.9f)
        {
            yield return null;
        }

        // Swap behind the fully black screen, then reveal the new scene.
        load.allowSceneActivation = true;
        yield return null;

        float fadeOut = GameOptions.ReducedMotion ? ReducedMotionDuration : FadeOutDuration;
        yield return Fade(1f, 0f, fadeOut);

        overlayRoot.SetActive(false);
    }

    // Simple alpha ramp using unscaled time so a paused game still fades cleanly.
    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            Color color = overlayImage.color;
            color.a = Mathf.Clamp01(Mathf.Lerp(from, to, time / duration));
            overlayImage.color = color;
            yield return null;
        }
        Color final = overlayImage.color;
        final.a = Mathf.Clamp01(to);
        overlayImage.color = final;
    }
}
