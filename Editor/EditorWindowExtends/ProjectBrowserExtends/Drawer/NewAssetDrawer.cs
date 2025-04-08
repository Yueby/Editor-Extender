using UnityEditor;
using UnityEngine;
using Yueby.EditorWindowExtends.ProjectBrowserExtends.Core;
using System.Collections.Generic;
using System.Linq;

namespace Yueby.EditorWindowExtends.ProjectBrowserExtends.Drawer
{
    public class NewAssetDrawer : ProjectBrowserDrawer
    {
        public override string DrawerName => "New Asset Dot";
        public override string Tooltip => "Create a new asset in the project browser";
        protected override int DefaultOrder => 1;

        // 文件夹状态缓存，避免频繁检查
        private static Dictionary<string, bool> _folderStatusCache = new Dictionary<string, bool>();

        public override void OnProjectBrowserGUI(AssetItem item)
        {
            if (item.Asset == null)
                return;

            // 查找资产
            var assetInfo = AssetListener.FindByGuid(item.Guid);
            if (assetInfo == null)
            {
                // 由于懒加载，可能需要延迟查找
                // 如果在AllAssets中找不到，但确实是一个有效资产，强制尝试加载
                string path = AssetDatabase.GUIDToAssetPath(item.Guid);
                if (!string.IsNullOrEmpty(path) && 
                    (path.StartsWith("Assets/") || path.StartsWith("Packages/")))
                {
                    // 重新查找一次
                    assetInfo = AssetListener.FindByGuid(item.Guid);
                }
                
                if (assetInfo == null)
                    return;
            }
                
            item.ProjectBrowserAsset = assetInfo; // 设置项目资产引用
            
            // 首先直接检查是否在NewAssets字典中
            bool isNewAsset = AssetListener.NewAssets.ContainsKey(item.Guid);
            
            bool shouldShowDot = false;
            
            // 判断是否应该显示dot
            if (item.IsFolder)
            {
                // 直接在NewAssets中找到，则无需计算
                if (isNewAsset)
                {
                    shouldShowDot = true;
                }
                else
                {
                    // 检查是否有缓存
                    if (!_folderStatusCache.TryGetValue(item.Guid, out shouldShowDot))
                    {
                        // 计算文件夹是否包含新资产
                        shouldShowDot = AssetListener.DoesDirectoryContainNewAssets(item.Guid);
                        _folderStatusCache[item.Guid] = shouldShowDot;
                    }
                }
            }
            else
            {
                // 对于文件，优先使用NewAssets字典，其次使用IsNewAsset属性
                shouldShowDot = isNewAsset || assetInfo.IsNewAsset;
            }

            if (!shouldShowDot)
                return;

            const float dotRadius = 8f;

            // 计算点标记的位置
            float yOffset = -dotRadius;

            if(item.OriginRect.height == 16){
                yOffset = -dotRadius/2;
            }
            
            var dotRect = new Rect(
                item.OriginRect.x - dotRadius * 0.5f,
                item.OriginRect.y + yOffset,
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

            // 处理点击事件
            if (Event.current.type == EventType.MouseDown)
            {
                if(assetInfo.IsNewAsset)
                {
                    AssetListener.SetAssetAsNew(item.Guid, false);
                }
                
                 ClearCache(); // 清理缓存确保UI更新
            }

            if (!item.IsFolder) return;
            var content = EditorGUIUtility.IconContent(
                EditorGUIUtility.isProSkin ? "d_Package Manager" : "Package Manager"
            );
            content.tooltip = "Clear New Asset Dot";
            DrawIconButton(item, () => { ClearDot(item.Guid); }, content);
        }

        private void ClearDot(string guid)
        {
            // 清除资产标记
            AssetListener.ClearAsset(guid);
            
            // 直接清空所有缓存，确保视图刷新
            ClearCache();
        }

        // 添加清理缓存的方法
        public static void ClearCache()
        {
            // 完全清空文件夹状态缓存，确保下次重新计算
            _folderStatusCache.Clear();
            
            // 强制刷新项目窗口
            EditorApplication.RepaintProjectWindow();
        }
    }
}