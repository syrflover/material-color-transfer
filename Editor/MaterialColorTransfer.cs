using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;

public class MaterialColorTransfer
{
    const string MENU_PATH = "Assets/Material Color Transfer";

    [MenuItem(MENU_PATH, false)]
    static void TransferColor()
    {
        TransferWindow.ShowWindow();
    }

    [MenuItem(MENU_PATH, true)]
    static bool Validate()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length != 1)
        {
            return false;
        }

        var assetGUID = Selection.assetGUIDs[0];
        var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);

        return AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(Material);
    }
}

enum TransferMode
{
    Hue,
    Saturation
}

public class TransferWindow : EditorWindow
{
    public static void ShowWindow()
    {
        GetWindow<TransferWindow>("Material Color Transfer");
    }


    private TransferMode transferMode = TransferMode.Hue;

    private Color OriginalShadowColor1;
    private Color OriginalShadowColor2;
    private Color OriginalShadowColor3;
    private Color OriginalShadowBorderColor;
    private Color OriginalRimShadeColor;

    private DropdownField TransferModeField;

    private ColorField BaseColorField;
    private VisualElement ShadowColor1Field;
    private VisualElement ShadowColor2Field;
    private VisualElement ShadowColor3Field;
    private VisualElement ShadowBorderColorField;
    private VisualElement RimShadeColorField;

    public void CreateGUI()
    {
        var root = rootVisualElement;

        var assetGUID = Selection.assetGUIDs[0];
        var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        TransferModeField = new DropdownField("Transfer Mode", new[] { "Hue", "Saturation" }.ToList(), (int)transferMode);

        BaseColorField = new ColorField("Base Color") { value = Color.white };

        OriginalShadowColor1 = material.GetColor("_ShadowColor");
        OriginalShadowColor2 = material.GetColor("_Shadow2ndColor");
        OriginalShadowColor3 = material.GetColor("_Shadow3rdColor");
        OriginalShadowBorderColor = material.GetColor("_ShadowBorderColor");
        OriginalRimShadeColor = material.GetColor("_RimShadeColor");

        ShadowColor1Field = CreateTogglePreviewColorField("Shadow 1", OriginalShadowColor1, OriginalShadowColor1.a > 0 && material.GetFloat("_UseShadow") == 1);
        ShadowColor2Field = CreateTogglePreviewColorField("Shadow 2", OriginalShadowColor2, OriginalShadowColor2.a > 0 && material.GetFloat("_UseShadow") == 1);
        ShadowColor3Field = CreateTogglePreviewColorField("Shadow 3", OriginalShadowColor3, OriginalShadowColor3.a > 0 && material.GetFloat("_UseShadow") == 1);
        ShadowBorderColorField = CreateTogglePreviewColorField("Shadow Border", OriginalShadowBorderColor, OriginalShadowBorderColor.a > 0 && material.GetFloat("_UseShadow") == 1);
        RimShadeColorField = CreateTogglePreviewColorField("Rim Shade", OriginalRimShadeColor, OriginalRimShadeColor.a > 0 && material.GetFloat("_UseRimShade") == 1);

        root.Add(new TextField("Material Path") { value = assetPath, isReadOnly = true });
        root.Add(CreateSpacer(8));
        root.Add(TransferModeField);
        root.Add(CreateSpacer(8));
        root.Add(BaseColorField);
        root.Add(CreateSpacer(8));
        root.Add(new Label("Preview"));
        root.Add(CreateSpacer(8));
        root.Add(ShadowColor1Field);
        root.Add(CreateSpacer(8));
        root.Add(ShadowColor2Field);
        root.Add(CreateSpacer(8));
        root.Add(ShadowColor3Field);
        root.Add(CreateSpacer(8));
        root.Add(ShadowBorderColorField);
        root.Add(CreateSpacer(8));
        root.Add(RimShadeColorField);
        root.Add(CreateSpacer(12));
        root.Add(new Button(() =>
        {
            SyncDerivedFields(BaseColorField.value, transferMode);

            if (ShadowColor1Field.Q<Toggle>().value)
            {
                material.SetColor("_ShadowColor", ShadowColor1Field.Q<ColorField>().value);
            }

            if (ShadowColor2Field.Q<Toggle>().value)
            {
                material.SetColor("_Shadow2ndColor", ShadowColor2Field.Q<ColorField>().value);
            }

            if (ShadowColor3Field.Q<Toggle>().value)
            {
                material.SetColor("_Shadow3rdColor", ShadowColor3Field.Q<ColorField>().value);
            }

            if (ShadowBorderColorField.Q<Toggle>().value)
            {
                material.SetColor("_ShadowBorderColor", ShadowBorderColorField.Q<ColorField>().value);
            }

            if (RimShadeColorField.Q<Toggle>().value)
            {
                material.SetColor("_RimShadeColor", RimShadeColorField.Q<ColorField>().value);
            }

            Close();
        })
        { text = "Apply" });

        BaseColorField.RegisterValueChangedCallback(evt =>
        {
            SyncDerivedFields(evt.newValue, transferMode);
        });

        TransferModeField.RegisterValueChangedCallback(evt =>
        {
            // Debug.Log($"Transfer mode changed to: {evt.newValue}");

            if (evt.newValue == "Hue")
            {
                transferMode = TransferMode.Hue;
            }
            else if (evt.newValue == "Saturation")
            {
                transferMode = TransferMode.Saturation;
            }

            RevertToOriginal();

            SyncDerivedFields(BaseColorField.value, transferMode);
        });
    }

    private void RevertToOriginal()
    {
        ShadowColor1Field.Q<ColorField>().value = OriginalShadowColor1;
        ShadowColor2Field.Q<ColorField>().value = OriginalShadowColor2;
        ShadowColor3Field.Q<ColorField>().value = OriginalShadowColor3;
        ShadowBorderColorField.Q<ColorField>().value = OriginalShadowBorderColor;
        RimShadeColorField.Q<ColorField>().value = OriginalRimShadeColor;
    }

    private void SyncDerivedFields(Color c, TransferMode transferMode)
    {
        BaseColorField.value = c;

        var HSVBaseColor = HSVColor.FromRGB(BaseColorField.value);

        var HSVShadowColor1 = HSVColor.FromRGB(ShadowColor1Field.Q<ColorField>().value);
        var HSVShadowColor2 = HSVColor.FromRGB(ShadowColor2Field.Q<ColorField>().value);
        var HSVShadowColor3 = HSVColor.FromRGB(ShadowColor3Field.Q<ColorField>().value);
        var HSVShadowBorderColor = HSVColor.FromRGB(ShadowBorderColorField.Q<ColorField>().value);
        var HSVRimShadeColor = HSVColor.FromRGB(RimShadeColorField.Q<ColorField>().value);

        if (transferMode == TransferMode.Hue)
        {
            HSVShadowColor1.hue = HSVBaseColor.hue;
            HSVShadowColor2.hue = HSVBaseColor.hue;
            HSVShadowColor3.hue = HSVBaseColor.hue;
            HSVShadowBorderColor.hue = HSVBaseColor.hue;
            HSVRimShadeColor.hue = HSVBaseColor.hue;
        }
        else if (transferMode == TransferMode.Saturation)
        {
            HSVShadowColor1.saturation = HSVBaseColor.saturation;
            HSVShadowColor2.saturation = HSVBaseColor.saturation;
            HSVShadowColor3.saturation = HSVBaseColor.saturation;
            HSVShadowBorderColor.saturation = HSVBaseColor.saturation;
            HSVRimShadeColor.saturation = HSVBaseColor.saturation;
        }

        if (ShadowColor1Field.Q<Toggle>().value)
        {
            ShadowColor1Field.Q<ColorField>().value = HSVShadowColor1.ToRGB();
        }

        if (ShadowColor2Field.Q<Toggle>().value)
        {
            ShadowColor2Field.Q<ColorField>().value = HSVShadowColor2.ToRGB();
        }

        if (ShadowColor3Field.Q<Toggle>().value)
        {
            ShadowColor3Field.Q<ColorField>().value = HSVShadowColor3.ToRGB();
        }

        if (ShadowBorderColorField.Q<Toggle>().value)
        {
            ShadowBorderColorField.Q<ColorField>().value = HSVShadowBorderColor.ToRGB();
        }

        if (RimShadeColorField.Q<Toggle>().value)
        {
            RimShadeColorField.Q<ColorField>().value = HSVRimShadeColor.ToRGB();
        }
    }

    private VisualElement CreateSpacer(float height)
    {
        var spacer = new VisualElement();
        spacer.style.height = height;
        spacer.style.width = 0;
        spacer.style.flexShrink = 0;
        spacer.pickingMode = PickingMode.Ignore;
        return spacer;
    }

    private VisualElement CreateTogglePreviewColorField(string label, Color originalColor, bool enabled)
    {
        var toggle = new Toggle(label) { value = enabled };

        toggle.SetEnabled(enabled);

        var colorField = new ColorField() { value = originalColor, showEyeDropper = false };

        colorField.SetEnabled(false);

        var hexColorCodeField = new TextField() { value = "#" + ColorUtility.ToHtmlStringRGB(originalColor), isReadOnly = true };

        var row = new VisualElement();

        row.Add(toggle);
        row.Add(colorField);
        row.Add(hexColorCodeField);

        row.style.flexDirection = FlexDirection.Row;

        toggle.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue)
            {
                SyncDerivedFields(BaseColorField.value, transferMode);
            }
            else
            {
                colorField.value = originalColor;
            }

        });

        return row;
    }
}

struct HSVColor
{
    public float hue;
    public float saturation;
    public float value;
    public float alpha;

    public HSVColor(float h, float s, float v, float a)
    {
        hue = h;
        saturation = s;
        value = v;
        alpha = a;
    }

    public static HSVColor FromRGB(Color color)
    {
        float r = color.r / 255f;
        float g = color.g / 255f;
        float b = color.b / 255f;

        // Debug.Log($"r = {r}; g = {g}; b = {b}");

        float max = Mathf.Max(r, g, b);
        float min = Mathf.Min(r, g, b);
        float delta = max - min;

        float value = max;
        float saturation = value == 0 ? 0 : delta / value;

        float hue;

        if (delta == 0)
        {
            hue = 0;
        }
        else if (max == r)
        {
            hue = (g - b) / delta + (g < b ? 6 : 0);
        }
        else if (max == g)
        {
            hue = (b - r) / delta + 2;
        }
        else
        {
            hue = (r - g) / delta + 4;
        }

        // Debug.Log($"max = {max}; min = {min}; delta = {delta}; hue = {hue}; saturation = {saturation}; value = {value}");

        return new HSVColor(hue * 60f, saturation, value, color.a);
    }

    public Color ToRGB()
    {
        if (value == 0)
        {
            return new Color(0f, 0f, 0f, alpha);
        }

        float h = hue / 60f;
        float i = Mathf.Floor(h);
        float f = h - i;

        float p = value * (1 - saturation) * 255f;
        float q = value * (1 - saturation * f) * 255f;
        float t = value * (1 - saturation * (1 - f)) * 255f;
        float v = value * 255f;

        // Debug.Log($"v = {v}; h = {h}; i = {i}");

        if (i == 0)
        {
            return new Color(v, t, p, alpha);
        }
        else if (i == 1)
        {
            return new Color(q, v, p, alpha);
        }
        else if (i == 2)
        {
            return new Color(p, v, t, alpha);
        }
        else if (i == 3)
        {
            return new Color(p, q, v, alpha);
        }
        else if (i == 4)
        {
            return new Color(t, p, v, alpha);
        }
        else if (i == 5)
        {
            return new Color(v, p, q, alpha);
        }
        else
        {
            Debug.LogError($"Invalid hue value: {hue}. Returning white color.");
            return new Color(1f, 1f, 1f, alpha);
        }
    }
}
