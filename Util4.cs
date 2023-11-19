using UnityEngine;

using VRM;
using UniGLTF;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Util4
{
    public class Util4 : EditorWindow
    {
        private GameObject avatarPrefab = null;
        private BlendShapeAvatar blendShapeObject = null;
        private UnityEngine.Object blendShapeFolder = null;
        private Vector2 shapeKeysScrollPosition = Vector2.zero;

        [MenuItem("Tools/Util-4")]
        static void Init()
        {
            var window = GetWindowWithRect<Util4>(new Rect(0, 0, 165, 100));
            window.Show();
        }

        void OnGUI()
        {
            avatarPrefab     = (GameObject)EditorGUILayout.ObjectField("Avatar Prefab", avatarPrefab, typeof(GameObject), true);
            blendShapeObject = (BlendShapeAvatar)EditorGUILayout.ObjectField("Blend Shape Object", blendShapeObject, typeof(UnityEngine.Object), true);
            blendShapeFolder = EditorGUILayout.ObjectField("Blend Shape Folder", blendShapeFolder, typeof(UnityEngine.Object), true);

            using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(shapeKeysScrollPosition, EditorStyles.helpBox))
            {
                shapeKeysScrollPosition = scroll.scrollPosition;
            }

            if (GUILayout.Button("Create Blend Shape Clips"))
            {
                SkinnedMeshRenderer[] renderers = avatarPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in renderers)
                {
                    var mesh = renderer.sharedMesh;
                    string meshName = renderer.name;

                    for (int i = 0; i < mesh.blendShapeCount; ++i)
                    {
                        string dir = AssetDatabase.GetAssetPath(blendShapeFolder);  // Assets/SavedObject
                        string path = dir + "/" + mesh.GetBlendShapeName(i) + ".asset";  // ディレクトリ + /<シェイプキーの名前>.asset;

                        if (!string.IsNullOrEmpty(path))
                        {
                            // 保存と追加
                            var clip = BlendShapeAvatar.CreateBlendShapeClip(path.ToUnityRelativePath());
                            blendShapeObject.Clips.Add(clip);
                            EditorUtility.SetDirty(blendShapeObject);


                            // シェイプキーの設定
                            BlendShapeBinding blendShapeBinding = new BlendShapeBinding();
                            blendShapeBinding.RelativePath = meshName;
                            blendShapeBinding.Index = i;
                            blendShapeBinding.Weight = 100;

                            BlendShapeBinding[] bindings = { blendShapeBinding };
                            clip.Values = bindings;
                        }
                    }
                }
            }
        }
    }
}
