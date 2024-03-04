using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    MaterialEditor _editor;
    private Object[] _materials;
    MaterialProperty[] _properties;

    private bool _showPresets;

    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    private BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }
    
    private BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }

    bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1.0f : 0.0f);
    }

    CompareFunction ZTest
    {
        set => SetProperty("_ZTest", (float)value);
    }

    RenderQueue RenderQueue
    {
        set
        {
            foreach (Material material in _materials)
            {
                material.renderQueue = (int)value;
            }
        }
    }


    bool PresenButton(string name)
    {
        if (GUILayout.Button(name))
        {
            _editor.RegisterPropertyChangeUndo(name);
            return true;
        }

        return false;
    }

    void OpaquePreset()
    {
        if (PresenButton("Opaque"))
        {
            Clipping = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            ZTest = CompareFunction.LessEqual;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    
    void ClipPreset()
    {
        if (PresenButton("Clip"))
        {
            Clipping = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            ZTest = CompareFunction.LessEqual;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }
    
    void TransparentPreset()
    {
        if (PresenButton("Trnasparent"))
        {
            Clipping = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            ZTest = CompareFunction.Always;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        base.OnGUI(editor, properties);
        _editor = editor;
        _materials = editor.targets;
        _properties = properties;

        //BakeEmission(editor);

        EditorGUILayout.Space();
        _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
        if (_showPresets)
        {
            OpaquePreset();
            ClipPreset();
            TransparentPreset();
        }
    }

    void BakeEmission(MaterialEditor editor)
    {
        EditorGUI.BeginChangeCheck();
        editor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material m in editor.targets)
            {
                m.globalIlluminationFlags &=
                    ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }

    bool SetProperty(string name, float value)
    {
        var property = FindProperty(name, _properties);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }

        return false;
    }

    void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material material in _materials)
            {
                material.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material material in _materials)
            {
                material.DisableKeyword(keyword);
            }
        }
    }

    void SetProperty(string name, string keyword, bool enabled)
    {
        if (SetProperty(name, enabled ? 1.0f : 0.0f))
        {
            SetKeyword(keyword, enabled);
        }
    }
}