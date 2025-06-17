using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal; // ReorderableList
using UnityEngine;

namespace VST {
    public class PreviewRendererEditor : EditorWindow
    {
        private PreviewRenderer                        m_renderer       = new PreviewRenderer();
        private GameObject                             m_vrmPrefab      = null;
        private UnityEditor.DefaultAsset               m_exportFolder   = null;
        private List<Camera>                           m_cameraObjects  = new List<Camera>();
        private ReorderableList                        m_cameraList     = null;
        private PreviewRenderer.IMAGE_SIZE             m_imageSize      = PreviewRenderer.IMAGE_SIZE.Square_1k;
        private int                                    m_imageHeight    = 1024,  // default 1k
                                                       m_imageWidth     = 1024;  // default 1k
        private PreviewRenderer.SUPPORTED_FILE_FORMATS m_saveFileFormat = PreviewRenderer.SUPPORTED_FILE_FORMATS.PNG;

        [MenuItem("VRM0/VST/BlendShape/Screenshot")]
        static void Init()
        {
            // window
            var window = GetWindowWithRect<PreviewRendererEditor>(new Rect(0, 0, 400, 560));
            window.Show();
        }

        private void OnEnable()
        {
            // initialize ReorderableList
            m_cameraList = new ReorderableList(m_cameraObjects, typeof(Camera), true, true, true, true);

            // display header
            m_cameraList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Camera List");
            };

            // display ObjectFields
            m_cameraList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                m_cameraObjects[index] = (Camera)EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    m_cameraObjects[index], typeof(Camera), true);
            };

            // called when an element is added
            m_cameraList.onAddCallback = (ReorderableList list) =>
            {
                m_cameraObjects.Add(null);
            };

            // called when an element is deleted
            m_cameraList.onRemoveCallback = (ReorderableList list) =>
            {
                m_cameraObjects.RemoveAt(list.index);
            };
        }

        void OnGUI()
        {
            GUILayout.Space(10); // px
            m_vrmPrefab      = (GameObject)EditorGUILayout.ObjectField("VRM Prefab", m_vrmPrefab, typeof(GameObject), true);
            m_exportFolder   = (UnityEditor.DefaultAsset)EditorGUILayout.ObjectField("Export Folder", m_exportFolder, typeof(UnityEditor.DefaultAsset), true);
            m_imageSize      = (PreviewRenderer.IMAGE_SIZE)EditorGUILayout.EnumPopup("Image Size", m_imageSize);

            if (m_imageSize == PreviewRenderer.IMAGE_SIZE.Custom) {
                EditorGUI.indentLevel++;
                m_imageHeight = (int)EditorGUILayout.IntField("Height", m_imageHeight);
                m_imageWidth  = (int)EditorGUILayout.IntField("Width" , m_imageWidth );
                EditorGUI.indentLevel--;

                if (m_renderer.m_imageHeight != m_imageHeight || m_renderer.m_imageWidth != m_imageWidth) {
                    m_renderer.SetImageSize(m_imageWidth, m_imageHeight);
                }
            }

            m_saveFileFormat = (PreviewRenderer.SUPPORTED_FILE_FORMATS)EditorGUILayout.EnumPopup("File Format", m_saveFileFormat);
            GUILayout.Space(20); // px

            m_cameraList.DoLayoutList();
            GUILayout.Space(10); // px

            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Capture BlendShape Results"))
            {
                RemoveDuplicateOrNullItemsFromList(m_cameraObjects);  // uniq

                m_renderer.SetExportFolder(m_exportFolder);
                m_renderer.SetImageSize(m_imageSize);
                m_renderer.SetSaveFileFormat(m_saveFileFormat);
                m_renderer.SetCameraObjects(m_cameraObjects);

                m_renderer.ExportBlendShapeResults(vrmPrefab: m_vrmPrefab, cameraIndex: 0, blendShapeClipIndex: 0);
            }
            GUI.enabled = true;
            if (!EditorApplication.isPlaying) EditorGUILayout.HelpBox("Enable when playing", MessageType.Info);
        }

        private void RemoveDuplicateOrNullItemsFromList(List<Camera> list)
        {
            HashSet<UnityEngine.Object> focusedObjects = new HashSet<UnityEngine.Object>();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != null && !focusedObjects.Add(list[i])) list.RemoveAt(i);
            }
        }
    }
}
