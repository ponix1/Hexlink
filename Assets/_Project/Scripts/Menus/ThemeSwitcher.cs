// ThemeSwitcher: applies the active design (Dark / Light / Glass) to all UI at runtime.
// Recolours every Graphic through the HexlinkTheme palette; in Glass mode additionally
// converts panels/faces/buttons into translucent frosted surfaces with configurable
// outlines (fully reversible via GlassMark).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Remembers a graphic's pre-glass state so the outline conversion is fully
// reversible when switching away from the Glass design.
public class GlassMark : MonoBehaviour
{
    public Color originalColor;
    public float originalOutline = -1f;
    public GameObject createdOutline;
    public GameObject createdBackdrop;
    public Material originalMaterial;
    public bool deactivateSelf;
    public GameObject deactivatedSibling;
}

public static class ThemeSwitcher
{
    // The Transparent design's outline colour and opacity are user-configurable
    // in Options.
    private static Color GlassOutline
    {
        get
        {
            Color color = GameOptions.GlassOutlineColor;
            return new Color(color.r, color.g, color.b, GameOptions.GlassOutlineAlpha);
        }
    }
    // Dark, colourless frost: the blurred sky shows through with a slight
    // darkening for text readability - no white/grey wash at all.
    private static readonly Color FrostTint = new Color(0f, 0f, 0f, 0.35f);
    // Not fully zero: UI Masks cut children where the mask graphic's alpha is
    // 0 and HexagonHitboxApplier alpha-tests at 0.01 - hidden layers keep a
    // sliver of alpha so masks and clicks keep working.
    private static readonly Color Hidden = new Color(1f, 1f, 1f, 0.02f);

    // Runtime-spawner pattern: re-apply per scene load so spawned UI is always themed.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => ApplyToScene();
        ApplyToScene();
    }

    // Full-scene pipeline: toggle blur, undo any previous glass conversion, recolour
    // every graphic + button tint, then re-convert to glass if theme 2 is active.
    public static void ApplyToScene()
    {
        int theme = GameOptions.ThemeIndex;

        if (theme == 2)
        {
            BlurBackdrop.Refresh();
        }
        else
        {
            BlurBackdrop.Shutdown();
        }

        RestoreGlass(Object.FindObjectsByType<GlassMark>(FindObjectsInactive.Include));

        Graphic[] graphics = Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include);
        foreach (Graphic graphic in graphics)
        {
            if (HexlinkTheme.TryRemap(graphic.color, theme, out Color remapped))
            {
                graphic.color = remapped;
            }
        }

        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (button.transition == Button.Transition.ColorTint)
            {
                HexlinkTheme.ApplyHoverTint(button);
            }
        }

        if (theme == 2)
        {
            ConvertToGlass(graphics);
        }

        ColourBlindApplier.ApplyToScene();
    }

    // Same pipeline scoped to one subtree (e.g. the runtime-built Options screen).
    public static void ApplyToSubtree(Transform root)
    {
        if (root == null) return;

        int theme = GameOptions.ThemeIndex;

        // Only restore marks inside this subtree - a spawned card must never
        // revert the glass conversion of the rest of the scene.
        RestoreGlass(root.GetComponentsInChildren<GlassMark>(true));

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (HexlinkTheme.TryRemap(graphic.color, theme, out Color remapped))
            {
                graphic.color = remapped;
            }
        }

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.transition == Button.Transition.ColorTint)
            {
                HexlinkTheme.ApplyHoverTint(button);
            }
        }

        if (theme == 2)
        {
            ConvertToGlass(graphics);
        }
    }

    private static void RestoreGlass(GlassMark[] marks)
    {
        foreach (GlassMark mark in marks)
        {
            Graphic graphic = mark.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = mark.originalColor;
                if (mark.originalOutline >= 0f)
                {
                    if (graphic is ChamferedImage chamfer) chamfer.OutlineThickness = mark.originalOutline;
                    if (graphic is HexagonImage hex) hex.OutlineThickness = mark.originalOutline;
                }
                graphic.material = mark.originalMaterial;
                if (mark.deactivateSelf) graphic.gameObject.SetActive(true);
                if (mark.deactivatedSibling != null) mark.deactivatedSibling.SetActive(true);
            }
            if (mark.createdBackdrop != null) Object.DestroyImmediate(mark.createdBackdrop);
            if (mark.createdOutline != null) Object.DestroyImmediate(mark.createdOutline);
            // Immediate destroy matters: ApplyToScene re-converts in the same
            // frame, and a still-pending mark would make the converter skip
            // the graphic, leaving it stuck in the restored (non-glass) state.
            Object.DestroyImmediate(mark);
        }
    }

    // Glass = literally just outlines. Baked button stacks (ring + bevel +
    // face) are converted: redundant layers hidden, faces and buttons become
    // thin outlines (hexagon rings for sprite hexes, chamfered outlines
    // otherwise).
    private static void ConvertToGlass(IEnumerable<Graphic> graphics)
    {
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || graphic.GetComponent<GlassMark>() != null) continue;

            // The puzzle grid repaints its cells every state change, so only
            // the cell area under GridController.gridContent self-themes via
            // the palette - the rest of the InfoTab converts normally.
            GridController grid = graphic.GetComponentInParent<GridController>();
            if (grid != null && grid.GridContentRoot != null
                && graphic.transform.IsChildOf(grid.GridContentRoot))
            {
                continue;
            }

            string name = graphic.gameObject.name;
            Transform parent = graphic.transform.parent;
            bool hasRingSibling = parent != null && parent.Find("Ring") != null;

            bool isBevelLayer = name == "Bevel";
            // Button stack roots carry a Mask over their click surface - they
            // must stay (near-)invisible but present. Detected by their Face
            // child (Bevel children no longer exist - bevels were removed).
            bool isStackRoot = graphic.transform.Find("Bevel") != null
                || (graphic.transform.Find("Face") != null && graphic.GetComponent<Mask>() != null);
            bool isFace = (name == "Face" || name == "Fill") && !hasRingSibling;

            if (isBevelLayer || isStackRoot)
            {
                GlassMark mark = Mark(graphic);
                if (isBevelLayer)
                {
                    // Bevels only exist for the Dark/Light designs - in Glass
                    // they are deactivated outright so nothing of them shows.
                    mark.deactivateSelf = true;
                    graphic.gameObject.SetActive(false);
                }
                else
                {
                    // Stack roots carry Masks over their click surface - they
                    // stay active but (near-)invisible.
                    graphic.color = Hidden;
                }
                continue;
            }

            if (isFace)
            {
                ConvertFaceToFrost(graphic);
                continue;
            }

            // Panel surfaces become frosted glass: a RawImage child showing the
            // heavily downsampled scene capture, screen-aligned, tinted dark.
            // No custom shader - if an image can render, this renders.
            if (SameColor(graphic.color, HexlinkTheme.Glass.Panel))
            {
                bool strongBlur = IsInsideNamed(graphic.transform, "WinPopup");
                GlassMark blurMark = Mark(graphic);
                blurMark.createdBackdrop = AttachBackdrop(graphic, FrostTint, strongBlur);
                graphic.color = Hidden;
                continue;
            }

            // Every button surface becomes frosted glass with an outline in
            // the user-configured colour and opacity.
            Button button = graphic.GetComponent<Button>();
            if (button != null && button.targetGraphic == graphic)
            {
                if (SameColor(graphic.color, HexlinkTheme.Glass.Ghost)
                    || SameColor(graphic.color, HexlinkTheme.Glass.Accent))
                {
                    ConvertButtonToFrost(graphic, GlassOutline);
                    continue;
                }
            }

            // Border, frame and divider surfaces join the same outline
            // language: chamfered ones flip to outline mode in the configured
            // colour, thin ones (divider lines, tracks) are recoloured, and
            // other plain ones get an outline child in place of their fill.
            if (IsBorderColored(graphic))
            {
                if (graphic is ChamferedImage borderChamfer)
                {
                    if (borderChamfer.OutlineThickness == 0f)
                    {
                        GlassMark mark = Mark(graphic);
                        mark.originalOutline = 0f;
                        borderChamfer.OutlineThickness = 4f;
                        graphic.color = GlassOutline;
                    }
                }
                else if (IsThin(graphic))
                {
                    Mark(graphic);
                    graphic.color = GlassOutline;
                }
                else if (graphic is Image plainImage)
                {
                    GlassMark mark = Mark(graphic);
                    mark.createdOutline = CreateOutlineChild(plainImage, plainImage.sprite != null, GlassOutline, 10f);
                    graphic.color = Hidden;
                }
            }
        }
    }

    private static GlassMark Mark(Graphic graphic)
    {
        GlassMark mark = graphic.gameObject.AddComponent<GlassMark>();
        mark.originalColor = graphic.color;
        mark.originalOutline = -1f;
        return mark;
    }

    // Sprite hex faces (main menu buttons): the bevel and face are deleted,
    // leaving the button surface at opacity 2, with a hexagon outline ring
    // promoted to the button root and scaled up. Chamfered faces (cards,
    // panel buttons) become frosted glass tinted with the configured outline
    // colour - puzzle cards at full opacity, everything else at 9%.
    private static void ConvertFaceToFrost(Graphic graphic)
    {
        Image image = graphic as Image;
        if (image == null) return;

        if (image.sprite != null)
        {
            Transform root = image.transform.parent;
            if (root == null) return;

            GlassMark mark = Mark(graphic);
            mark.deactivateSelf = true;

            Transform bevel = root.Find("Bevel");
            if (bevel != null)
            {
                mark.deactivatedSibling = bevel.gameObject;
                bevel.gameObject.SetActive(false);
            }

            // The ring lives outside the masked button (a mask would clip the
            // scaled-up outline), as a sibling matching the button's rect.
            GameObject outline = CreateOutlineChild(image, true, GlassOutline, 10f);
            if (root.parent != null)
            {
                outline.transform.SetParent(root.parent, false);
                if (root is RectTransform rootRT)
                {
                    RectTransform outlineRT = outline.GetComponent<RectTransform>();
                    outlineRT.anchorMin = rootRT.anchorMin;
                    outlineRT.anchorMax = rootRT.anchorMax;
                    outlineRT.pivot = rootRT.pivot;
                    outlineRT.anchoredPosition = rootRT.anchoredPosition;
                    outlineRT.sizeDelta = rootRT.sizeDelta;
                }
            }
            else
            {
                outline.transform.SetParent(root, false);
            }
            outline.transform.localScale = Vector3.one * 1.185f;
            outline.transform.SetAsLastSibling();
            mark.createdOutline = outline;

            image.gameObject.SetActive(false);
            return;
        }

        bool isCard = image.GetComponentInParent<PuzzleCardView>() != null;
        Color faceTint = isCard ? ConfiguredTint(1f) : ConfiguredTint(0.15f);

        GlassMark chamferMark = Mark(graphic);
        chamferMark.createdBackdrop = AttachBackdrop(graphic, faceTint, false);
        chamferMark.createdOutline = CreateOutlineChild(image, false, GlassOutline, GetChamfer(image));
        graphic.color = Hidden;
    }

    // A RawImage displaying the downsampled scene capture, stretched over the
    // surface and offset so it shows exactly the part of the scene behind it.
    // The tint darkens/colours the blurred backdrop for readability.
    private static GameObject AttachBackdrop(Graphic surface, Color tint, bool strong)
    {
        RenderTexture capture = BlurBackdrop.GetTexture(strong);
        if (capture == null) return null;

        GameObject backdropGO = new GameObject("GlassBackdrop", typeof(RectTransform), typeof(RawImage));
        backdropGO.transform.SetParent(surface.transform, false);
        RectTransform rt = backdropGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        backdropGO.transform.SetAsFirstSibling();

        RawImage raw = backdropGO.GetComponent<RawImage>();
        raw.texture = capture;
        raw.color = tint;
        raw.raycastTarget = false;
        raw.uvRect = ScreenRect(surface);
        return backdropGO;
    }

    // The surface's rect in 0..1 screen UV space, matching the camera capture.
    private static Rect ScreenRect(Graphic graphic)
    {
        Canvas canvas = graphic.canvas;
        if (canvas == null) return new Rect(0f, 0f, 1f, 1f);

        Vector3[] corners = new Vector3[4];
        graphic.rectTransform.GetWorldCorners(corners);
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in corners)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(cam, corner);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        Rect pixel = canvas.pixelRect;
        if (pixel.width <= 0f || pixel.height <= 0f) return new Rect(0f, 0f, 1f, 1f);

        return new Rect(
            min.x / pixel.width,
            min.y / pixel.height,
            (max.x - min.x) / pixel.width,
            (max.y - min.y) / pixel.height);
    }

    private static Color ConfiguredTint(float alpha)
    {
        Color color = GameOptions.GlassOutlineColor;
        return new Color(color.r, color.g, color.b, alpha);
    }

    // Generic button surface: frosted backdrop + outline child; original graphic hidden.
    private static void ConvertButtonToFrost(Graphic graphic, Color outlineColor)
    {
        Image image = graphic as Image;
        if (image == null) return;

        GlassMark mark = Mark(graphic);
        mark.createdBackdrop = AttachBackdrop(graphic, ConfiguredTint(0.15f), false);
        mark.createdOutline = CreateOutlineChild(image, image.sprite != null, outlineColor, GetChamfer(image));
        graphic.color = Hidden;
    }

    private static float GetChamfer(Graphic graphic)
    {
        if (graphic is ChamferedImage chamfer) return chamfer.Chamfer;
        return 10f;
    }

    // Stretched outline ring child: hexagon shape for sprite hexes, chamfered otherwise.
    private static GameObject CreateOutlineChild(Image image, bool hexagon, Color color, float chamfer)
    {
        GameObject outline = new GameObject("GlassOutline", typeof(RectTransform));
        outline.transform.SetParent(image.transform, false);
        RectTransform rt = outline.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        if (hexagon)
        {
            HexagonImage hex = outline.AddComponent<HexagonImage>();
            hex.OutlineThickness = 6f;
            hex.color = color;
            hex.raycastTarget = false;
        }
        else
        {
            ChamferedImage chamferImage = outline.AddComponent<ChamferedImage>();
            chamferImage.Chamfer = chamfer;
            chamferImage.OutlineThickness = 4f;
            chamferImage.color = color;
            chamferImage.raycastTarget = false;
        }
        return outline;
    }

    private static bool IsInsideNamed(Transform transform, string name)
    {
        while (transform != null)
        {
            if (transform.name == name) return true;
            transform = transform.parent;
        }
        return false;
    }

    // True when the graphic's colour matches any Glass-palette border-ish colour.
    private static bool IsBorderColored(Graphic graphic)
    {
        Color color = graphic.color;
        return SameColor(color, HexlinkTheme.Glass.Border)
            || SameColor(color, HexlinkTheme.Glass.CellFrame)
            || SameColor(color, HexlinkTheme.Glass.Divider);
    }

    // Thin graphics (divider lines, tracks) stay as they are - just recoloured.
    private static bool IsThin(Graphic graphic)
    {
        Rect rect = graphic.rectTransform.rect;
        return rect.width < 10f || rect.height < 10f;
    }

    // Exact Color32 equality - palette detection needs bit-exact matches.
    private static bool SameColor(Color a, Color b)
    {
        Color32 ca = a;
        Color32 cb = b;
        return ca.r == cb.r && ca.g == cb.g && ca.b == cb.b && ca.a == cb.a;
    }
}
