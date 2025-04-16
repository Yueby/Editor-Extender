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

        // 缓存常量和样式，避免每帧重新创建
        private const float DotRadius = 8f;
        private static readonly Color DotColor = Color.green;
        private static readonly GUIContent DotContent = new GUIContent("●");
        private static GUIStyle _dotStyle;
        
        // 缓存图标内容，避免每次重新获取
        private static GUIContent _clearButtonContent;
        
        // 缓存常用路径的状态，避免频繁检查
        private static readonly Dictionary<string, bool> PathStatusCache = new Dictionary<string, bool>();
        private static int _lastClearedFrame = -1;
        
        // 初始化样式和内容
        private static void InitializeStyles()
        {
            if (_dotStyle == null)
            {
                _dotStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = DotColor }
                };
            }
            
            if (_clearButtonContent == null)
            {
                _clearButtonContent = EditorGUIUtility.IconContent(
                    EditorGUIUtility.isProSkin ? "d_Package Manager" : "Package Manager"
                );
                _clearButtonContent.tooltip = "Clear New Asset Dot";
            }
        }

        public override void OnProjectBrowserGUI(AssetItem item)
        {
            int currentFrame = Time.frameCount;
            
            // 快速检查：如果没有新资产且不是同一帧，清除缓存并返回
            if (AssetListener.NewAssetPaths.Count == 0)
            {
                if (_lastClearedFrame != currentFrame)
                {
                    PathStatusCache.Clear();
                    _lastClearedFrame = currentFrame;
                }
                return;
            }

            if (item.Asset == null || string.IsNullOrEmpty(item.Path))
                return;
                
            string path = item.Path;
            
            // 初始化样式（惰性初始化）
            InitializeStyles();
                
            // 检查缓存中是否有该路径的状态
            if (!PathStatusCache.TryGetValue(path, out bool shouldShowDot))
            {
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
                
                // 缓存结果
                PathStatusCache[path] = shouldShowDot;
            }

            if (!shouldShowDot)
                return;

            // 计算点标记的位置
            float yOffset = (item.OriginRect.height == 16) ? -DotRadius/2 : -DotRadius;
            
            var dotRect = new Rect(
                item.OriginRect.x - DotRadius * 0.5f,
                item.OriginRect.y + yOffset,
                DotRadius * 2f,
                DotRadius * 2f
            );

            // 绘制点标记
            GUI.Label(dotRect, DotContent, _dotStyle);

            if (!item.IsHover)
                return;

            // 处理点击事件
            if (Event.current.type == EventType.MouseDown)
            {
                if (AssetListener.IsNewAsset(path))
                {
                    AssetListener.SetAssetAsNew(path, false);
                    PathStatusCache.Clear(); // 清除缓存，因为状态已改变
                }
                
                // 强制刷新项目窗口
                RefreshProjectWindow();
            }

            if (!item.IsFolder) return;

            // 为文件夹绘制清除按钮
            DrawIconButton(item, () => 
            { 
                ClearDot(path);
            }, _clearButtonContent);
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
            // 清除缓存，因为状态已改变
            PathStatusCache.Clear();
            
            // 强制刷新项目窗口
            EditorApplication.RepaintProjectWindow();
        }
    }
}