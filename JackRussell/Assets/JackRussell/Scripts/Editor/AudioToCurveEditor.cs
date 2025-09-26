using UnityEngine;
using UnityEditor;

public class CurveAsset : ScriptableObject
{
    public AnimationCurve curve;
}

public class AudioToCurveEditor : EditorWindow
{
    private AudioClip audioClip;
    private int sampleStep = 100;
    private AnimationCurve generatedCurve;
    private AnimationCurve editableCurve;

    [MenuItem("Tools/Audio to Curve")]
    static void ShowWindow()
    {
        GetWindow<AudioToCurveEditor>("Audio to Curve");
    }

    void OnGUI()
    {
        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);
        sampleStep = EditorGUILayout.IntField("Sample Step", sampleStep);

        if (GUILayout.Button("Generate Curve"))
        {
            GenerateCurve();
        }

        if (generatedCurve != null)
        {
            EditorGUILayout.CurveField("Generated Curve", generatedCurve);
            EditorGUILayout.CurveField("Editable Curve", editableCurve);

            if (GUILayout.Button("Create Curve Asset"))
            {
                CreateCurveAsset();
            }
        }
    }

    private void GenerateCurve()
    {
        if (audioClip == null) return;

        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        int keyframeCount = samples.Length / sampleStep;
        Keyframe[] keyframes = new Keyframe[keyframeCount];
        float timeStep = (float)sampleStep / audioClip.frequency;

        for (int i = 0; i < keyframeCount; i++)
        {
            int sampleIndex = i * sampleStep;
            float amplitude = 0f;

            for (int channel = 0; channel < audioClip.channels; channel++)
            {
                amplitude += Mathf.Abs(samples[sampleIndex + channel]);
            }
            amplitude /= audioClip.channels;
            amplitude = Mathf.Clamp01(amplitude);

            keyframes[i] = new Keyframe(i * timeStep, amplitude);
        }

        generatedCurve = new AnimationCurve(keyframes);

        for (int i = 0; i < generatedCurve.keys.Length; i++)
        {
            generatedCurve.SmoothTangents(i, 0f);
        }

        editableCurve = generatedCurve;
    }

    private void CreateCurveAsset()
    {
        if (editableCurve == null) return;

        CurveAsset asset = CreateInstance<CurveAsset>();
        asset.curve = editableCurve;

        string path = EditorUtility.SaveFilePanelInProject("Save Curve Asset", "GlitchCurve", "asset", "Save the curve asset");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.Refresh();
        }
    }
}