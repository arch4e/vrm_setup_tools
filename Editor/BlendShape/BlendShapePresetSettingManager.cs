#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRM;

namespace VST {
    public class BlendShapePresetSettingManager
    {
        public Dictionary<string, BlendShapeClip> m_blendShapePresetClips = new Dictionary<string, BlendShapeClip>();
        public UnityEngine.Object                 m_blendShapeClipFolder  = null;

        public BlendShapePresetSettingManager()
        {
            foreach (var preset in Enum.GetValues(typeof(BlendShapePreset))) {
                if (preset is BlendShapePreset.Unknown) continue;

                // initialize the dictionary with preset names and null values
                m_blendShapePresetClips.TryAdd(preset.ToString(), null);
            }
        }

        public void FindBlendShapeClipFolder()
        {
            // find blend shape clip folder in Assets
            string[] guids = AssetDatabase.FindAssets("blendshapes t:Folder");

            if (guids is not null && guids.Length > 0) {
                string folderPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_blendShapeClipFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);

                if (m_blendShapeClipFolder is not null) LoadCurrentPresetBlendShapeClips();
            }
        }

        public void DetectPresetBlendShapeClips()
        {
            if (!m_blendShapeClipFolder) return;

            // get all blend shape clip info in the folder
            DirectoryInfo directoryInfo = new DirectoryInfo(AssetDatabase.GetAssetPath(m_blendShapeClipFolder));
            FileInfo[]    fileInfoArray = directoryInfo.GetFiles("*.asset");

            foreach (var preset in Enum.GetValues(typeof(BlendShapePreset))) {
                if (preset is BlendShapePreset.Unknown) continue;

                foreach (var fileInfo in fileInfoArray) {
                    var clipName = fileInfo.Name;
                    if (clipName.ToLower().Contains($"{preset.ToString().ToLower()}.asset")) {
                        SetBlendShapePresetClip(preset.ToString(), clipName);
                    }
                }
            }
        }

        public void LoadCurrentPresetBlendShapeClips()
        {
            if (!m_blendShapeClipFolder) return;

            DirectoryInfo directoryInfo = new DirectoryInfo(AssetDatabase.GetAssetPath(m_blendShapeClipFolder));
            FileInfo[]    fileInfoArray = directoryInfo.GetFiles("*.asset");

            foreach (var fileInfo in fileInfoArray) {
                var clipPath = Path.Combine(AssetDatabase.GetAssetPath(m_blendShapeClipFolder), fileInfo.Name);
                var clip     = AssetDatabase.LoadAssetAtPath<BlendShapeClip>(clipPath);

                if (clip is not null) {
                    if (clip.Preset != BlendShapePreset.Unknown) {
                        SetBlendShapePresetClip(clip.Preset.ToString(), fileInfo.Name);
                    }
                }
            }
        }

        public void SetBlendShapePresetClip(string presetName, string clipFileName)
        {
            if (clipFileName is null) {
                m_blendShapePresetClips[presetName] = null;
                return;
            }

            if (m_blendShapePresetClips.ContainsKey(presetName)) {
                var clipPath = Path.Combine(AssetDatabase.GetAssetPath(m_blendShapeClipFolder), clipFileName);
                var clip     = AssetDatabase.LoadAssetAtPath<BlendShapeClip>(clipPath);

                if (clip) m_blendShapePresetClips[presetName] = clip;
                else      Debug.LogWarning($"Failed to load preset blend shape clip: {clipPath}");
            } else {
                Debug.LogWarning($"Preset name not found: {presetName}");
            }
        }

        public void UpdatePresetSettings()
        {
            if (!m_blendShapeClipFolder) return;

            DirectoryInfo directoryInfo = new DirectoryInfo(AssetDatabase.GetAssetPath(m_blendShapeClipFolder));
            FileInfo[] fileInfoArray    = directoryInfo.GetFiles("*.asset");

            // reset all clips to Unknown preset
            foreach (var fileInfo in fileInfoArray) {
                var clipPath = Path.Combine(AssetDatabase.GetAssetPath(m_blendShapeClipFolder), fileInfo.Name);
                var clip     = AssetDatabase.LoadAssetAtPath<BlendShapeClip>(clipPath);

                if (clip is not null) clip.Preset = BlendShapePreset.Unknown;
            }

            // update the clips with the current preset settings
            foreach (BlendShapePreset preset in Enum.GetValues(typeof(BlendShapePreset))) {
                if (preset is BlendShapePreset.Unknown) continue;

                BlendShapeClip clip = m_blendShapePresetClips[preset.ToString()];
                if (clip is not null) clip.Preset = preset;
            }
        }
    }
}
#endif // UNITY_EDITOR