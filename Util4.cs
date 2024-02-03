using System;
using System.IO;
using UniGLTF;
using UnityEngine;
using VRM;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Util4
{
    public class Util4 : EditorWindow
    {
        private GameObject         avatarPrefab     = null;
        private BlendShapeAvatar   blendShapeObject = null;
        private UnityEngine.Object blendShapeFolder = null;
        private SkinnedMeshRenderer skinnedMeshRenderer = null;
        private int selectedSourceIndex = 0;
        private int selectedExistClipOptionIndex = 0;

        [MenuItem("Tools/Util-4")]
        static void Init()
        {
            var window = GetWindowWithRect<Util4>(new Rect(0, 0, 400, 560));
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
            GUILayout.Label("Exist Blend Shape Clip:");
            string[] existBlendShapeClipOptions = {"Skip", "Set Weight to 1 (from 0 or empty)"};
            selectedExistClipOptionIndex = GUILayout.SelectionGrid(selectedExistClipOptionIndex, existBlendShapeClipOptions, 1, styleRadio);
            GUILayout.EndHorizontal();
            GUILayout.Space(10); // 10px

            /* --- Object Field --- */
            avatarPrefab = (GameObject)EditorGUILayout.ObjectField("Avatar Prefab", avatarPrefab, typeof(GameObject), true);
            if (sourceOptions[selectedSourceIndex] == "Mesh")
            {
                skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Source Mesh", skinnedMeshRenderer,
                                                                                       typeof(SkinnedMeshRenderer), true);
            }
            blendShapeObject = (BlendShapeAvatar)EditorGUILayout.ObjectField("Blend Shape Object", blendShapeObject, typeof(UnityEngine.Object), true);
            blendShapeFolder = EditorGUILayout.ObjectField("Save Folder", blendShapeFolder, typeof(UnityEngine.Object), true);
            GUILayout.Space(5); // 5px

            /* --- Create Blend Shape Clips --- */
            if (GUILayout.Button("Create Blend Shape Clips"))
            {
                if (sourceOptions[selectedSourceIndex] == "Prefab") {
                    SkinnedMeshRenderer[] renderers = avatarPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var renderer in renderers) CreateBlendShapeClipsFromSMR(renderer);
                } else if (sourceOptions[selectedSourceIndex] == "Mesh") CreateBlendShapeClipsFromSMR(skinnedMeshRenderer);
            }

            if (GUILayout.Button("Remove Null Clips")) blendShapeObject.Clips.RemoveAll(item => item == null);
        }

        private void CreateBlendShapeClipsFromSMR(SkinnedMeshRenderer renderer)
        {
            var    mesh     = renderer.sharedMesh;
            string meshName = renderer.name;

            // get mesh relative path
            GameObject meshParent   = FindMeshParentObject(avatarPrefab.transform, meshName); // attention: possible duplication of names in child game objects in hierarchy
            string meshRelativePath = GetMeshRelativePath(meshParent);
            meshRelativePath        = meshRelativePath.Substring(avatarPrefab.name.Length + 1); // exclude prefab name + "/"

            for (int i = 0; i < mesh.blendShapeCount; ++i)
            {
                string savePath = AssetDatabase.GetAssetPath(blendShapeFolder);          // Assets/<path>/<to>/<blend shape dir>
                string dataPath = savePath + "/" + mesh.GetBlendShapeName(i) + ".asset"; // dir name + key name + .asset

                // skip processing when save directory is empty or asset file exists
                // "selectedExistClipOptionIndex == 0" means "Skip"
                if (string.IsNullOrEmpty(savePath) || (selectedExistClipOptionIndex == 0 && File.Exists(dataPath))) continue;

                int clipIndex = blendShapeObject.Clips.FindIndex(0, blendShapeObject.Clips.Count, x => x.name == mesh.GetBlendShapeName(i));
                if (clipIndex == -1) {
                    // create new blend shape clip
                    var clip = BlendShapeAvatar.CreateBlendShapeClip(dataPath.ToUnityRelativePath());
                    blendShapeObject.Clips.Add(clip);
                    EditorUtility.SetDirty(blendShapeObject);

                    // create blend shape binding
                    BlendShapeBinding blendShapeBinding = new BlendShapeBinding();
                    blendShapeBinding.RelativePath      = meshRelativePath;
                    blendShapeBinding.Index             = i;
                    blendShapeBinding.Weight            = 100;

                    // add blend shape binding to blend shape clip
                    BlendShapeBinding[] blendShapeBindings = { blendShapeBinding };
                    clip.Values = blendShapeBindings;
                } else {
                    BlendShapeBinding[] blendShapeBindingValues = blendShapeObject.Clips[clipIndex].Values;
                    int blendShapeBindingValueIndex = Array.FindIndex(blendShapeBindingValues, x => x.Index == i);
                    if (blendShapeBindingValueIndex == -1) {
                        // create blend shape binding
                        BlendShapeBinding blendShapeBinding = new BlendShapeBinding();
                        blendShapeBinding.RelativePath      = meshRelativePath;
                        blendShapeBinding.Index             = i;
                        blendShapeBinding.Weight            = 100;

                        // add blend shape binding to blend shape clip
                        BlendShapeBinding[] blendShapeBindings  = { blendShapeBinding };
                        blendShapeObject.Clips[clipIndex].Values = blendShapeBindings;
                    } else if (blendShapeObject.Clips[clipIndex].Values[blendShapeBindingValueIndex].Weight == 0) {
                        blendShapeObject.Clips[clipIndex].Values[blendShapeBindingValueIndex].Weight = 100;
                    } else {
                        int _blendShapeBindingValueIndex = Array.FindIndex(blendShapeBindingValues, x => x.RelativePath == meshRelativePath);

                        // create blend shape binding
                        BlendShapeBinding blendShapeBinding = new BlendShapeBinding();
                        blendShapeBinding.RelativePath      = meshRelativePath;
                        blendShapeBinding.Index             = i;
                        blendShapeBinding.Weight            = 100;

                        // add blend shape binding to blend shape clip
                        if (_blendShapeBindingValueIndex == -1) { // BlendShapeClip is exists, BlendShapeBinding is not exist
                            BlendShapeBinding[] blendShapeBindings = { blendShapeBinding };
                            Array.Resize(ref blendShapeObject.Clips[clipIndex].Values, blendShapeObject.Clips[clipIndex].Values.Length + 1);
                            blendShapeObject.Clips[clipIndex].Values[blendShapeObject.Clips[clipIndex].Values.Length - 1] = blendShapeBinding;
                        }
                    }
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
