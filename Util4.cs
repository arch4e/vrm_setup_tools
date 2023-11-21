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

        [MenuItem("Tools/Util-4")]
        static void Init()
        {
            var window = GetWindowWithRect<Util4>(new Rect(0, 0, 400, 560));
            window.Show();
        }

        void OnGUI()
        {
            avatarPrefab     = (GameObject)EditorGUILayout.ObjectField("Avater Prefab", avatarPrefab, typeof(GameObject), true);
            blendShapeObject = (BlendShapeAvatar)EditorGUILayout.ObjectField("Blend Shape Object", blendShapeObject, typeof(UnityEngine.Object), true);
            blendShapeFolder = EditorGUILayout.ObjectField("Save Folder", blendShapeFolder, typeof(UnityEngine.Object), true);
            GUILayout.Space(5); // 5px

            if (GUILayout.Button("Create Blend Shape Clips"))
            {
                SkinnedMeshRenderer[] renderers = avatarPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (var renderer in renderers)
                {
                    var    mesh     = renderer.sharedMesh;
                    string meshName = renderer.name;

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
                        blendShapeBinding.RelativePath      = meshName;
                        blendShapeBinding.Index             = i;
                        blendShapeBinding.Weight            = 100;

                        // add blend shape binding to blend shape clip
                        BlendShapeBinding[] blendShapeBindings = { blendShapeBinding };
                        clip.Values = blendShapeBindings;
                    }
                }
            }
        }
    }
}
