using System.Collections.Generic;
using System; // Enum.TryParse
using System.IO;
using UnityEditor;
using UnityEngine;
using VRM;

namespace VST {
    public class PreviewRenderer
    {
        /* config */
        public enum IMAGE_SIZE             { Square_1k, Square_2k, Square_4k }; // todo: add Custom
        public enum SUPPORTED_FILE_FORMATS { PNG, JPG };

        /* variables */
        private List<Camera>             m_cameraObjects                    = new List<Camera>();
        private UnityEditor.DefaultAsset m_exportFolder                     = null;
        private int                      m_imageHeight, m_imageWidth        = 1024; // default 1k
        private SUPPORTED_FILE_FORMATS   m_saveFileFormat                   = SUPPORTED_FILE_FORMATS.PNG;
        private System.Action            m_onEditorUpdateAction             = null;
        private bool                     m_finishedCaptureBlendShapeResults = false;

        public void ExportBlendShapeResults(GameObject vrmPrefab,int cameraIndex, int blendShapeClipIndex)
        {
            VRMBlendShapeProxy blendShapeProxy  = vrmPrefab.GetComponent<VRMBlendShapeProxy>();
            BlendShapeAvatar   blendShapeAvatar = blendShapeProxy.BlendShapeAvatar;

            // guard
            if (cameraIndex >= m_cameraObjects.Count)
            {
                m_finishedCaptureBlendShapeResults = true;
                Debug.Log("[VST] Screenshots have been captured.");
                return;
            } else m_finishedCaptureBlendShapeResults = false;

            // remove null blend shape clips
            blendShapeProxy.BlendShapeAvatar.Clips.RemoveAll(item => item == null);

            string blendShapeName = blendShapeAvatar.Clips[blendShapeClipIndex].name.Replace("BlendShape.", "");;
            string fileName       = m_cameraObjects[cameraIndex].gameObject.name + "_" + blendShapeName + "." + m_saveFileFormat.ToString().ToLower();

            SetBlendShapeProxyValue(blendShapeProxy, blendShapeName, 1.0f);
            SceneView.RepaintAll();

            WaitForBlendShapeUpdate(() =>
            {
                RenderImage(m_cameraObjects[cameraIndex], fileName);

                SetBlendShapeProxyValue(blendShapeProxy, blendShapeName, 0.0f);
                AssetDatabase.Refresh();

                if (++blendShapeClipIndex >= blendShapeAvatar.Clips.Count)
                {
                    blendShapeClipIndex = 0;
                    cameraIndex++;
                }
                ExportBlendShapeResults(vrmPrefab, cameraIndex, blendShapeClipIndex);
            });
        }

        private void WaitForBlendShapeUpdate(System.Action onComplete)
        {
            // keep callback
            m_onEditorUpdateAction = onComplete;

            // add a callback that is called when the editor is updated.
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // wait one frame before executing the process
            if (m_onEditorUpdateAction != null)
            {
                m_onEditorUpdateAction.Invoke();
                if (m_finishedCaptureBlendShapeResults)
                {
                    m_onEditorUpdateAction = null;                 // clear callback
                    EditorApplication.update -= OnEditorUpdate;  // delete from update event
                }
            }
        }
        private void SetBlendShapeProxyValue(VRMBlendShapeProxy blendShapeProxy, string blendShapeName, float value)
        {
            if (Enum.TryParse(blendShapeName, out BlendShapePreset _blendShapePreset))
                blendShapeProxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(_blendShapePreset), value);
            else
                blendShapeProxy.ImmediatelySetValue(BlendShapeKey.CreateUnknown(blendShapeName), value);
        }

        public void RenderImage(Camera targetCamera, string fileName)
        {
            try {
                // create RenderTexture & Attach to camera
                RenderTexture renderTexture = new RenderTexture(m_imageWidth, m_imageHeight, 24);
                targetCamera.targetTexture  = renderTexture;
                targetCamera.Render();

                // read texture from RenderTexture
                RenderTexture.active = renderTexture;
                Texture2D texture    = new Texture2D(m_imageWidth, m_imageHeight, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, m_imageWidth, m_imageHeight), 0, 0);
                texture.Apply();

                // free RenderTexture
                targetCamera.targetTexture = null;
                RenderTexture.active       = null;
                UnityEngine.Object.Destroy(renderTexture);

                // save texture to image file
                byte[] bytes = texture.EncodeToPNG(); // todo support jpg
                string path  = Path.Combine(AssetDatabase.GetAssetPath(m_exportFolder), fileName);
                File.WriteAllBytes(path, bytes);

                // free memory
                UnityEngine.Object.Destroy(texture);
            } catch (Exception e) {
                Debug.LogError("[VST] Error while rendering image: " + e.Message);
            }
        }

        public void SetExportFolder(UnityEditor.DefaultAsset exportFolder)
        {
            m_exportFolder = exportFolder;
        }

        public void SetImageSize(IMAGE_SIZE imageSize)
        {
            if (imageSize == IMAGE_SIZE.Square_1k)
            {
                m_imageHeight = 1024;
                m_imageWidth  = 1024;
            }
            else if (imageSize == IMAGE_SIZE.Square_2k)
            {
                m_imageHeight = 2048;
                m_imageWidth  = 2048;
            }
            else if (imageSize == IMAGE_SIZE.Square_4k)
            {
                m_imageHeight = 4096;
                m_imageWidth  = 4096;
            }
        }

        public void SetSaveFileFormat(SUPPORTED_FILE_FORMATS saveFileFormat)
        {
            m_saveFileFormat = saveFileFormat;
        }

        public void SetCameraObjects(List<Camera> cameraObjects)
        {
            m_cameraObjects = cameraObjects;
        }
    }
}
