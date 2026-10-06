// OptionsMenuController: drives the factory-built Options screen - wires every control to
// GameOptions (persisting on each change), handles live re-theming, opens/closes the modal.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsMenuController : MonoBehaviour
{
    // Execution-speed cycle order; values and labels pair by index.
    private static readonly float[] SpeedValues = { 0.5f, 1f, 2f };
    private static readonly string[] SpeedLabels = { "0.5x", "1x", "2x" };

    private int speedIndex;
    private TextMeshProUGUI speedLabel;
    private TextMeshProUGUI designLabel;
    private Transform rowsRoot;

    // Runs on FIRST activation (the screen spawns inactive), so wiring waits until Open.
    // All lookups go through FindDeep by name - hardcoded paths broke when the factory
    // gained a Border layer.
    private void Start()
    {
        GameOptions.ApplySystemSettings();
        rowsRoot = FindDeep(transform, "Rows");

        WireToggle("ColourBlindRow", v =>
        {
            GameOptions.ColourBlindMode = v;
            ColourBlindApplier.ApplyToScene();
        }, GameOptions.ColourBlindMode);
        WireToggle("ReducedMotionRow", v => GameOptions.ReducedMotion = v, GameOptions.ReducedMotion);
        WireToggle("ConfirmRow", v => GameOptions.ConfirmTileChanges = v, GameOptions.ConfirmTileChanges);
        WireToggle("VSyncRow", v => GameOptions.VSync = v, GameOptions.VSync);

        WireStepper("UiScaleRow", v => GameOptions.UiScale = v, GameOptions.UiScale, 0.05f, 0.75f, 1.5f, "F2");
        WireStepper("PanRow", v => GameOptions.PanSpeed = v, GameOptions.PanSpeed, 0.005f, 0.005f, 0.08f, "F3");
        WireStepper("OrbitRow", v => GameOptions.OrbitSpeed = v, GameOptions.OrbitSpeed, 1f, 1f, 15f, "F0");

        WireStepper("GlassOpacityRow", v =>
        {
            GameOptions.GlassOutlineAlpha = v / 100f;
            GameOptions.Save();
            ThemeSwitcher.ApplyToScene();
        }, Mathf.Round(GameOptions.GlassOutlineAlpha * 100f), 5f, 0f, 100f, "F0");

        WireGlassColourRow();
        SetGlassSectionVisible(GameOptions.ThemeIndex == 2);

        if (rowsRoot != null)
        {
            Transform designRow = rowsRoot.Find("DesignRow");
            if (designRow != null)
            {
                Button designButton = designRow.Find("DesignButton").GetComponent<Button>();
                designLabel = designButton.GetComponentInChildren<TextMeshProUGUI>();
                UpdateDesignLabel();
                designButton.onClick.AddListener(CycleDesign);
            }

            Transform speedRow = rowsRoot.Find("SpeedRow");
            if (speedRow != null)
            {
                Button speedButton = speedRow.Find("SpeedButton").GetComponent<Button>();
                speedLabel = speedButton.GetComponentInChildren<TextMeshProUGUI>();

                float current = GameOptions.ExecutionSpeed;
                speedIndex = 1;
                for (int i = 0; i < SpeedValues.Length; i++)
                {
                    if (Mathf.Abs(SpeedValues[i] - current) < 0.01f) speedIndex = i;
                }
                UpdateSpeedLabel();
                speedButton.onClick.AddListener(CycleSpeed);
            }
        }

        Transform close = FindDeep(transform, "CloseButton");
        if (close != null)
        {
            close.GetComponent<Button>().onClick.AddListener(Close);
        }
    }

    // Invoked by the menu's Options button (listener added by the factory).
    public void Open()
    {
        gameObject.SetActive(true);
    }

    // Persist all staged pref writes on the way out.
    public void Close()
    {
        GameOptions.Save();
        gameObject.SetActive(false);
    }

    // Wraps through SpeedValues, persisting and relabelling each click.
    private void CycleSpeed()
    {
        speedIndex = (speedIndex + 1) % SpeedValues.Length;
        GameOptions.ExecutionSpeed = SpeedValues[speedIndex];
        GameOptions.Save();
        UpdateSpeedLabel();
    }

    private void UpdateSpeedLabel()
    {
        if (speedLabel != null) speedLabel.text = SpeedLabels[speedIndex];
    }

    // Cycles Dark -> Light -> Glass, re-theming the whole scene live and toggling the
    // Glass-only outline section.
    private void CycleDesign()
    {
        GameOptions.ThemeIndex = (GameOptions.ThemeIndex + 1) % HexlinkTheme.ThemeCount;
        GameOptions.Save();
        ThemeSwitcher.ApplyToScene();
        UpdateDesignLabel();
        SetGlassSectionVisible(GameOptions.ThemeIndex == 2);
    }

    // Wires swatch clicks by parsing the hex out of each "Swatch_XXXXXX" name.
    private void WireGlassColourRow()
    {
        Transform gridRow = rowsRoot != null ? rowsRoot.Find("GlassColourRowGrid") : null;
        if (gridRow == null) return;

        foreach (Transform swatch in gridRow)
        {
            if (!swatch.name.StartsWith("Swatch_")) continue;
            string hex = swatch.name.Substring("Swatch_".Length);

            swatch.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (GameOptions.GlassOutlineHex == hex) return;
                GameOptions.GlassOutlineHex = hex;
                GameOptions.Save();
                UpdateSwatchSelection(gridRow);
                ThemeSwitcher.ApplyToScene();
            });
        }

        UpdateSwatchSelection(gridRow);
    }

    // Moves the white "Sel" ring onto whichever swatch matches the saved colour.
    private static void UpdateSwatchSelection(Transform gridRow)
    {
        foreach (Transform swatch in gridRow)
        {
            if (!swatch.name.StartsWith("Swatch_")) continue;

            for (int i = swatch.childCount - 1; i >= 0; i--)
            {
                // DestroyImmediate: rebuild + re-check happen in the same frame.
                if (swatch.GetChild(i).name == "Sel") Object.DestroyImmediate(swatch.GetChild(i).gameObject);
            }

            string hex = swatch.name.Substring("Swatch_".Length);
            if (GameOptions.GlassOutlineHex != hex) continue;

            GameObject sel = new GameObject("Sel", typeof(RectTransform));
            sel.transform.SetParent(swatch, false);
            ChamferedImage ring = sel.AddComponent<ChamferedImage>();
            ring.Chamfer = 6f;
            ring.OutlineThickness = 2f;
            ring.color = Color.white;
            ring.raycastTarget = false;
            RectTransform selRT = sel.GetComponent<RectTransform>();
            selRT.anchorMin = Vector2.zero;
            selRT.anchorMax = Vector2.one;
            selRT.offsetMin = new Vector2(-3f, -3f);
            selRT.offsetMax = new Vector2(3f, 3f);
        }
    }

    // Outline colour/opacity only affect the Glass design (theme index 2).
    private void SetGlassSectionVisible(bool visible)
    {
        if (rowsRoot == null) return;
        string[] names = { "OUTLINEHeader", "GlassOpacityRow", "GlassColourRow", "GlassColourRowGrid" };
        foreach (string name in names)
        {
            Transform row = rowsRoot.Find(name);
            if (row != null) row.gameObject.SetActive(visible);
        }
    }

    private void UpdateDesignLabel()
    {
        if (designLabel != null) designLabel.text = HexlinkTheme.ThemeName(GameOptions.ThemeIndex);
    }

    // Wires a factory-built toggle row: knob POSITION (+/-12 x) carries the state, not
    // colour alone - keeps the control readable in colour-blind mode.
    private void WireToggle(string rowName, System.Action<bool> setter, bool initial)
    {
        Transform row = rowsRoot != null ? rowsRoot.Find(rowName) : null;
        if (row == null) return;

        Toggle toggle = row.Find("Toggle").GetComponent<Toggle>();
        Image track = toggle.GetComponent<Image>();
        RectTransform knob = toggle.transform.Find("Knob") as RectTransform;

        void Apply(bool v)
        {
            if (knob != null) knob.anchoredPosition = new Vector2(v ? 12f : -12f, 0f);
            if (track != null) track.color = v ? HexlinkTheme.Accent : HexlinkTheme.CellFill;
        }

        toggle.transition = Toggle.Transition.None;
        toggle.isOn = initial;
        Apply(initial);
        toggle.onValueChanged.AddListener(v =>
        {
            Apply(v);
            setter(v);
            GameOptions.Save();
        });
    }

    // Wires a stepper row: clamps to [min,max], formats the value box, persists per click.
    private void WireStepper(string rowName, System.Action<float> setter, float initial, float step, float min, float max, string format)
    {
        Transform row = rowsRoot != null ? rowsRoot.Find(rowName) : null;
        if (row == null) return;

        TextMeshProUGUI value = row.Find("Value/Text").GetComponent<TextMeshProUGUI>();
        float current = Mathf.Clamp(initial, min, max);
        value.text = current.ToString(format);

        row.Find("Minus").GetComponent<Button>().onClick.AddListener(() =>
        {
            current = Mathf.Max(min, current - step);
            value.text = current.ToString(format);
            setter(current);
            GameOptions.Save();
        });

        row.Find("Plus").GetComponent<Button>().onClick.AddListener(() =>
        {
            current = Mathf.Min(max, current + step);
            value.text = current.ToString(format);
            setter(current);
            GameOptions.Save();
        });
    }

    // Recursive by-name lookup; resilient to hierarchy changes (Border-layer past bug).
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
