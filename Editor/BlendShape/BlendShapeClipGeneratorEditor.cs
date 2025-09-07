using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRM;

namespace VST
{
    public class BlendShapeClipGeneratorEditor : EditorWindow
    {
        private BlendShapeClipGenerator m_generator              = null;
        private BlendShapeGroupManager  m_blendShapeGroupManager = null;
        private BlendShapeGroupDrawer   m_blendShapeGroupDrawer  = null;

        // field values
        private GameObject           m_vrmPrefab               = null;
        private UnityEngine.Object   m_exportFolder            = null;
        private bool                 m_optionFieldIsOpen       = false;
        private SkinnedMeshRenderer  m_skinnedMeshRenderer     = null;
        private bool                 m_removePrefixInClipName  = false;
        private bool                 m_skipIfClipAlreadyExists = false;

        [MenuItem("VRM0/VST/BlendShape/BlendShapeClipGenerator")]
        static void Init()
        {
            var window = GetWindowWithRect<BlendShapeClipGeneratorEditor>(new Rect(0, 0, 400, 560));
            window.Show();
        }

        void OnEnable()
        {
            m_generator              = new BlendShapeClipGenerator();
            m_blendShapeGroupManager = new BlendShapeGroupManager();
            m_blendShapeGroupDrawer  = new BlendShapeGroupDrawer(m_blendShapeGroupManager);
        }

        void OnGUI()
        {
            GUIStyle styleRadio = new GUIStyle(EditorStyles.radioButton);
            string[] sourceOptions = {"Prefab", "Mesh"};

            GUILayout.Space(10); // px
            m_vrmPrefab    = (GameObject)EditorGUILayout.ObjectField("VRM Prefab", m_vrmPrefab, typeof(GameObject), true);
            m_exportFolder = EditorGUILayout.ObjectField("Export Folder", m_exportFolder, typeof(UnityEngine.Object), true);

            GUILayout.Space(5); // px
            GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
            GUILayout.Space(5); // px

            /* blend shape selector */
            if (m_vrmPrefab != null) {
                // build blend shape group
                m_blendShapeGroupManager.BuildGroups(m_vrmPrefab, m_skinnedMeshRenderer);

                // draw ui
                m_blendShapeGroupDrawer.DrawBlendShapeGroups();
            }
            GUILayout.Space(20);

            /* options */
            m_optionFieldIsOpen = EditorGUILayout.BeginFoldoutHeaderGroup(m_optionFieldIsOpen, "Options");
            if (m_optionFieldIsOpen)
            {
                // source data
                EditorGUILayout.HelpBox("If you want to create blend shape clips from a mesh,\nset the mesh in this field.", MessageType.Info);
                m_skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                    "Source Mesh",
                    m_skinnedMeshRenderer,
                    typeof(SkinnedMeshRenderer),
                    true
                );

                GUILayout.Space(20); // px
                m_removePrefixInClipName  = EditorGUILayout.Toggle("Remove prefix in clip name", m_removePrefixInClipName);
                m_skipIfClipAlreadyExists = EditorGUILayout.Toggle("Skip if clip already exists", m_skipIfClipAlreadyExists);
            }

            GUILayout.Space(5); // px
            GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
            GUILayout.Space(5); // px

            /* buttons */
            if (GUILayout.Button("Create Blend Shape Clips")) {
                if (m_vrmPrefab == null || m_exportFolder == null) {
                    Debug.LogError("[VST] VRM Prefab and Export Folder must be set.");
                    return;
                }

                List<string> targetBlendShapeNames = m_blendShapeGroupManager.GetSelectedBlendShapeNames();

                // remove null blend shape clips
                VRMBlendShapeProxy blendShapeProxy = m_vrmPrefab.GetComponent<VRMBlendShapeProxy>();
                blendShapeProxy.BlendShapeAvatar.Clips.RemoveAll(item => item == null);

                m_generator.SetExportFolder(m_exportFolder);
                m_generator.SetOptionValues(
                    removePrefix  : m_removePrefixInClipName,
                    skipExistClips: m_skipIfClipAlreadyExists
                );

                if (m_skinnedMeshRenderer == null) m_generator.CreateBlendShapeClips(m_vrmPrefab, targetBlendShapeNames);
                else                               m_generator.CreateBlendShapeClips(m_vrmPrefab, m_skinnedMeshRenderer, targetBlendShapeNames);
            }

            if (GUILayout.Button("Remove Null Clips")) {
                VRMBlendShapeProxy blendShapeProxy = m_vrmPrefab.GetComponent<VRMBlendShapeProxy>();
                blendShapeProxy.BlendShapeAvatar.Clips.RemoveAll(item => item == null);
            }
        }
    }
}
