using System.Collections.Generic;
using System.Linq; // List.Any
using UnityEngine;

namespace VST
{
    // [System.Serializable]
    public class BlendShapeGroups : Dictionary<string, BlendShapeGroupData> {}

    public class BuildSource
    {
        public GameObject          vrmPrefab;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public int                 prefixDepth;

        public BuildSource()
        {
            vrmPrefab           = null;
            skinnedMeshRenderer = null;
            prefixDepth         = 1;
        }
    }

    public class BlendShapeGroupData
    {
        public Dictionary<string, bool> blendShapes;
        public bool                     isExpanded;
        public bool                     isSelected;

        public BlendShapeGroupData()
        {
            blendShapes = new Dictionary<string, bool>();
            isExpanded  = false;
            isSelected  = true;
        }
    }

    public class BlendShapeGroupManager
    {
        private BuildSource      m_lastBuildSource = new BuildSource();
        private BlendShapeGroups m_group           = new BlendShapeGroups();
        private int              m_prefixDepth     = 1;

        public void BuildGroups(GameObject vrmPrefab, SkinnedMeshRenderer skinnedMeshRenderer = null)
        {
            if (vrmPrefab == null) return;

            // build source check
            if (m_lastBuildSource.vrmPrefab == vrmPrefab
                && m_lastBuildSource.skinnedMeshRenderer == skinnedMeshRenderer
                && m_lastBuildSource.prefixDepth == m_prefixDepth
            ) return;

            // init
            m_lastBuildSource.vrmPrefab           = vrmPrefab;
            m_lastBuildSource.skinnedMeshRenderer = skinnedMeshRenderer;
            m_lastBuildSource.prefixDepth         = m_prefixDepth;
            m_group.Clear();

            SkinnedMeshRenderer[] renderers = null;
            if (skinnedMeshRenderer == null) renderers = vrmPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            else                             renderers = new SkinnedMeshRenderer[] { skinnedMeshRenderer };

            foreach (var renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null) continue;

                for (int i = 0; i < mesh.blendShapeCount; ++i) {
                    string blendShapeName = mesh.GetBlendShapeName(i);
                    string groupName      = "Other";

                    // prefix group
                    string prefix = ExtractPrefix(blendShapeName);
                    groupName = prefix != null ? prefix : groupName;

                    // other group
                    if (!m_group.ContainsKey(groupName)) m_group[groupName] = new BlendShapeGroupData();

                    m_group[groupName].blendShapes[blendShapeName] = true;
                }
            }
        }

        private string ExtractPrefix(string str)
        {
            int separatorIndex = NthIndexOf(str, "_", m_prefixDepth);
            return separatorIndex > 0 ? str.Substring(0, separatorIndex) : null;
        }

        public BlendShapeGroups GetGroups()
        {
            return m_group;
        }

        public int GetPrefixDepth()
        {
            return m_prefixDepth;
        }

        public List<string> GetSelectedBlendShapeNames()
        {
            List<string> selectBlendShapeNames = new List<string>();

            foreach(var groupName in m_group.Keys) {
                foreach(var blendShapeName in m_group[groupName].blendShapes.Keys) {
                    if (m_group[groupName].blendShapes[blendShapeName]) selectBlendShapeNames.Add(blendShapeName);
                }
            }

            return selectBlendShapeNames;
        }

        public void SetBlendShapeSelected(BlendShapeGroupData groupData, string blendShapeName, bool isSelected)
        {
            if (groupData.blendShapes.ContainsKey(blendShapeName)) {
                groupData.blendShapes[blendShapeName] = isSelected;
            }
        }

        public void SetGroupExpanded(string groupName, bool isExpanded)
        {
            if (m_group.ContainsKey(groupName)) m_group[groupName].isExpanded = isExpanded;
        }

        public void SetGroupSelected(string groupName, bool isSelected)
        {
            if (m_group.ContainsKey(groupName)) m_group[groupName].isSelected = isSelected;
        }

        public void SetPrefixDepth(int depth)
        {
            if (depth > 0) {
                m_prefixDepth = depth;
                BuildGroups(m_lastBuildSource.vrmPrefab, m_lastBuildSource.skinnedMeshRenderer);
            }
        }

        public void SetAllGroupItems(string groupName, bool value)
        {
            List<string> blendShapeNames = new List<string> (m_group[groupName].blendShapes.Keys);
            foreach (var blendShapeName in blendShapeNames) {
                m_group[groupName].blendShapes[blendShapeName] = value;
            }
        }

        private int NthIndexOf(string str, string key, int depth)
        {
            List<int> indexes = new List<int>();

            for (int i = str.IndexOf(key); i > -1; i = str.IndexOf(key, i + 1)) indexes.Add(i);

            if (indexes.Count >= depth) return indexes[depth - 1];
            else                        return -1;
        }
    }
}
