// Util4 version 0.2.2
// repository: https://github.com/arch4e/util4_for_unity
// usage (ja): https://arch4e.com/posts/vrm-setup-utility-usage-ja/
// contact   : X      ) https://twitter.com/undefi4ed
//             Misskey) https://misskey.io/@arch4e

using System;
using System.IO;
using UniGLTF;
using UnityEngine;
using VRM;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VST_BlendShape_BlendShapeClips
{
    public class VST_BlendShape_BlendShapeClips : EditorWindow
    {
        private GameObject          vrmPrefab           = null;
        private BlendShapeAvatar    blendShapeAvatar    = null;
        private UnityEngine.Object  blendShapeFolder    = null;
        private SkinnedMeshRenderer skinnedMeshRenderer = null;
        private int selectedSourceIndex = 0;
        private int selectedExistClipOptionIndex = 0;

        [MenuItem("VRM0/VST/BlendShape/BlendShapeClips")]
        static void Init()
        {
            var window = GetWindowWithRect<VST_BlendShape_BlendShapeClips>(new Rect(0, 0, 400, 560));
            window.Show();
        }

        void OnGUI()
        {
            GUIStyle styleRadio = new GUIStyle(EditorStyles.radioButton);

            /* --- source selector --- */
            GUILayout.BeginHorizontal();
            GUILayout.Label("Blend Shape Source:");
            string[] sourceOptions = {"Prefab", "Mesh"};
            selectedSourceIndex = GUILayout.SelectionGrid(selectedSourceIndex, sourceOptions, 1, styleRadio);
            GUILayout.EndHorizontal();
            GUILayout.Space(10); // 5px

            /* --- exist clip option --- */
            GUILayout.BeginHorizontal();
            GUILayout.Label("Exist Blend Shape Clips:");
            string[] existBlendShapeClipOptions = {"Set Weight to 1 (from 0 or empty)", "Skip"};
            selectedExistClipOptionIndex = GUILayout.SelectionGrid(selectedExistClipOptionIndex, existBlendShapeClipOptions, 1, styleRadio);
            GUILayout.EndHorizontal();
            GUILayout.Space(10); // 10px

            /* --- Object Field --- */
            vrmPrefab = (GameObject)EditorGUILayout.ObjectField("VRM Prefab", vrmPrefab, typeof(GameObject), true);
            if (sourceOptions[selectedSourceIndex] == "Mesh")
            {
                skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Source Mesh", skinnedMeshRenderer,
                                                                                       typeof(SkinnedMeshRenderer), true);
            }
            blendShapeFolder = EditorGUILayout.ObjectField("Save Folder", blendShapeFolder, typeof(UnityEngine.Object), true);
            GUILayout.Space(5); // 5px

            /* --- Create Blend Shape Clips --- */
            if (GUILayout.Button("Create Blend Shape Clips"))
            {
                VRMBlendShapeProxy blendShapeProxy = vrmPrefab.GetComponent<VRMBlendShapeProxy>();
                blendShapeAvatar = blendShapeProxy.BlendShapeAvatar;

                if (sourceOptions[selectedSourceIndex] == "Prefab") {
                    SkinnedMeshRenderer[] renderers = vrmPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var renderer in renderers) CreateBlendShapeClipsFromSMR(renderer);
                } else if (sourceOptions[selectedSourceIndex] == "Mesh") CreateBlendShapeClipsFromSMR(skinnedMeshRenderer);
                Debug.Log("[VST] The creation of blend shape clips has been completed.");
            }

            if (GUILayout.Button("Remove Null Clips")) {
                VRMBlendShapeProxy blendShapeProxy = vrmPrefab.GetComponent<VRMBlendShapeProxy>();
                blendShapeProxy.BlendShapeAvatar.Clips.RemoveAll(item => item == null);
            }
        }

        private void CreateBlendShapeClipsFromSMR(SkinnedMeshRenderer renderer)
        {
            var    mesh     = renderer.sharedMesh;
            string meshName = renderer.name;

            // get mesh relative path
            GameObject meshParent   = FindMeshParentObject(vrmPrefab.transform, meshName);   // attention: possible duplication of names in child game objects in hierarchy
            string meshRelativePath = GetMeshRelativePath(meshParent);
            meshRelativePath        = meshRelativePath.Substring(vrmPrefab.name.Length + 1); // exclude prefab name + "/"

            for (int i = 0; i < mesh.blendShapeCount; ++i)
            {
                string savePath = AssetDatabase.GetAssetPath(blendShapeFolder);          // Assets/<path>/<to>/<blend shape dir>
                string dataPath = savePath + "/" + mesh.GetBlendShapeName(i) + ".asset"; // dir name + key name + .asset

                // skip processing when save directory is empty or asset file exists
                // "selectedExistClipOptionIndex == 1" means "Skip"
                if (string.IsNullOrEmpty(savePath) || (selectedExistClipOptionIndex == 1 && File.Exists(dataPath))) continue;

                // define blend shape binding
                BlendShapeBinding blendShapeBinding = new BlendShapeBinding();
                blendShapeBinding.RelativePath      = meshRelativePath;
                blendShapeBinding.Index             = i;
                blendShapeBinding.Weight            = 100;

                // add blend shape binding to blend shape clip
                BlendShapeBinding[] blendShapeBindings = { blendShapeBinding };
                int clipIndex = blendShapeAvatar.Clips.FindIndex(0, blendShapeAvatar.Clips.Count, x => x.name == mesh.GetBlendShapeName(i));
                if (clipIndex == -1) { // if the blend shape clip does not exist
                    // create new blend shape clip
                    var clip = BlendShapeAvatar.CreateBlendShapeClip(dataPath.ToUnityRelativePath());
                    blendShapeAvatar.Clips.Add(clip);
                    clip.Values = blendShapeBindings;

                    // notify Unity that the blendShapeAvatar has changed
                    EditorUtility.SetDirty(blendShapeAvatar);
                    EditorUtility.SetDirty(clip);
                } else { // if the blend shape clip exists
                    BlendShapeBinding[] blendShapeBindingValues = blendShapeAvatar.Clips[clipIndex].Values;
                    int blendShapeBindingValueIndex = Array.FindIndex(blendShapeBindingValues, x => x.RelativePath == meshRelativePath);

                    if (blendShapeBindingValues.Length == 0) {
                        blendShapeAvatar.Clips[clipIndex].Values = blendShapeBindings;
                    } else if (blendShapeBindingValueIndex == -1) {
                        // add the blend shape binding
                        // when the blend shape clip is exists, but blend shape binding is not exists
                        Array.Resize(ref blendShapeAvatar.Clips[clipIndex].Values, blendShapeAvatar.Clips[clipIndex].Values.Length + 1);
                        blendShapeAvatar.Clips[clipIndex].Values[blendShapeAvatar.Clips[clipIndex].Values.Length - 1] = blendShapeBinding;
                    } else if (blendShapeAvatar.Clips[clipIndex].Values[blendShapeBindingValueIndex].Weight == 0) {
                        // set the blend shape binding weight
                        // when the blend shape clip and the blend shape binding exist, and the weight value is 0
                        blendShapeAvatar.Clips[clipIndex].Values[blendShapeBindingValueIndex].Weight = 100;
                    }

                    // notify Unity that the blendShapeAvatar has changed
                    EditorUtility.SetDirty(blendShapeAvatar.Clips[clipIndex]);
                    EditorUtility.SetDirty(blendShapeAvatar);
                }
            }
        }


        private GameObject FindMeshParentObject(Transform transform, string meshName)
        {
            GameObject meshParentObject = null;
            for (int i = 0; i < transform.childCount && meshParentObject is null; i++)
            {
                Transform childTransform = transform.GetChild(i);
                if (childTransform.name == meshName) return childTransform.gameObject;
                else meshParentObject = FindMeshParentObject(childTransform, meshName);
            }

            return meshParentObject;
        }

        private string GetMeshRelativePath(GameObject o)
        {
            return GetMeshRelativePath(o.transform);
        }

        private string GetMeshRelativePath(Transform t)
        {
            string path   = t.name;
            var    parent = t.parent;

            while (parent)
            {
                path   = $"{parent.name}/{path}";
                parent = parent.parent;
            }

            return path;
        }
    }
}
