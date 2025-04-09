using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Yueby.EditorWindowExtends.ProjectBrowserExtends.Drawer;
using System.Linq;
using System.Threading.Tasks;

namespace Yueby.EditorWindowExtends.ProjectBrowserExtends
{
    [System.Serializable]
    public class ProjectBrowserAsset
    {
        // 基本属性
        public string Name;
        public string Path;
        public string Guid { get { return AssetDatabase.AssetPathToGUID(Path); } } // 简化为只读计算属性
        public string ParentPath; // 修改ParentGuid为ParentPath
        
        // 状态标记
        public bool IsNewAsset;
        
        // 记录资产类型，避免频繁调用AssetDatabase
        public AssetType Type;
        
        public ProjectBrowserAsset(string path, string parentPath = null)
        {
            Path = path;
            ParentPath = parentPath;
            Name = System.IO.Path.GetFileName(path);
            IsNewAsset = false;
            
            // 确定资产类型
            Type = AssetDatabase.IsValidFolder(path) ? AssetType.Folder : AssetType.File;
        }

        public ProjectBrowserAsset() { }
    }
    
    // 资产类型枚举
    public enum AssetType
    {
        File,
        Folder
    }

    [InitializeOnLoad]
    public class AssetListener : AssetPostprocessor
    {
        // 扁平化存储所有资产，使用路径作为键
        public static Dictionary<string, ProjectBrowserAsset> AllAssets = new();
        
        // 存储父子关系的索引，用于快速查找子资产
        internal static Dictionary<string, HashSet<string>> _childrenIndex = new();
        
        // 存储新资产的集合，使用HashSet提高查找效率
        public static HashSet<string> NewAssetPaths = new();
        
        // 标记是否已初始化
        private static bool _hasInitialized = false;
        
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
        
        // 获取新资产列表的只读版本
        public static IReadOnlyCollection<ProjectBrowserAsset> GetNewAssets()
        {
            return NewAssetPaths
                .Where(path => AllAssets.ContainsKey(path))
                .Select(path => AllAssets[path])
                .ToList();
        }
        
        // 检查资产是否为新资产
        public static bool IsNewAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
                
            // 优先检查NewAssetPaths集合
            if (NewAssetPaths.Contains(path))
                return true;
                
            // 其次检查资产的IsNewAsset标志
            return AllAssets.TryGetValue(path, out var asset) && asset.IsNewAsset;
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
            ClearAllNewAssetMarks();
        }

        // 根据路径查找资产
        public static ProjectBrowserAsset FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // 如果已有缓存则直接返回
            if (AllAssets.TryGetValue(path, out var existingAsset))
                return existingAsset;

            // 创建新资产并添加到树中
            AddAssetToFlatStructure(path);
            return AllAssets.TryGetValue(path, out var newAsset) ? newAsset : null;
        }
        
        // 实现新的文件树初始化方法
        public static void InitializeFileTree()
        {
            // 清除所有现有数据
            AllAssets.Clear();
            _childrenIndex.Clear();
            NewAssetPaths.Clear();
            
            // 获取所有资产路径
            string[] allPaths = AssetDatabase.GetAllAssetPaths()
                .OrderBy(p => p.Count(c => c == '/')) // 按目录层级排序，确保父目录先处理
                .ToArray();
                
            // 构建资产树结构
            foreach (var path in allPaths)
            {
                AddAssetToFlatStructureInternal(path);
            }
        }

        // 获取子资产列表
        public static List<ProjectBrowserAsset> GetChildren(string path)
        {
            List<ProjectBrowserAsset> result = new List<ProjectBrowserAsset>();

            if (_childrenIndex.TryGetValue(path, out var childrenPaths))
            {
                foreach (var childPath in childrenPaths)
                {
                    // 尝试获取子资产，如果不存在则尝试加载
                    ProjectBrowserAsset child = null;
                    if (AllAssets.TryGetValue(childPath, out child))
                    {
                        result.Add(child);
                    }
                    else
                    {
                        child = FindByPath(childPath);
                        if (child != null)
                        {
                            result.Add(child);
                        }
                    }
                }
            }

            return result;
        }

        // 获取资产的子资产路径列表
        public static List<string> GetChildrenPaths(string path)
        {
            if (_childrenIndex.TryGetValue(path, out var children))
                return new List<string>(children);
            return new List<string>();
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
            
            // 确保当前文件夹已被加载
            if (!AllAssets.ContainsKey(path))
            {
                FindByPath(path);
                // 如果仍然不在缓存中，返回false
                if (!AllAssets.ContainsKey(path))
                    return false;
            }
                
            // 检查此文件夹是否有子资产
            if (!_childrenIndex.TryGetValue(path, out var childrenPaths) || childrenPaths.Count == 0)
                return false;
                
            // 遍历所有子资产，检查是否包含新资产
            foreach (var childPath in childrenPaths)
            {
                // 1. 检查子资产是否标记为新资产
                if (NewAssetPaths.Contains(childPath))
                    return true;
                    
                // 2. 获取子资产，如果不存在则尝试加载
                if (!AllAssets.TryGetValue(childPath, out var child))
                {
                    child = FindByPath(childPath);
                    if (child == null)
                        continue;
                }
                
                // 3. 检查文件是否标记为新资产
                if (child.IsNewAsset)
                    return true;
                    
                // 4. 如果是文件夹，递归检查
                if (child.Type == AssetType.Folder && DoesDirectoryContainNewAssets(childPath))
                    return true;
            }

            return false;
        }

        // 添加资产到扁平结构（不标记为新资产，仅用于初始化）
        private static void AddAssetToFlatStructureInternal(string path)
        {
            // 跳过无效路径
            if (string.IsNullOrEmpty(path))
                return;
                
            // 如果已存在，不要重复添加
            if (AllAssets.ContainsKey(path))
                return;
            
            // 获取父路径
            string parentPath = null;
            if (path != "Assets" && path != "Packages")
            {
                parentPath = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                
                // 如果父资产不存在于缓存中，需要先确保其被加载
                if (!string.IsNullOrEmpty(parentPath) && !AllAssets.ContainsKey(parentPath))
                {
                    // 递归处理父资产，确保整个路径链都被正确加载
                    AddAssetToFlatStructureInternal(parentPath);
                }
            }
            
            // 创建并添加资产
            var asset = new ProjectBrowserAsset(path, parentPath);
            
            // 更新缓存
            AllAssets[path] = asset;
            
            // 更新子节点索引
            if (!string.IsNullOrEmpty(parentPath))
            {
                if (!_childrenIndex.ContainsKey(parentPath))
                    _childrenIndex[parentPath] = new HashSet<string>();
                    
                _childrenIndex[parentPath].Add(path);
            }
            
            // 为当前节点创建子节点索引列表（如果不存在）
            if (!_childrenIndex.ContainsKey(path))
                _childrenIndex[path] = new HashSet<string>();
        }

        // 移除资产
        public static bool RemoveAsset(string path)
        {
            if (!AllAssets.TryGetValue(path, out var asset))
                return false;

            // 保存父路径，后续检查父节点是否需要移除
            string parentPath = asset.ParentPath;

            // 从父节点的子节点列表中移除
            if (!string.IsNullOrEmpty(parentPath) && _childrenIndex.TryGetValue(parentPath, out var siblings))
            {
                siblings.Remove(path);
            }

            // 递归移除所有子节点
            if (_childrenIndex.TryGetValue(path, out var children))
            {
                // 创建一个副本以避免集合修改错误
                var childrenCopy = children.ToList();
                foreach (var childPath in childrenCopy)
                {
                    RemoveAsset(childPath);
                }
            }

            // 移除当前节点
            _childrenIndex.Remove(path);
            AllAssets.Remove(path);
            NewAssetPaths.Remove(path);

            // 检查父节点是否需要移除（如果没有子节点且不是顶级节点）
            if (!string.IsNullOrEmpty(parentPath))
            {
                CheckAndRemoveEmptyParent(parentPath);
            }

            return true;
        }
        
        // 检查并移除空父节点（没有子节点的文件夹）
        private static void CheckAndRemoveEmptyParent(string parentPath)
        {
            // 跳过根目录检查
            if (parentPath == "Assets" || parentPath == "Packages")
                return;
                
            // 检查是否存在且是否有子节点
            if (!_childrenIndex.TryGetValue(parentPath, out var siblings) ||
                !AllAssets.TryGetValue(parentPath, out var parentAsset))
                return;

            // 如果父节点没有子节点，移除它
            if (siblings.Count == 0)
            {
                // 获取祖父节点
                string grandParentPath = parentAsset.ParentPath;
                
                // 从父节点的子节点列表中移除
                if (!string.IsNullOrEmpty(grandParentPath) && _childrenIndex.TryGetValue(grandParentPath, out var grandSiblings))
                {
                    grandSiblings.Remove(parentPath);
                }

                // 移除当前节点
                _childrenIndex.Remove(parentPath);
                AllAssets.Remove(parentPath);
                NewAssetPaths.Remove(parentPath);

                // 检查祖父节点是否需要移除
                if (!string.IsNullOrEmpty(grandParentPath))
                {
                    CheckAndRemoveEmptyParent(grandParentPath);
                }
            }
        }

        // 检查文件夹是否为空
        private static bool IsEmptyFolder(string path)
        {
            if (!_childrenIndex.TryGetValue(path, out var children) || children.Count == 0)
                return true;

            return false;
        }

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            // 如果资产树尚未初始化，则跳过处理
            if (!_hasInitialized)
            {
                return;
            }
            
            // 步骤1: 合并需要处理的删除资产（deletedAssets + movedFromAssetPaths）
            HashSet<string> allDeletedPaths = new HashSet<string>(deletedAssets);
            allDeletedPaths.UnionWith(movedFromAssetPaths);
            
            // 步骤2: 处理所有需要删除的资产
            foreach (var deletedPath in allDeletedPaths)
            {
                // 移除资产并清理相关缓存
                RemoveAsset(deletedPath);
                ProjectBrowserExtender.Instance.RemoveAssetItem(deletedPath);
            }
            
            // 步骤3: 合并需要处理的新增/修改资产（importedAssets + movedAssets）
            HashSet<string> allImportedPaths = new HashSet<string>(importedAssets);
            HashSet<string> trueNewAssets = new HashSet<string>();
            
            // 找出真正的新资产
            foreach (var asset in importedAssets)
            {
                // 如果资产不在缓存中，认为是新资产
                if (!AllAssets.ContainsKey(asset))
                {
                    trueNewAssets.Add(asset);
                }
            }
            
            // 添加所有移动目标资产（这些通常被视为新资产）
            foreach (var asset in movedAssets)
            {
                allImportedPaths.Add(asset);
                trueNewAssets.Add(asset); // 移动的资产总是被视为新资产
            }
            
            // 步骤4: 处理所有新增/修改的资产
            foreach (var assetPath in allImportedPaths)
            {
                // 如果资产已经在缓存中，跳过添加但可能需要更新
                if (AllAssets.ContainsKey(assetPath))
                {
                    // 可能需要更新缓存中的资产信息，如类型或修改时间
                    var asset = AllAssets[assetPath];
                    // 更新资产类型
                    asset.Type = AssetDatabase.IsValidFolder(assetPath) ? AssetType.Folder : AssetType.File;
                }
                else
                {
                    // 添加新资产到扁平结构
                    AddAssetToFlatStructure(assetPath);
                }
            }
            
            // 步骤5: 标记真正的新资产
            foreach (var assetPath in trueNewAssets)
            {
                // 使用新方法标记新资产
                AddNewAssetMark(assetPath);
            }
        }

        public static void ClearAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            // 使用非递归方式清理所有子资产
            ClearAssetSafe(path);
        }

        // 添加一个非递归实现，避免栈溢出
        private static void ClearAssetSafe(string startPath)
        {
            if (string.IsNullOrEmpty(startPath))
                return;

            // 使用HashSet跟踪已处理的路径，防止循环引用
            HashSet<string> processedPaths = new HashSet<string>();

            // 使用队列进行广度优先遍历
            Queue<string> pathQueue = new Queue<string>();
            pathQueue.Enqueue(startPath);

            while (pathQueue.Count > 0)
            {
                string currentPath = pathQueue.Dequeue();

                // 如果已处理过此路径，跳过
                if (processedPaths.Contains(currentPath))
                    continue;

                // 标记为已处理
                processedPaths.Add(currentPath);

                // 将资产标记为非新资产
                RemoveNewAssetMark(currentPath);

                // 获取子资产列表并加入队列
                if (_childrenIndex.TryGetValue(currentPath, out var children))
                {
                    foreach (var childPath in children)
                    {
                        if (!processedPaths.Contains(childPath))
                        {
                            pathQueue.Enqueue(childPath);
                        }
                    }
                }
            }
        }
        
        // 添加初始化和资产添加的方法（之前代码中省略的部分）
        
        // 异步初始化方法
        private static async Task DelayedInitializeAsync(int delayMs)
        {
            await Task.Delay(delayMs);
            InitializeFileTree();
            _hasInitialized = true;
        }
        
        // 添加资产到扁平结构
        public static void AddAssetToFlatStructure(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
                
            AddAssetToFlatStructureInternal(path);
        }

        // 添加新资产标记
        public static void AddNewAssetMark(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
                
            // 确保资产已被加载
            if (!AllAssets.ContainsKey(path))
            {
                var asset = FindByPath(path);
                if (asset == null)
                    return; // 如果无法加载资产，返回
            }
            
            // 获取资产
            var assetObj = AllAssets[path];
            
            // 检查是否为文件夹
            bool isFolder = assetObj.Type == AssetType.Folder;
            
            // 文件直接标记为新资产
            // 文件夹只有在为空时才标记为新资产
            bool shouldMark = !isFolder || (isFolder && IsEmptyFolder(path));
            
            if (shouldMark)
            {
                assetObj.IsNewAsset = true;
                NewAssetPaths.Add(path); // HashSet会自动去重
                
                // 刷新UI
                NewAssetDrawer.RefreshProjectWindow();
            }
        }

        // 移除新资产标记
        public static void RemoveNewAssetMark(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
                
            if (AllAssets.TryGetValue(path, out var asset))
            {
                asset.IsNewAsset = false;
            }
            
            NewAssetPaths.Remove(path);
            
            // 刷新UI
            NewAssetDrawer.RefreshProjectWindow();
        }

        // 清除所有新资产标记
        public static void ClearAllNewAssetMarks()
        {
            foreach (var path in NewAssetPaths.ToArray())
            {
                if (AllAssets.TryGetValue(path, out var asset))
                {
                    asset.IsNewAsset = false;
                }
            }
            
            NewAssetPaths.Clear();
            
            // 刷新UI
            NewAssetDrawer.RefreshProjectWindow();
        }
    }
}
