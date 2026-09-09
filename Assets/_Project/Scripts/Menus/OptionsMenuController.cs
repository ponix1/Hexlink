using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsMenuController : MonoBehaviour
{
    private static readonly float[] SpeedValues = { 0.5f, 1f, 2f };
    private static readonly string[] SpeedLabels = { "0.5x", "1x", "2x" };

    private int speedIndex;
    private TextMeshProUGUI speedLabel;
    private Transform rowsRoot;

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

        if (rowsRoot != null)
        {
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

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        GameOptions.Save();
        gameObject.SetActive(false);
    }

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
            if (track != null) track.color = v ? new Color(0.36f, 0.63f, 0.94f) : new Color(0.35f, 0.35f, 0.41f);
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
