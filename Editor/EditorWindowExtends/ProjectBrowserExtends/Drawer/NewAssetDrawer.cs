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

        public override void OnProjectBrowserGUI(AssetItem item)
        {
            if (item.Asset == null)
                return;

            string path = item.Path;
            
            // 查找资产
            var assetInfo = AssetListener.FindByPath(path);
            if (assetInfo == null)
            {
                // 由于懒加载，可能需要延迟查找
                // 如果在AllAssets中找不到，但确实是一个有效资产，强制尝试加载
                if (!string.IsNullOrEmpty(path))
                {
                    // 重新查找一次
                    assetInfo = AssetListener.FindByPath(path);
                }
                
                if (assetInfo == null)
                    return;
            }
                
            item.ProjectBrowserAsset = assetInfo; // 设置项目资产引用
            
            bool shouldShowDot = false;
            
            // 判断是否应该显示dot
            if (item.IsFolder)
            {
                // 直接检查文件夹是否包含新资产（包括自身）
                shouldShowDot = AssetListener.IsNewAsset(path) || AssetListener.DoesDirectoryContainNewAssets(path);
            }
            else
            {
                // 对于文件，只需检查资产是否为新资产
                shouldShowDot = AssetListener.IsNewAsset(path);
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
                    AssetListener.SetAssetAsNew(path, false);
                }
                
                // 强制刷新项目窗口
                RefreshProjectWindow();
            }

            if (!item.IsFolder) return;
            var content = EditorGUIUtility.IconContent(
                EditorGUIUtility.isProSkin ? "d_Package Manager" : "Package Manager"
            );
            content.tooltip = "Clear New Asset Dot";
            DrawIconButton(item, () => { ClearDot(path); }, content);
        }

        private void ClearDot(string path)
        {
            // 清除资产标记
            AssetListener.ClearAsset(path);
            
            // 强制刷新项目窗口
            RefreshProjectWindow();
        }

        // 刷新项目窗口
        public static void RefreshProjectWindow()
        {
            // 强制刷新项目窗口
            EditorApplication.RepaintProjectWindow();
        }
    }
}