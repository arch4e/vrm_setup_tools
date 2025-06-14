using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // string.Contains
using UniGLTF;     // string.ToUnityRelativePath()
using UnityEditor;
using UnityEngine;
using VRM;

namespace VST
{
    public class BlendShapeClipGenerator
    {
        private string[] SUFFIX_LIST = new string[] {
            "l", "r", "left" , "right",
            "open"  , "close",
            "up"    , "down"
        };

        private UnityEngine.Object m_exportFolder = null;

        // options
        private bool m_removePrefixInClipName  = false;
        private bool m_skipIfClipAlreadyExists = false;

        public void CreateBlendShapeClips(GameObject vrmPrefab)
        {
            SkinnedMeshRenderer[] renderers = vrmPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var renderer in renderers) CreateBlendShapeClips(vrmPrefab, renderer);

            Debug.Log("[VST] The creation of blend shape clips has been completed.");
        }

        public void CreateBlendShapeClips(GameObject vrmPrefab, SkinnedMeshRenderer renderer)
        {
            VRMBlendShapeProxy blendShapeProxy  = vrmPrefab.GetComponent<VRMBlendShapeProxy>();
            BlendShapeAvatar   blendShapeAvatar = blendShapeProxy.BlendShapeAvatar;
            MeshUtil           meshUtil         = new MeshUtil();
            Mesh               mesh             = renderer.sharedMesh;
            string             meshName         = renderer.name;
            string             savePath         = AssetDatabase.GetAssetPath(m_exportFolder);              // Assets/<path>/<to>/<blend shape dir>

            // get mesh relative path
            GameObject meshParent       = meshUtil.FindMeshParentObject(vrmPrefab.transform, meshName);    // attention: possible duplication of names in child game objects in hierarchy
            string     meshRelativePath = meshUtil.GetMeshRelativePath(meshParent);
            meshRelativePath            = meshRelativePath.Substring(vrmPrefab.name.Length + 1);           // exclude prefab name + "/"

            for (int i = 0; i < mesh.blendShapeCount; ++i) {
                try {
                    string blendShapeName = mesh.GetBlendShapeName(i);
                    string clipName       = blendShapeName;
                    string dataPath       = savePath + "/" + blendShapeName + ".asset";    // dir name + key name + .asset

                    // skip processing when save directory is empty or blend shape clip already exists
                    if (string.IsNullOrEmpty(savePath) || (m_skipIfClipAlreadyExists && File.Exists(dataPath))) continue;

                    // find blend shape binding index
                    int blendShapeBindingIndex = renderer.sharedMesh.GetBlendShapeIndex(blendShapeName);

                    // exit when the blend shape binding does not exist
                    if (blendShapeBindingIndex == -1) continue;

                    // if prefix remove option is enabled, remove the prefix from the blend shape name
                    if (m_removePrefixInClipName)
                    {
                        clipName = GetClipName(blendShapeName.Split('_'));
                        dataPath = savePath + "/" + clipName + ".asset";    // dir name + key name + .asset
                    }

                    // define new blend shape binding
                    BlendShapeBinding blendShapeBinding = new BlendShapeBinding();
                    blendShapeBinding.RelativePath      = meshRelativePath;
                    blendShapeBinding.Index             = blendShapeBindingIndex;
                    blendShapeBinding.Weight            = 100;

                    // add blend shape binding to blend shape clip
                    BlendShapeBinding[] blendShapeBindings = { blendShapeBinding };
                    int clipIndex = blendShapeAvatar.Clips.FindIndex(x => x.name == clipName);
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
                        int blendShapeBindingValueIndex = Array.FindIndex(blendShapeBindingValues, x => x.RelativePath == meshRelativePath && x.Index == blendShapeBindingIndex);

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
                } catch (Exception e) {
                    Debug.LogError($"[VST] Failed to create blend shape clip for '{meshName}' with blend shape '{mesh.GetBlendShapeName(i)}'\n{e.Message}");
                }
            }
        }

        public void SetExportFolder(UnityEngine.Object exportFolder)
        {
            m_exportFolder = exportFolder;
        }

        public void SetOptionValues(bool removePrefix, bool skipExistClips)
        {
            m_removePrefixInClipName  = removePrefix;
            m_skipIfClipAlreadyExists = skipExistClips;
        }

        private string GetClipName() { return ""; }

        private string GetClipName(string[] splitBlendShapeName)
        {
            string clipName = splitBlendShapeName[splitBlendShapeName.Length - 1];

            if (splitBlendShapeName.Length >=2 && SUFFIX_LIST.Contains(clipName.ToLower())) {
                return GetClipName(splitBlendShapeName[..^1]) + "_" + clipName;
            } else return clipName;
        }
    }
}
