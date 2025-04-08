using UnityEditor;
using UnityEngine;
using Yueby.EditorWindowExtends.ProjectBrowserExtends.Core;
using System.Collections.Generic;

namespace Yueby.EditorWindowExtends.ProjectBrowserExtends.Drawer
{
    public class NewAssetDrawer : ProjectBrowserDrawer
    {
        public override string DrawerName => "New Asset Dot";
        public override string Tooltip => "Create a new asset in the project browser";
        protected override int DefaultOrder => 1;

        // 添加静态缓存字典，存储GUID到ProjectBrowserAsset的映射
        private static Dictionary<string, ProjectBrowserAsset> _assetCache = new Dictionary<string, ProjectBrowserAsset>();

        public override void OnProjectBrowserGUI(AssetItem item)
        {
            if (item.Asset == null)
                return;

            // 使用缓存查找资产，避免每帧都遍历资产树
            if (item.ProjectBrowserAsset == null)
            {
                if (!_assetCache.TryGetValue(item.Guid, out var cachedAsset))
                {
                    cachedAsset = AssetListener.Root.FindByGuid(item.Guid);
                    if (cachedAsset != null)
                    {
                        _assetCache[item.Guid] = cachedAsset;
                    }
                }
                item.ProjectBrowserAsset = cachedAsset;
            }

            if (item.ProjectBrowserAsset is not { HasNewAsset: true })
                return;

            const float dotRadius = 8f;

            var dotRect = new Rect(
                item.OriginRect.x - dotRadius * 0.5f,
                item.OriginRect.y - dotRadius,
                dotRadius * 2f,
                dotRadius * 2f
            );
            var dotStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green }
            };

            GUI.Label(dotRect, new GUIContent("●"), dotStyle);

            if (!item.IsHover)
                return;

            if (item.ProjectBrowserAsset.IsNewAsset && Event.current.type == EventType.MouseDown) item.ProjectBrowserAsset.SetNewAsset(false);

            if (!item.IsFolder) return;
            var content = EditorGUIUtility.IconContent(
                EditorGUIUtility.isProSkin ? "d_Package Manager" : "Package Manager"
            );
            content.tooltip = "Clear New Asset Dot";
            DrawIconButton(item, () => { ClearDot(item.ProjectBrowserAsset); }, content);
        }

        private void ClearDot(ProjectBrowserAsset asset)
        {
            AssetListener.ClearAsset(asset);
        }

        // 添加清理缓存的方法
        public static void ClearCache()
        {
            _assetCache.Clear();
        }
    }
}