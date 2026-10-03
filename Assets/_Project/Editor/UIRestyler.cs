using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class UIRestyler
{
    [MenuItem("Hexlink/Restyle Scenes To InfoTab Theme")]
    public static void RestyleAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Selection.activeObject = null;
        DeleteChamferAsset();

        string originalScene = SceneManager.GetActiveScene().path;

        RestyleScene("Assets/_Project/Scenes/Main Menu.unity", RestyleMainMenu);
        RestyleScene("Assets/_Project/Scenes/Puzzle_Level.unity", RestylePuzzleLevel);
        RestyleScene("Assets/_Project/Scenes/Level_Select.unity", RestyleLevelSelect);
        RestyleScene("Assets/_Project/Scenes/Puzzle_Select.unity", RestylePuzzleSelect);

        try
        {
            RestyleCardPrefab();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Restyle failed for Card.prefab - prefab left unchanged.");
        }

        if (!string.IsNullOrEmpty(originalScene))
        {
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
        }
        Selection.activeObject = null;
        Debug.Log("InfoTab theme restyle complete.");
    }

    private static void DeleteChamferAsset()
    {
        const string path = "Assets/_Project/Assets/ChamferPanel.png";
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void RestyleScene(string scenePath, System.Action restyle)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogWarning($"Scene not found, skipping: {scenePath}");
            return;
        }
        try
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            restyle();
            EditorSceneManager.SaveScene(scene);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Restyle failed for '{scenePath}' - reopening to discard partial changes.");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }

    private static void RestyleMainMenu()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform group = FindDeep(canvas.transform, "HexButtonGroup");
        if (group == null) return;

        foreach (Transform child in EnumerateDeep(group))
        {
            if (child.GetComponent<Button>() == null) continue;
            ThemeHexButton(child);
        }

        SnapHexButton(group, "Play", 0f, 100f);
        SnapHexButton(group, "Creator", -218f, 100f);
        SnapHexButton(group, "Options", 218f, 100f);
        SnapHexButton(group, "Save", -109f, -87.5f);
        SnapHexButton(group, "Exit", 109f, -87.5f);
    }

    private static void ThemeHexButton(Transform t)
    {
        Image ring = t.GetComponent<Image>();
        Button button = t.GetComponent<Button>();
        if (ring == null || button == null || ring.sprite == null) return;

        Sprite sprite = ring.sprite;
        RectTransform rootRT = (RectTransform)t;

        EnsureTightMesh(sprite);

        ring.color = HexlinkTheme.Divider;

        Mask mask = t.GetComponent<Mask>();
        if (mask == null) mask = t.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        Image bevel = GetOrCreateLayer(t, "Bevel", sprite);
        bevel.color = HexlinkTheme.Border;
        RectTransform bevelRT = (RectTransform)bevel.transform;
        bevelRT.anchorMin = new Vector2(0.5f, 0.5f);
        bevelRT.anchorMax = new Vector2(0.5f, 0.5f);
        bevelRT.pivot = new Vector2(0.5f, 0.5f);
        bevelRT.anchoredPosition = new Vector2(0f, -7f);
        bevelRT.sizeDelta = rootRT.sizeDelta;
        bevelRT.localScale = Vector3.one;
        bevelRT.SetSiblingIndex(0);

        Image face = GetOrCreateLayer(t, "Face", sprite);
        face.color = HexlinkTheme.CellFill;
        RectTransform faceRT = (RectTransform)face.transform;
        faceRT.anchorMin = new Vector2(0.5f, 0.5f);
        faceRT.anchorMax = new Vector2(0.5f, 0.5f);
        faceRT.pivot = new Vector2(0.5f, 0.5f);
        faceRT.anchoredPosition = Vector2.zero;
        faceRT.sizeDelta = rootRT.sizeDelta;
        faceRT.localScale = Vector3.one * 0.92f;
        faceRT.SetSiblingIndex(1);

        button.targetGraphic = face;
        HexlinkTheme.ApplyHoverTint(button);

        TextMeshProUGUI label = t.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            SetTMPColor(label, HexlinkTheme.ChipText);
            label.fontStyle |= FontStyles.Bold;
            label.transform.SetAsLastSibling();
        }
        EditorUtility.SetDirty(t.gameObject);
    }

    private static void EnsureTightMesh(Sprite sprite)
    {
        string path = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(path)) return;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteMeshType == SpriteMeshType.Tight) return;

        settings.spriteMeshType = SpriteMeshType.Tight;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void SnapHexButton(Transform group, string name, float x, float y)
    {
        RectTransform rt = FindDeep(group, name) as RectTransform;
        if (rt == null)
        {
            Debug.LogWarning($"UIRestyler: hex button '{name}' not found for snapping.");
            return;
        }

        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(208f, 240f);
        EditorUtility.SetDirty(rt.gameObject);
    }

    private static void RestylePuzzleLevel()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        foreach (Transform t in EnumerateDeep(canvas.transform))
        {
            if (t.name != "Puzzles" && t.name != "Levels") continue;

            Button button = t.GetComponent<Button>();
            if (button != null)
            {
                RestylePanelButton(t);
            }
            else
            {
                TextMeshProUGUI header = t.GetComponent<TextMeshProUGUI>();
                if (header != null)
                {
                    SetTMPColor(header, HexlinkTheme.TextLight);
                    EditorUtility.SetDirty(header.gameObject);
                }
            }
        }
    }

    private static void RestylePanelButton(Transform t)
    {
        Button button = t.GetComponent<Button>();
        if (button == null) return;

        RectTransform rootRT = (RectTransform)t;

        Transform legacyFill = t.Find("Fill");
        if (legacyFill != null)
        {
            Object.DestroyImmediate(legacyFill.gameObject);
        }

        SwapToChamferedImage(t, HexlinkTheme.Divider, 18f, true);

        Mask panelMask = t.GetComponent<Mask>();
        if (panelMask == null) panelMask = t.gameObject.AddComponent<Mask>();
        panelMask.showMaskGraphic = true;

        RectTransform bevelRT = GetOrCreateChild(t, "Bevel") as RectTransform;
        if (bevelRT != null)
        {
            bevelRT.anchorMin = new Vector2(0.5f, 0.5f);
            bevelRT.anchorMax = new Vector2(0.5f, 0.5f);
            bevelRT.pivot = new Vector2(0.5f, 0.5f);
            bevelRT.anchoredPosition = new Vector2(0f, -8f);
            bevelRT.sizeDelta = rootRT.sizeDelta;
            bevelRT.localScale = Vector3.one;
            bevelRT.SetSiblingIndex(0);
            SwapToChamferedImage(bevelRT, HexlinkTheme.Border, 18f, false);
        }

        RectTransform faceRT = GetOrCreateChild(t, "Face") as RectTransform;
        ChamferedImage face = null;
        if (faceRT != null)
        {
            faceRT.anchorMin = new Vector2(0.5f, 0.5f);
            faceRT.anchorMax = new Vector2(0.5f, 0.5f);
            faceRT.pivot = new Vector2(0.5f, 0.5f);
            faceRT.anchoredPosition = Vector2.zero;
            faceRT.sizeDelta = rootRT.sizeDelta - new Vector2(16f, 16f);
            faceRT.localScale = Vector3.one;
            faceRT.SetSiblingIndex(1);
            face = SwapToChamferedImage(faceRT, HexlinkTheme.Panel, 18f, false);
        }

        if (face != null)
        {
            button.targetGraphic = face;
        }
        HexlinkTheme.ApplyHoverTint(button);

        TextMeshProUGUI label = t.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            SetTMPColor(label, HexlinkTheme.TextLight);
            label.fontStyle |= FontStyles.Bold;
            label.transform.SetAsLastSibling();
        }
        EditorUtility.SetDirty(t.gameObject);
    }

    private static void RestyleLevelSelect()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform back = FindDeep(canvas.transform, "BackButton");
        if (back != null)
        {
            RestyleButtonVisual(back, HexlinkTheme.Ghost, HexlinkTheme.TextLight, true);
        }
    }

    private static void RestylePuzzleSelect()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform worldCard = FindDeep(canvas.transform, "WorldCard");
        if (worldCard != null)
        {
            SwapToChamferedImage(worldCard, HexlinkTheme.Border, 18f, true);
            CreateInsetChamferFill(worldCard, HexlinkTheme.Panel, 5f);

            TextMeshProUGUI nameText = worldCard.GetComponentInChildren<TextMeshProUGUI>(true);
            if (nameText != null)
            {
                SetTMPColor(nameText, HexlinkTheme.TextLight);
                nameText.fontStyle |= FontStyles.Bold;
                EditorUtility.SetDirty(nameText.gameObject);
            }
        }

        Transform scrollView = FindDeep(canvas.transform, "Scroll View");
        if (scrollView != null)
        {
            SwapToChamferedImage(scrollView, HexlinkTheme.Border, 18f, true);
        }

        Transform scrollbar = FindDeep(canvas.transform, "Scrollbar Vertical");
        if (scrollbar != null)
        {
            Image track = scrollbar.GetComponent<Image>();
            if (track != null)
            {
                track.color = HexlinkTheme.CellFrame;
                track.sprite = null;
                EditorUtility.SetDirty(track.gameObject);
            }

            Transform handle = scrollbar.Find("Sliding Area/Handle");
            if (handle != null)
            {
                Image handleImage = handle.GetComponent<Image>();
                if (handleImage != null)
                {
                    handleImage.color = HexlinkTheme.Divider;
                    EditorUtility.SetDirty(handleImage.gameObject);
                }
            }
        }

        Transform viewport = FindDeep(canvas.transform, "Viewport");
        if (viewport != null)
        {
            SwapToChamferedImage(viewport, HexlinkTheme.Panel, 18f, true);

            Mask mask = viewport.GetComponent<Mask>();
            if (mask != null)
            {
                mask.showMaskGraphic = true;
                EditorUtility.SetDirty(mask.gameObject);
            }
        }

        Transform content = FindDeep(canvas.transform, "Content");
        if (content != null)
        {
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.padding = new RectOffset(20, 20, 20, 20);
                EditorUtility.SetDirty(grid.gameObject);
            }
        }
    }

    private static void RestyleCardPrefab()
    {
        string path = "Assets/_Project/Prefabs/Card.prefab";
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Prefab not found, skipping: {path}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Button button = root.GetComponent<Button>();

            Transform legacyFill = root.transform.Find("Fill");
            if (legacyFill != null)
            {
                Object.DestroyImmediate(legacyFill.gameObject);
            }

            SwapToChamferedImage(root.transform, HexlinkTheme.Divider, 14f, true);

            Mask cardMask = root.GetComponent<Mask>();
            if (cardMask == null) cardMask = root.AddComponent<Mask>();
            cardMask.showMaskGraphic = true;

            RectTransform bevelRT = GetOrCreateChild(root.transform, "Bevel") as RectTransform;
            if (bevelRT != null)
            {
                bevelRT.anchorMin = Vector2.zero;
                bevelRT.anchorMax = Vector2.one;
                bevelRT.offsetMin = new Vector2(0f, -6f);
                bevelRT.offsetMax = new Vector2(0f, -6f);
                bevelRT.localScale = Vector3.one;
                bevelRT.SetSiblingIndex(0);
                SwapToChamferedImage(bevelRT, HexlinkTheme.Border, 14f, false);
            }

            ChamferedImage face = null;
            RectTransform faceRT = GetOrCreateChild(root.transform, "Face") as RectTransform;
            if (faceRT != null)
            {
                faceRT.anchorMin = Vector2.zero;
                faceRT.anchorMax = Vector2.one;
                faceRT.offsetMin = new Vector2(10f, 10f);
                faceRT.offsetMax = new Vector2(-10f, -10f);
                faceRT.localScale = Vector3.one;
                faceRT.SetSiblingIndex(1);
                face = SwapToChamferedImage(faceRT, HexlinkTheme.CellFill, 14f, false);
            }

            if (button != null)
            {
                if (face != null) button.targetGraphic = face;
                HexlinkTheme.ApplyHoverTint(button);
            }

            foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                SetTMPColor(tmp, tmp.name == "PuzzleName" ? HexlinkTheme.TextLight : HexlinkTheme.TextGray);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RestyleButtonVisual(Transform t, Color imageColor, Color labelColor, bool bold)
    {
        Image image = t.GetComponent<Image>();
        if (image != null)
        {
            image.color = imageColor;
            EditorUtility.SetDirty(image.gameObject);
        }
        Button button = t.GetComponent<Button>();
        if (button != null)
        {
            HexlinkTheme.ApplyHoverTint(button);
            EditorUtility.SetDirty(button.gameObject);
        }
        TextMeshProUGUI label = t.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            SetTMPColor(label, labelColor);
            if (bold) label.fontStyle |= FontStyles.Bold;
            EditorUtility.SetDirty(label.gameObject);
        }
    }

    private static void SetTMPColor(TextMeshProUGUI tmp, Color color)
    {
        tmp.color = color;
        SerializedObject so = new SerializedObject(tmp);
        so.FindProperty("m_fontColor").colorValue = color;

        Color32 c32 = color;
        int packed = (c32.a << 24) | (c32.r << 16) | (c32.g << 8) | c32.b;
        SerializedProperty color32Prop = so.FindProperty("m_fontColor32");
        if (color32Prop != null)
        {
            if (color32Prop.propertyType == SerializedPropertyType.Integer)
            {
                color32Prop.intValue = packed;
            }
            else
            {
                SerializedProperty rgba = color32Prop.FindPropertyRelative("rgba");
                if (rgba != null && rgba.propertyType == SerializedPropertyType.Integer)
                {
                    rgba.intValue = packed;
                }
            }
        }
        so.ApplyModifiedProperties();
    }

    private static void CreateInsetChamferFill(Transform parent, Color color, float inset)
    {
        RectTransform fillRT = GetOrCreateChild(parent, "Fill") as RectTransform;
        if (fillRT == null) return;

        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(inset, inset);
        fillRT.offsetMax = new Vector2(-inset, -inset);
        fillRT.SetSiblingIndex(0);
        SwapToChamferedImage(fillRT, color, 18f, false);
    }

    private static ChamferedImage SwapToChamferedImage(Transform t, Color color, float chamfer, bool raycastTarget)
    {
        Image existingImage = t.GetComponent<Image>();
        ChamferedImage chamferImage = t.GetComponent<ChamferedImage>();

        if (chamferImage == null && existingImage != null)
        {
            Button owner = t.GetComponent<Button>();
            bool wasTarget = owner != null && owner.targetGraphic == existingImage;
            Object.DestroyImmediate(existingImage);
            chamferImage = t.gameObject.AddComponent<ChamferedImage>();
            if (wasTarget)
            {
                owner.targetGraphic = chamferImage;
            }
        }
        else if (chamferImage == null)
        {
            chamferImage = t.gameObject.AddComponent<ChamferedImage>();
        }
        else if (existingImage != null && existingImage != (Image)chamferImage)
        {
            Object.DestroyImmediate(existingImage);
        }

        chamferImage.Chamfer = chamfer;
        chamferImage.sprite = null;
        chamferImage.color = color;
        chamferImage.raycastTarget = raycastTarget;
        EditorUtility.SetDirty(t.gameObject);
        return chamferImage;
    }

    private static Transform GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Image GetOrCreateLayer(Transform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
        }

        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
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

    private static System.Collections.Generic.IEnumerable<Transform> EnumerateDeep(Transform parent)
    {
        foreach (Transform child in parent)
        {
            yield return child;
            foreach (Transform descendant in EnumerateDeep(child))
            {
                yield return descendant;
            }
        }
    }
}
