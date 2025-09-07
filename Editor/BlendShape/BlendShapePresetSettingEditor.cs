#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UniGLTF;
using UnityEditor;
using UnityEngine;
using VRM;

namespace VST
{
    public class BlendShapePresetSettingEditor : EditorWindow
    {
        public BlendShapePresetSettingManager m_manager = new BlendShapePresetSettingManager();

        [MenuItem("VRM0/VST/BlendShape/BlendShapePresetSetting")]
        static void Init()
        {
            var window = GetWindowWithRect<BlendShapePresetSettingEditor>(new Rect(0, 0, 400, 560));
            window.Show();
        }

        void OnEnable()
        {
            // find blend shape clip folder in Assets
            m_manager.FindBlendShapeClipFolder();
        }

        void OnGUI()
        {
            m_manager.m_blendShapeClipFolder = EditorGUILayout.ObjectField("Blend Shape Clip Folder", m_manager.m_blendShapeClipFolder, typeof(UnityEngine.Object), true);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("Box");
            var blendShapePresetClips = new Dictionary<string, BlendShapeClip>(m_manager.m_blendShapePresetClips);
            foreach (var presetName in blendShapePresetClips.Keys) {
                var presetClip = (BlendShapeClip)EditorGUILayout.ObjectField(
                    presetName,
                    blendShapePresetClips[presetName],
                    typeof(BlendShapeClip),
                    true
                );

                if (presetClip != m_manager.m_blendShapePresetClips[presetName]) {
                    if (presetClip is null) {
                        m_manager.SetBlendShapePresetClip(presetName, null);
                    } else {
                        m_manager.SetBlendShapePresetClip(presetName, $"{presetClip.name}.asset");
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Load current preset blend shape clips")) {
                m_manager.LoadCurrentPresetBlendShapeClips();
            }

            if (GUILayout.Button("Detect preset blend shape clips")) {
                m_manager.DetectPresetBlendShapeClips();
            }

            if (GUILayout.Button("Update preset settings")) {
                m_manager.UpdatePresetSettings();
            }
        }
    }
}
#endif // UNITY_EDITOR