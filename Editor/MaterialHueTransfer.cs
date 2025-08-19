using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class MaterialHueTransfer
{
    const string MENU_PATH = "Assets/Material Hue Transfer";

    [MenuItem(MENU_PATH, false)]
    static void TransferHue()
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

public class TransferWindow : EditorWindow
{
    public static void ShowWindow()
    {
        GetWindow<TransferWindow>("Material Hue Transfer");
    }


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

        BaseColorField = new ColorField("Base Color") { value = Color.white };

        ShadowColor1Field = new ColorField("Shadow 1") { value = material.GetColor("_ShadowColor"), showEyeDropper = false };
        ShadowColor2Field = new ColorField("Shadow 2") { value = material.GetColor("_Shadow2ndColor"), showEyeDropper = false };
        ShadowColor3Field = new ColorField("Shadow 3") { value = material.GetColor("_Shadow3rdColor"), showEyeDropper = false };
        ShadowBorderColorField = new ColorField("Shadow Border") { value = material.GetColor("_ShadowBorderColor"), showEyeDropper = false };
        RimShadeColorField = new ColorField("Rim Shade") { value = material.GetColor("_RimShadeColor"), showEyeDropper = false };

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
        root.Add(BaseColorField);
        root.Add(CreateSpacer(8));
        root.Add(new HelpBox("Does not work with grayscale colors.", HelpBoxMessageType.Warning));
        root.Add(CreateSpacer(12));
        root.Add(new Label("Transferred Colors"));
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
            material.SetColor("_ShadowColor", ShadowColor1Field.value);
            material.SetColor("_Shadow2ndColor", ShadowColor2Field.value);
            material.SetColor("_Shadow3rdColor", ShadowColor3Field.value);
            material.SetColor("_ShadowBorderColor", ShadowBorderColorField.value);
            material.SetColor("_RimShadeColor", RimShadeColorField.value);

            Close();
        })
        { text = "Apply" });

        SyncDerivedFields(BaseColorField.value);

        BaseColorField.RegisterValueChangedCallback(evt =>
        {
            SyncDerivedFields(evt.newValue);
        });
    }

    private void SyncDerivedFields(Color c)
    {
        BaseColorField.value = c;

        var HSVBaseColor = HSVColor.FromRGB(BaseColorField.value);

        if (float.IsNaN(HSVBaseColor.hue))
        {
            Debug.Log("Base color is grayscale, cannot transfer hue.");
            return;
        }

        var HSVShadowColor1 = HSVColor.FromRGB(ShadowColor1Field.value);
        var HSVShadowColor2 = HSVColor.FromRGB(ShadowColor2Field.value);
        var HSVShadowColor3 = HSVColor.FromRGB(ShadowColor3Field.value);
        var HSVShadowBorderColor = HSVColor.FromRGB(ShadowBorderColorField.value);
        var HSVRimShadeColor = HSVColor.FromRGB(RimShadeColorField.value);

        HSVShadowColor1.hue = HSVBaseColor.hue;
        HSVShadowColor2.hue = HSVBaseColor.hue;
        HSVShadowColor3.hue = HSVBaseColor.hue;
        HSVShadowBorderColor.hue = HSVBaseColor.hue;
        HSVRimShadeColor.hue = HSVBaseColor.hue;

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
            hue = float.NaN;
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

        if (saturation == 0)
        {
            return new Color(value, value, value);
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
