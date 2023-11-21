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

        [MenuItem("Tools/Util-4")]
        static void Init()
        {
            var window = GetWindowWithRect<Util4>(new Rect(0, 0, 400, 560));
            window.Show();
        }

        void OnGUI()
        {
            /* --- source selector --- */
            GUILayout.BeginHorizontal();
            GUILayout.Label("Blend Shape Source:");
            string[] sourceOptions = {"Prefab", "Mesh"};
            GUIStyle styleRadio = new GUIStyle(EditorStyles.radioButton);
            selectedSourceIndex = GUILayout.SelectionGrid(selectedSourceIndex, sourceOptions, 1, styleRadio);
            GUILayout.EndHorizontal();
            GUILayout.Space(10); // 5px

            /* --- Object Field --- */
            avatarPrefab = (GameObject)EditorGUILayout.ObjectField("Avater Prefab", avatarPrefab, typeof(GameObject), true);
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
            GameObject meshParent   = GameObject.Find(meshName);
            string meshRelativePath = GetMeshRelativePath(meshParent);
            meshRelativePath        = meshRelativePath.Substring(avatarPrefab.name.Length + 1); // exclude prefab name + "/"

            for (int i = 0; i < mesh.blendShapeCount; ++i)
            {
                string savePath = AssetDatabase.GetAssetPath(blendShapeFolder);          // Assets/<path>/<to>/<blend shape dir>
                string dataPath = savePath + "/" + mesh.GetBlendShapeName(i) + ".asset"; // dir name + key name + .asset

                // skip processing when save directory is empty or asset file exists
                if (string.IsNullOrEmpty(savePath) || File.Exists(dataPath)) continue;

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
            }
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

