using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

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

class Property
{
    public string PropertyName;
    public string FlagName;
    public string Label;
    public Color OriginalColor;

    public VisualElement Field;

    public Property(string propertyName, string flagName, string label, Color originalColor)
    {
        PropertyName = propertyName;
        FlagName = flagName;
        Label = label;
        OriginalColor = originalColor;
        Field = null;

        // Debug.Log($"{PropertyName} " + OriginalColor);
    }
}

public class TransferWindow : EditorWindow
{
    public static void ShowWindow()
    {
        GetWindow<TransferWindow>("Material Color Transfer");
    }

    private static bool IsHDR(Color c)
    {
        return c.r > 1 || c.g > 1 || c.b > 1;
    }

    private TransferMode transferMode = TransferMode.Hue;
    private DropdownField TransferModeField;
    private ColorField BaseColorField;
    private List<Property> Properties;

    public void CreateGUI()
    {
        var root = rootVisualElement;

        var assetGUID = Selection.assetGUIDs[0];
        var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        TransferModeField = new DropdownField("Transfer Mode", new[] { "Hue", "Saturation" }.ToList(), (int)transferMode);

        BaseColorField = new ColorField("Base Color") { value = Color.white };

        Properties = new List<Property>(new Property[]{
            new("_ShadowColor", "_UseShadow","Shadow 1", material.GetColor("_ShadowColor")),
            new("_Shadow2ndColor", "_UseShadow","Shadow 2", material.GetColor("_Shadow2ndColor")),
            new("_Shadow3rdColor", "_UseShadow","Shadow 3", material.GetColor("_Shadow3rdColor")),
            new("_ShadowBorderColor", "_UseShadow", "Shadow Border", material.GetColor("_ShadowBorderColor")),
            new("_RimShadeColor", "_UseRimShade", "Rim Shade", material.GetColor("_RimShadeColor")),
            new("_RimColor", "_UseRim", "Rim Light", material.GetColor("_RimColor")),
            new("_RimIndirColor", "_UseRim", "Rim Indirect Light", material.GetColor("_RimIndirColor"))
        });

        root.Add(new TextField("Material Path") { value = assetPath, isReadOnly = true });
        root.Add(CreateSpacer(8));
        root.Add(TransferModeField);
        root.Add(CreateSpacer(8));
        root.Add(BaseColorField);
        root.Add(CreateSpacer(8));
        root.Add(new Label("Preview"));

        Properties.ForEach(p =>
        {
            p.Field = CreateTogglePreviewColorField(p.Label, p.OriginalColor, p.OriginalColor.a > 0 && material.GetFloat(p.FlagName) == 1);

            root.Add(CreateSpacer(8));
            root.Add(p.Field);
        });

        root.Add(CreateSpacer(12));

        var button = new VisualElement()
        {
            style = {
                flexDirection = FlexDirection.Row,
                width = new StyleLength(Length.Percent(100)),
                justifyContent = new StyleEnum<Justify>(Justify.Center),
            }
        };

        button.Add(new Button(() =>
        {
            RevertToOriginal();
        })
        { text = "Clear", style = { width = new StyleLength(Length.Percent(48)) } });

        button.Add(new Button(() =>
        {
            SyncDerivedFields(BaseColorField.value, transferMode);

            Properties.ForEach(p =>
            {
                if (p.Field.Q<Toggle>().value)
                {
                    material.SetColor(p.PropertyName, p.Field.Q<ColorField>().value);
                }
            });

            Close();
        })
        { text = "Apply", style = { width = new StyleLength(Length.Percent(48)) } });

        root.Add(button);

        // root.Add(CreateTestField());

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
        Properties.ForEach(p =>
        {
            p.Field.Q<ColorField>().SetValueWithoutNotify(p.OriginalColor);
        });
    }

    private void SyncDerivedFields(Color c, TransferMode transferMode)
    {
        BaseColorField.SetValueWithoutNotify(c);

        var HSVBaseColor = HSVColor.FromRGB(BaseColorField.value);

        Properties.ForEach(p =>
        {
            var pcHSV = HSVColor.FromRGB(p.Field.Q<ColorField>().value);

            // Debug.Log($"{p.PropertyName} : {pcHSV.hue} {pcHSV.saturation} {pcHSV.value} {pcHSV.alpha}");

            if (transferMode == TransferMode.Hue)
            {
                pcHSV.hue = HSVBaseColor.hue;
            }
            else if (transferMode == TransferMode.Saturation)
            {
                pcHSV.saturation = HSVBaseColor.saturation;
            }

            var pcRGB = pcHSV.ToRGB();
            var pcHex = ColorUtility.ToHtmlStringRGB(pcRGB);

            p.Field.Q<ColorField>().SetValueWithoutNotify(pcRGB);

            if (!IsHDR(pcRGB))
            {
                p.Field.Q<TextField>().value = pcHex;
            }

            // Debug.Log($"{p.PropertyName} : {c} : HSVColor({pcHSV.hue}, {pcHSV.saturation}, {pcHSV.value}) -> {pcRGB} -> {pcHex}");
        });
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

        var isHDR = IsHDR(originalColor);

        var colorField = new ColorField() { value = originalColor, showEyeDropper = false, hdr = isHDR };
        colorField.SetEnabled(true);

        var hexColorCodeField = new TextField() { value = ColorUtility.ToHtmlStringRGB(originalColor), isReadOnly = true };

        var row = new VisualElement();

        row.Add(toggle);
        row.Add(colorField);

        if (!isHDR)
        {
            row.Add(hexColorCodeField);
        }

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

    private VisualElement CreateTestField()
    {
        var test = new VisualElement() { style = { alignContent = new StyleEnum<Align>(Align.Center), flexDirection = FlexDirection.Column } };

        test.Add(CreateSpacer(16));

        var input = new VisualElement() { style = { flexDirection = FlexDirection.Column } };

        var inputRGB = new ColorField() { value = Color.white, hdr = true };
        var inputText = new VisualElement() { style = { flexDirection = FlexDirection.Column } };

        var inputR = new TextField() { value = "0" };
        var inputG = new TextField() { value = "0" };
        var inputB = new TextField() { value = "0" };

        inputText.Add(inputR);
        inputText.Add(inputG);
        inputText.Add(inputB);

        input.Add(inputRGB);
        input.Add(inputText);

        test.Add(input);

        var outputHSV = new TextField() { value = "0, 0, 0", isReadOnly = true };
        var outputRGB = new ColorField() { value = Color.white, hdr = true };
        outputRGB.SetEnabled(false);

        var outputText = new VisualElement() { style = { flexDirection = FlexDirection.Column } };

        var outputR = new TextField() { value = "0" };
        var outputG = new TextField() { value = "0" };
        var outputB = new TextField() { value = "0" };

        outputText.Add(outputR);
        outputText.Add(outputG);
        outputText.Add(outputB);

        test.Add(outputHSV);
        test.Add(outputRGB);
        test.Add(outputText);

        inputRGB.RegisterValueChangedCallback(evt =>
        {
            var c1 = HSVColor.FromRGB(evt.newValue);
            var c2 = c1.ToRGB();

            outputHSV.value = $"{c1.hue}, {c1.saturation}, {c1.value}";
            outputRGB.value = c2;
            outputR.value = c2.r.ToString();
            outputG.value = c2.g.ToString();
            outputB.value = c2.b.ToString();
        });
        inputR.RegisterValueChangedCallback(evt =>
        {
            var color = inputRGB.value;

            if (float.TryParse(evt.newValue, out float r))
            {
                color.r = r;
                inputRGB.value = color;
            }
        });
        inputG.RegisterValueChangedCallback(evt =>
        {
            var color = inputRGB.value;

            if (float.TryParse(evt.newValue, out float g))
            {
                color.g = g;
                inputRGB.value = color;
            }
        });
        inputB.RegisterValueChangedCallback(evt =>
        {
            var color = inputRGB.value;

            if (float.TryParse(evt.newValue, out float b))
            {
                color.b = b;
                inputRGB.value = color;
            }
        });

        return test;
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
