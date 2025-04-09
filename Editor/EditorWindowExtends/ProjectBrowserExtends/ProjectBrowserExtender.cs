using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yueby.Core.Utils;
using Yueby.EditorWindowExtends.Core;
using Yueby.EditorWindowExtends.ProjectBrowserExtends.Core;

namespace Yueby.EditorWindowExtends.ProjectBrowserExtends
{
    [InitializeOnLoad]
    public sealed class ProjectBrowserExtender
        : EditorExtender<ProjectBrowserExtender, ProjectBrowserDrawer>
    {
        public override string Name => "ProjectWindow";

        public const float RightOffset = 2f;
        // 使用路径作为键
        private Dictionary<string, AssetItem> _assetItems;

        private EditorWindow _mouseOverWindow;
        private string _lastHoveredPath;

        public static ProjectBrowserExtender Instance { get; private set; }

        static ProjectBrowserExtender()
        {
            Instance = new ProjectBrowserExtender();
        }

        public ProjectBrowserExtender()
        {
            SetEnable(IsEnabled);
        }

        public override void SetEnable(bool value)
        {
            if (!value)
            {
                EditorApplication.projectWindowItemOnGUI -= OnProjectBrowserItemGUI;
                EditorApplication.update -= OnUpdate;
            }
            else
            {
                EditorApplication.projectWindowItemOnGUI -= OnProjectBrowserItemGUI;
                EditorApplication.projectWindowItemOnGUI += OnProjectBrowserItemGUI;

                EditorApplication.update -= OnUpdate;
                EditorApplication.update += OnUpdate;
            }

            base.SetEnable(value);
        }

        private void OnUpdate()
        {
            _mouseOverWindow = EditorWindow.mouseOverWindow;
        }

        public static void OnProjectBrowserObjectAreaItemGUI(int instanceID, Rect rect)
        {
            if (!Instance.IsEnabled)
                return;

            if (Instance is { ExtenderDrawers: null })
                return;

            string path = AssetDatabase.GetAssetPath(instanceID);
            if (string.IsNullOrEmpty(path))
                return;

            Instance.CheckRepaintAndDoGUI(
                path,
                rect,
                (assetItem) =>
                {
                    foreach (
                        var drawer in Instance.ExtenderDrawers.Where(drawer =>
                            drawer is { IsVisible: true }
                        )
                    )
                    {
                        drawer.OnProjectBrowserObjectAreaItemBackgroundGUI(assetItem);
                        drawer.OnProjectBrowserObjectAreaItemGUI(assetItem);
                    }
                }
            );
        }

        public static void OnProjectBrowserTreeViewItemGUI(
            int instanceID,
            Rect rect,
            TreeViewItem item
        )
        {
            if (!Instance.IsEnabled)
                return;

            string path = AssetDatabase.GetAssetPath(instanceID);
            if (string.IsNullOrEmpty(path))
                return;
            
            if (Instance is { ExtenderDrawers: null })
                return;

            Instance.CheckRepaintAndDoGUI(
                path,
                rect,
                (assetItem) =>
                {
                    foreach (
                        var drawer in Instance.ExtenderDrawers.Where(drawer =>
                            drawer is { IsVisible: true }
                        )
                    )
                    {
                        drawer.OnProjectBrowserTreeViewItemBackgroundGUI(assetItem, item);
                        drawer.OnProjectBrowserTreeViewItemGUI(assetItem, item);
                    }
                }
            );
        }

        private void OnProjectBrowserItemGUI(string guid, Rect rect)
        {
            // 转换GUID为路径
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return;
                
            CheckRepaintAndDoGUI(
                path,
                rect,
                (assetItem) =>
                {
                    foreach (var drawer in ExtenderDrawers.Where(drawer => drawer is { IsVisible: true }))
                    {
                        drawer.OnProjectBrowserGUI(assetItem);
                    }
                }
            );
        }

        private void CheckRepaintAndDoGUI(string path, Rect rect, Action<AssetItem> callback)
        {
            if (ExtenderDrawers == null)
                return;

            // 确保鼠标悬停窗口的正确性
            if (_mouseOverWindow != null && _mouseOverWindow.GetType() == ProjectBrowserReflect.Type && !_mouseOverWindow.wantsMouseMove)
            {
                _mouseOverWindow.wantsMouseMove = true;
            }

            var needRepaint = false;
            var assetItem = GetAssetItem(path, rect);

            // 检查是否需要重绘
            if (assetItem.IsHover && _lastHoveredPath != path)
            {
                _lastHoveredPath = path;
                needRepaint = true;
            }

            callback?.Invoke(assetItem);

            // 如果需要重绘，执行重绘
            if (needRepaint && _mouseOverWindow != null)
            {
                _mouseOverWindow.Repaint();
            }
        }

        private AssetItem GetAssetItem(string path, Rect rect)
        {
            _assetItems ??= new Dictionary<string, AssetItem>();
            
            // 如果已有缓存，刷新并返回
            if (_assetItems.TryGetValue(path, out var assetItem))
            {
                assetItem.Refresh(path, rect);
                return assetItem;
            }

            // 创建新的AssetItem
            assetItem = new AssetItem(path, rect, true);
            _assetItems.Add(path, assetItem);
            return assetItem;
        }

        // 按路径移除AssetItem
        public void RemoveAssetItem(string path)
        {
            if (_assetItems != null && _assetItems.ContainsKey(path))
            {
                _assetItems.Remove(path);
            }
        }

        public override void Repaint()
        {
            EditorApplication.RepaintProjectWindow();
        }
    }
}