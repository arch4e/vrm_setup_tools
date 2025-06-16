using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VST
{
    public class BlendShapeGroupDrawer
    {
        private BlendShapeGroupManager m_blendShapeGroupManager;
        private Vector2                m_scrollPosition;

        public BlendShapeGroupDrawer(BlendShapeGroupManager manager)
        {
            m_blendShapeGroupManager = manager;
        }

        public void DrawBlendShapeGroups()
        {
            EditorGUILayout.LabelField("Blend Shape Selector", EditorStyles.boldLabel);

            GUILayout.Space(10);
            int prefixDepth = (int)EditorGUILayout.IntField("Prefix Depth", m_blendShapeGroupManager.GetPrefixDepth());
            if (prefixDepth > 0 && m_blendShapeGroupManager.GetPrefixDepth() != prefixDepth) m_blendShapeGroupManager.SetPrefixDepth(prefixDepth);
            GUILayout.Space(5);

            BlendShapeGroups groups = m_blendShapeGroupManager.GetGroups();

            using (var scrollView = new EditorGUILayout.ScrollViewScope(m_scrollPosition, GUILayout.Height(200)))
            {
                foreach (var groupName in groups.Keys) DrawGroup(groupName: groupName, groupData: groups[groupName]);

                m_scrollPosition = scrollView.scrollPosition;
            }
        }

        private void DrawGroup(string groupName, BlendShapeGroupData groupData)
        {
            EditorGUILayout.BeginVertical("Box");

            DrawGroupHeader(groupName, groupData);

            if (groupData.isExpanded) DrawGroupItems(groupData);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawGroupHeader(string groupName, BlendShapeGroupData groupData)
        {
            EditorGUILayout.BeginHorizontal();
            // fold toggle
            bool isExpanded = EditorGUILayout.Foldout(
                groupData.isExpanded,
                $"{groupName} ({groupData.blendShapes.Count} blend shapes)"
            );
            if (groupData.isExpanded != isExpanded) m_blendShapeGroupManager.SetGroupExpanded(groupName, isExpanded);

            // group toggle
            bool isSelected = EditorGUILayout.Toggle(groupData.isSelected, GUILayout.Width(20));
            if (groupData.isSelected != isSelected) {
                if (isSelected) m_blendShapeGroupManager.SetAllGroupItems(groupName, true);
                else            m_blendShapeGroupManager.SetAllGroupItems(groupName, false);

                m_blendShapeGroupManager.SetGroupSelected(groupName, isSelected);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGroupItems(BlendShapeGroupData groupData)
        {
            EditorGUI.indentLevel++;

            List<string> blendShapeNames = new List<string> (groupData.blendShapes.Keys);
            foreach (var blendShapeName in blendShapeNames) {
                bool isSelected = EditorGUILayout.Toggle(blendShapeName, groupData.blendShapes[blendShapeName]);

                if (groupData.blendShapes[blendShapeName] != isSelected) {
                    m_blendShapeGroupManager.SetBlendShapeSelected(groupData, blendShapeName, isSelected);
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
