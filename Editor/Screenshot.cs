using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System; // Enum.TryParse
using System.IO;
using VRM;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace VST_BlendShape_Screenshot {
    public class VST_BlendShape_Screenshot : EditorWindow
    {
        /* config */
        private enum SUPPORTED_FILE_FORMATS { PNG, JPG };
        private enum IMAGE_SIZE { Square_1k, Square_2k, Square_4k }; // todo: add Custom

        /* variables */
        private List<Camera> cameraObjects = new List<Camera>();
        private GameObject vrmPrefab = null; // del
        private BlendShapeAvatar blendShapeAvatar = null;
        private UnityEditor.DefaultAsset outputFolder = null;
        private ReorderableList cameraList;
        private SUPPORTED_FILE_FORMATS saveFileFormat;
        private IMAGE_SIZE imageSize;


        private System.Action onEditorUpdateAction = null;
        private bool finishedCaptureBlendShapeResults = false;

        [MenuItem("Tools/VRMSetupTools/BlendShape/Screenshot")]
        static void Init()
        {
            var window = GetWindowWithRect<VST_BlendShape_Screenshot>(new Rect(0, 0, 400, 560));
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize ReorderableList
            cameraList = new ReorderableList(cameraObjects, typeof(Camera), true, true, true, true);

            // Display header
            cameraList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Camera List");
            };

            // Display ObjectFields
            cameraList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                cameraObjects[index] = (Camera)EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    cameraObjects[index], typeof(Camera), true);
            };

            // Called when an element is added
            cameraList.onAddCallback = (ReorderableList list) =>
            {
                cameraObjects.Add(null);
            };

            // Called when an element is deleted
            cameraList.onRemoveCallback = (ReorderableList list) =>
            {
                cameraObjects.RemoveAt(list.index);
            };
        }

        void OnGUI()
        {
            GUILayout.Space(10); // px
            vrmPrefab = (GameObject)EditorGUILayout.ObjectField("VRM Prefab", vrmPrefab, typeof(GameObject), true); // del
            // blendShapeAvatar = (BlendShapeAvatar)EditorGUILayout.ObjectField("BlendShape File", blendShapeAvatar, typeof(BlendShapeAvatar), true);
            outputFolder = (UnityEditor.DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(UnityEditor.DefaultAsset), true);
            imageSize = (IMAGE_SIZE)EditorGUILayout.EnumPopup("Image Size", imageSize);
            saveFileFormat = (SUPPORTED_FILE_FORMATS)EditorGUILayout.EnumPopup("File Format", saveFileFormat);
            GUILayout.Space(20); // px

            cameraList.DoLayoutList();
            GUILayout.Space(10); // px

            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Capture BlendShape Results"))
            {
                RemoveDuplicateOrNullItems(cameraObjects); // uniq
                CaptureBlendShapeResults(cameraIndex: 0, blendShapeClipIndex: 0);
            }
            GUI.enabled = true;
            if (!EditorApplication.isPlaying) GUILayout.Label("! Enable when playing");
        }

        private void CaptureBlendShapeResults(int cameraIndex, int blendShapeClipIndex)
        {
            VRMBlendShapeProxy blendShapeProxy  = vrmPrefab.GetComponent<VRMBlendShapeProxy>();
            BlendShapeAvatar   blendShapeAvatar = blendShapeProxy.BlendShapeAvatar;

            // guard
            if (cameraIndex >= cameraObjects.Count)
            {
                finishedCaptureBlendShapeResults = true;
                Debug.Log("[VRMSetupTools] Screenshots have been captured.");
                return;
            } else finishedCaptureBlendShapeResults = false;

            string blendShapeName = blendShapeAvatar.Clips[blendShapeClipIndex].name.Replace("BlendShape.", "");;
            string fileName = cameraObjects[cameraIndex].gameObject.name + "_" + blendShapeName + "." + saveFileFormat.ToString().ToLower();

            SetBlendShapeProxyValue(blendShapeProxy, blendShapeName, 1.0f);
            SceneView.RepaintAll();

            WaitForBlendShapeUpdate(() =>
            {
                RenderImage(cameraObjects[cameraIndex], fileName);

                SetBlendShapeProxyValue(blendShapeProxy, blendShapeName, 0.0f);
                AssetDatabase.Refresh();

                if (++blendShapeClipIndex >= blendShapeAvatar.Clips.Count)
                {
                    blendShapeClipIndex = 0;
                    cameraIndex++;
                }
                CaptureBlendShapeResults(cameraIndex, blendShapeClipIndex);
            });
        }

        private void WaitForBlendShapeUpdate(System.Action onComplete)
        {
            // keep callback
            onEditorUpdateAction = onComplete;

            // add a callback that is called when the editor is updated.
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // wait one frame before executing the process
            if (onEditorUpdateAction != null)
            {
                onEditorUpdateAction.Invoke();
                if (finishedCaptureBlendShapeResults)
                {
                    onEditorUpdateAction = null;                 // clear callback
                    EditorApplication.update -= OnEditorUpdate;  // delete from update event
                }
            }
        }
        private void SetBlendShapeProxyValue(VRMBlendShapeProxy blendShapeProxy, string blendShapeName, float value)
        {
            if (Enum.TryParse(blendShapeName, out BlendShapePreset _blendShapePreset))
                blendShapeProxy.ImmediatelySetValue(_blendShapePreset, value);
            else
                blendShapeProxy.ImmediatelySetValue(BlendShapeKey.CreateUnknown(blendShapeName), value);
        }

        private void RemoveDuplicateOrNullItems(List<Camera> list)
        {
            HashSet<UnityEngine.Object> focusedObjects = new HashSet<UnityEngine.Object>();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != null && !focusedObjects.Add(list[i])) list.RemoveAt(i);
            }
        }

        public void RenderImage(Camera targetCamera, string fileName)
        {
            int imageWidth  = 1080;
            int imageHeight = 1080;

            if (imageSize == IMAGE_SIZE.Square_2k)
            {
                imageWidth  = 2160;
                imageHeight = 2160;
            }
            else if (imageSize == IMAGE_SIZE.Square_4k)
            {
                imageWidth  = 4320;
                imageHeight = 4320;        
            }

            // create RenderTexture & Attach to camera
            RenderTexture renderTexture = new RenderTexture(imageWidth, imageHeight, 24);
            targetCamera.targetTexture = renderTexture;
            targetCamera.Render();

            // read texture from RenderTexture
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
            texture.Apply();

            // free RenderTexture
            targetCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);

            // save texture to image file
            byte[] bytes = texture.EncodeToPNG(); // todo support jpg
            string path = Path.Combine(AssetDatabase.GetAssetPath(outputFolder), fileName);
            File.WriteAllBytes(path, bytes);
            Debug.Log("Saved Camera Image to: " + path);

            // free memory
            Destroy(texture);
        }
    }
}
