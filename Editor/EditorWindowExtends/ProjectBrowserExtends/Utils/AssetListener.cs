using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Yueby.EditorWindowExtends.ProjectBrowserExtends.Drawer;
using System.Linq;
using System.Threading.Tasks;

namespace Yueby.EditorWindowExtends.ProjectBrowserExtends
{
    // 资产类型枚举
    public enum AssetType
    {
        File,
        Folder
    }

    [InitializeOnLoad]
    public class AssetListener : AssetPostprocessor
    {
        // 存储新资产的集合，使用HashSet提高查找效率
        public static HashSet<string> NewAssetPaths = new();
        
        // 用于跟踪所有存在的资产，仅用于判断资产是否为新添加的
        private static HashSet<string> _existingAssets = new();
        
        // 标记是否已初始化
        private static bool _hasInitialized = false;

        // 文件夹结构缓存，仅用于检查文件夹是否为空
        private static Dictionary<string, HashSet<string>> _folderContents = new();
        
        static AssetListener()
        {
            // 如果编辑器已经加载完成，手动初始化一次
            EditorApplication.delayCall += () =>
            {
                if (!_hasInitialized)
                {
                    // 异步初始化，延迟100毫秒
                    _ = DelayedInitializeAsync(100);
                }
            };
        }
        
        // 检查资产是否为新资产
        public static bool IsNewAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
                
            return NewAssetPaths.Contains(path);
        }
        
        // 设置资产为新资产
        public static void SetAssetAsNew(string path, bool isNew)
        {
            if (string.IsNullOrEmpty(path))
                return;
                
            if (isNew)
            {
                AddNewAssetMark(path);
            }
            else
            {
                RemoveNewAssetMark(path);
            }
        }

        // 清空所有新资产标记
        public static void ClearAllNewAssets()
        {
            NewAssetPaths.Clear();
            NewAssetDrawer.RefreshProjectWindow();
        }

        // 添加新资产标记
        public static void AddNewAssetMark(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
                
            bool isFolder = AssetDatabase.IsValidFolder(path);
            
            // 文件直接标记为新资产
            // 文件夹只有在为空时才标记为新资产
            bool shouldMark = !isFolder || IsEmptyFolder(path);
            
            if (shouldMark)
            {
                NewAssetPaths.Add(path);
                NewAssetDrawer.RefreshProjectWindow();
            }
        }

        // 移除新资产标记
        public static void RemoveNewAssetMark(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            
            if (NewAssetPaths.Remove(path))
            {
                NewAssetDrawer.RefreshProjectWindow();
            }
        }

        // 清除所有新资产标记
        public static void ClearAllNewAssetMarks()
        {
            NewAssetPaths.Clear();
            NewAssetDrawer.RefreshProjectWindow();
        }

        // 实现新的文件树初始化方法 - 现在会构建存在资产索引
        public static void InitializeFileTree()
        {
            // 清空现有数据
            _existingAssets.Clear();
            _folderContents.Clear();
            NewAssetPaths.Clear();
            
            // 获取所有资产路径
            string[] allPaths = AssetDatabase.GetAllAssetPaths().ToArray();
                
            // 构建已存在资产索引
            foreach (var path in allPaths)
            {
                _existingAssets.Add(path);
                
                // 如果是文件，更新文件夹内容缓存
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string parentPath = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(parentPath))
                    {
                        if (!_folderContents.TryGetValue(parentPath, out var children))
                        {
                            children = new HashSet<string>();
                            _folderContents[parentPath] = children;
                        }
                        
                        children.Add(path);
                    }
                }
            }
            
            _hasInitialized = true;
        }

        // 刷新文件夹结构缓存
        private static void RefreshFolderStructure()
        {
            _folderContents.Clear();
            
            // 获取所有资产路径
            string[] allPaths = AssetDatabase.GetAllAssetPaths().ToArray();
            
            // 构建文件夹结构
            foreach (var path in allPaths)
            {
                if (AssetDatabase.IsValidFolder(path))
                    continue;
                
                string parentPath = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(parentPath))
                    continue;
                
                if (!_folderContents.TryGetValue(parentPath, out var children))
                {
                    children = new HashSet<string>();
                    _folderContents[parentPath] = children;
                }
                
                children.Add(path);
            }
        }

        // 检查文件夹是否包含新资产
        public static bool DoesDirectoryContainNewAssets(string path)
        {
            // 如果路径无效，直接返回false
            if (string.IsNullOrEmpty(path))
                return false;

            // 检查此资产是否已标记为新资产
            if (NewAssetPaths.Contains(path))
                return true;
                
            // 获取所有子资产
            string[] childPaths = AssetDatabase.GetSubFolders(path)
                .Concat(Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                    .Select(f => f.Replace('\\', '/')))
                .ToArray();
                
            // 检查子资产是否包含新资产
            foreach (var childPath in childPaths)
            {
                // 直接检查路径是否包含在新资产集合中
                if (NewAssetPaths.Contains(childPath))
                    return true;
                
                // 如果是文件夹，递归检查
                if (AssetDatabase.IsValidFolder(childPath) && DoesDirectoryContainNewAssets(childPath))
                    return true;
            }

            return false;
        }

        // 检查文件夹是否为空
        private static bool IsEmptyFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                return false;
            
            if (_folderContents.TryGetValue(path, out var children))
                return children.Count == 0;
            
            // 如果文件夹不在缓存中，则直接检查文件系统
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        // 清除资产及其子资产的新标记
        public static void ClearAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            // 移除当前资产的标记
            RemoveNewAssetMark(path);
            
            // 如果是文件夹，递归清除所有子资产
            if (AssetDatabase.IsValidFolder(path))
            {
                // 获取所有子文件夹
                string[] subFolders = AssetDatabase.GetSubFolders(path);
                foreach (var subFolder in subFolders)
                {
                    ClearAsset(subFolder);
                }
                
                // 获取所有子文件
                string[] files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                    .Select(f => f.Replace('\\', '/'))
                    .ToArray();
                    
                foreach (var file in files)
                {
                    RemoveNewAssetMark(file);
                }
            }
            
            NewAssetDrawer.RefreshProjectWindow();
        }
        
        // 处理资产导入
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            // 如果尚未初始化，则跳过处理
            if (!_hasInitialized)
                return;
            
            // 处理已删除的资产
            foreach (var path in deletedAssets.Concat(movedFromAssetPaths))
            {
                _existingAssets.Remove(path); // 从存在资产集合中移除
                NewAssetPaths.Remove(path);   // 从新资产集合中移除
                ProjectBrowserExtender.Instance.RemoveAssetItem(path);
            }
            
            // 处理新导入的资产
            foreach (var path in importedAssets)
            {
                if (string.IsNullOrEmpty(path))
                    continue;
                    
                // 如果资产已经存在于跟踪集合中，则不是新资产（如保存场景等操作）
                if (_existingAssets.Contains(path))
                    continue;
                    
                // 添加到存在资产集合
                _existingAssets.Add(path);
                
                // 标记为新资产
                AddNewAssetMark(path);
            }
            
            // 处理移动的资产 - 移动后的目标路径被视为新资产
            foreach (var path in movedAssets)
            {
                if (string.IsNullOrEmpty(path))
                    continue;
                
                // 添加到存在资产集合
                _existingAssets.Add(path);
                
                // 标记为新资产
                AddNewAssetMark(path);
            }
            
            // 更新文件夹结构缓存
            RefreshFolderStructure();
        }
        
        // 异步初始化方法
        private static async Task DelayedInitializeAsync(int delayMs)
        {
            await Task.Delay(delayMs);
            InitializeFileTree();
        }
    }
}
