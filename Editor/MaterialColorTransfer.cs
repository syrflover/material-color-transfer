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
    private ColorField ShadowColor1Field;
    private ColorField ShadowColor2Field;
    private ColorField ShadowColor3Field;
    private ColorField ShadowBorderColorField;
    private ColorField RimShadeColorField;

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

        ShadowColor1Field = new ColorField("Shadow 1") { value = OriginalShadowColor1, showEyeDropper = false };
        ShadowColor2Field = new ColorField("Shadow 2") { value = OriginalShadowColor2, showEyeDropper = false };
        ShadowColor3Field = new ColorField("Shadow 3") { value = OriginalShadowColor3, showEyeDropper = false };
        ShadowBorderColorField = new ColorField("Shadow Border") { value = OriginalShadowBorderColor, showEyeDropper = false };
        RimShadeColorField = new ColorField("Rim Shade") { value = OriginalRimShadeColor, showEyeDropper = false };

        ShadowColor1Field.SetEnabled(false);
        ShadowColor1Field.style.opacity = 1f;
        ShadowColor1Field.style.color = new StyleColor(Color.white);
        ShadowColor2Field.SetEnabled(false);
        ShadowColor2Field.style.opacity = 1f;
        ShadowColor2Field.style.color = new StyleColor(Color.white);
        ShadowColor3Field.SetEnabled(false);
        ShadowColor3Field.style.opacity = 1f;
        ShadowColor3Field.style.color = new StyleColor(Color.white);
        ShadowBorderColorField.SetEnabled(false);
        ShadowBorderColorField.style.opacity = 1f;
        ShadowBorderColorField.style.color = new StyleColor(Color.white);
        RimShadeColorField.SetEnabled(false);
        RimShadeColorField.style.opacity = 1f;
        RimShadeColorField.style.color = new StyleColor(Color.white);

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

            material.SetColor("_ShadowColor", ShadowColor1Field.value);
            material.SetColor("_Shadow2ndColor", ShadowColor2Field.value);
            material.SetColor("_Shadow3rdColor", ShadowColor3Field.value);
            material.SetColor("_ShadowBorderColor", ShadowBorderColorField.value);
            material.SetColor("_RimShadeColor", RimShadeColorField.value);

            Close();
        })
        { text = "Apply" });

        SyncDerivedFields(BaseColorField.value, transferMode);

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
        ShadowColor1Field.value = OriginalShadowColor1;
        ShadowColor2Field.value = OriginalShadowColor2;
        ShadowColor3Field.value = OriginalShadowColor3;
        ShadowBorderColorField.value = OriginalShadowBorderColor;
        RimShadeColorField.value = OriginalRimShadeColor;
    }

    private void SyncDerivedFields(Color c, TransferMode transferMode)
    {
        BaseColorField.value = c;

        var HSVBaseColor = HSVColor.FromRGB(BaseColorField.value);

        var HSVShadowColor1 = HSVColor.FromRGB(ShadowColor1Field.value);
        var HSVShadowColor2 = HSVColor.FromRGB(ShadowColor2Field.value);
        var HSVShadowColor3 = HSVColor.FromRGB(ShadowColor3Field.value);
        var HSVShadowBorderColor = HSVColor.FromRGB(ShadowBorderColorField.value);
        var HSVRimShadeColor = HSVColor.FromRGB(RimShadeColorField.value);

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
            Debug.Log($"HSVBaseColor.saturation: {HSVBaseColor.saturation}");
            HSVShadowColor1.saturation = HSVBaseColor.saturation;
            HSVShadowColor2.saturation = HSVBaseColor.saturation;
            HSVShadowColor3.saturation = HSVBaseColor.saturation;
            HSVShadowBorderColor.saturation = HSVBaseColor.saturation;
            HSVRimShadeColor.saturation = HSVBaseColor.saturation;
        }

        ShadowColor1Field.value = HSVShadowColor1.ToRGB();
        ShadowColor2Field.value = HSVShadowColor2.ToRGB();
        ShadowColor3Field.value = HSVShadowColor3.ToRGB();
        ShadowBorderColorField.value = HSVShadowBorderColor.ToRGB();
        RimShadeColorField.value = HSVRimShadeColor.ToRGB();
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
            return Color.black;
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
            return Color.white;
        }
    }
}
