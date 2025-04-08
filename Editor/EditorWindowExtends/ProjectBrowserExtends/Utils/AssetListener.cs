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
        public string Name;
        public string Path;
        public string Guid;
        public string ParentGuid; // 存储父资产的GUID而不是引用
        public bool IsNewAsset;

        public ProjectBrowserAsset(string path, string guid, string parentGuid = null)
        {
            Path = path;
            Guid = guid;
            ParentGuid = parentGuid;
            Name = System.IO.Path.GetFileName(path);
            IsNewAsset = false;
        }

        public ProjectBrowserAsset() { }
    }

    [InitializeOnLoad]
    public class AssetListener : AssetPostprocessor
    {
        // 扁平化存储所有资产，使用GUID作为键
        public static Dictionary<string, ProjectBrowserAsset> AllAssets = new();
        
        // 存储父子关系的索引，用于快速查找子资产
        internal static Dictionary<string, List<string>> _childrenIndex = new();
        
        // 存储新资产的集合
        public static Dictionary<string, ProjectBrowserAsset> NewAssets = new();
        
        // 标记是否已初始化
        private static bool _hasInitialized = false;

        static AssetListener()
        {
            // 如果编辑器已经加载完成，手动初始化一次
            EditorApplication.delayCall += () => {
                if (!_hasInitialized)
                {
                    // 异步初始化，延迟100毫秒
                    _ = DelayedInitializeAsync(100);
                }
            };
        }

        // 异步延迟初始化方法
        private static async Task DelayedInitializeAsync(int delayMs)
        {
            try
            {
                // 等待指定的毫秒数
                await Task.Delay(delayMs);
                
                // 在主线程上执行初始化
                EditorApplication.delayCall += () => {
                    if (Application.isPlaying) return; // 避免在播放模式下初始化
                    
                    InitializeFileTree();
                };
            }
            catch (System.Exception)
            {
                // 异常处理，但不输出日志
            }
        }

        private static void InitializeFileTree()
        {
            // 防止重复初始化
            _hasInitialized = true;
            
            // 清空现有数据
            AllAssets.Clear();
            _childrenIndex.Clear();
            NewAssets.Clear();

            // 初始化 Assets 和 Packages 文件夹作为顶级节点
            var assetsGuid = AssetDatabase.AssetPathToGUID("Assets");
            var packagesGuid = AssetDatabase.AssetPathToGUID("Packages");
            
            var assetsFolder = new ProjectBrowserAsset("Assets", assetsGuid);
            var packagesFolder = new ProjectBrowserAsset("Packages", packagesGuid);
            
            // 添加到扁平结构
            AllAssets[assetsGuid] = assetsFolder;
            AllAssets[packagesGuid] = packagesFolder;
            
            // 初始化子节点索引
            _childrenIndex[assetsGuid] = new List<string>();
            _childrenIndex[packagesGuid] = new List<string>();
            
            // 不再预加载所有资产，采用懒加载方式
            
            // 初始化完成后清理缓存
            NewAssetDrawer.ClearCache();
        }

        // 查找资产，如果不存在则尝试加载
        public static ProjectBrowserAsset FindByGuid(string guid)
        {
            if (AllAssets.TryGetValue(guid, out var asset))
                return asset;
            
            // 如果资产不在缓存中，尝试加载它
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && (path.StartsWith("Assets/") || path.StartsWith("Packages/")))
            {
                // 创建临时字典存储路径到GUID的映射
                var pathToGuidCache = new Dictionary<string, string>();
                
                // 添加到扁平结构
                AddAssetToFlatStructure(path, pathToGuidCache);
                
                // 再次尝试获取
                if (AllAssets.TryGetValue(guid, out asset))
                    return asset;
            }

            return null;
        }

        // 查找资产
        public static ProjectBrowserAsset FindByPath(string path)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            return FindByGuid(guid);
        }

        // 添加资产到扁平结构
        private static void AddAssetToFlatStructure(string path, Dictionary<string, string> pathToGuidCache)
        {
            // 跳过无效路径
            if (string.IsNullOrEmpty(path) || (!path.StartsWith("Assets/") && !path.StartsWith("Packages/")))
                return;
                
            // 获取父路径
            string parentPath = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parentPath))
            {
                if (path.StartsWith("Assets"))
                    parentPath = "Assets";
                else if (path.StartsWith("Packages"))
                    parentPath = "Packages";
                else
                    return; // 无法确定父路径
            }
            
            // 获取或创建父GUID
            string parentGuid;
            if (!pathToGuidCache.TryGetValue(parentPath, out parentGuid))
            {
                parentGuid = AssetDatabase.AssetPathToGUID(parentPath);
                
                // 如果父资产不存在于缓存中，需要先确保其被加载
                if (!string.IsNullOrEmpty(parentGuid) && !AllAssets.ContainsKey(parentGuid))
                {
                    // 递归处理父资产，确保整个路径链都被正确加载
                    AddAssetToFlatStructure(parentPath, pathToGuidCache);
                }
                
                if (!string.IsNullOrEmpty(parentGuid))
                {
                    pathToGuidCache[parentPath] = parentGuid;
                }
            }
            
            // 获取当前资产GUID
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return;
                
            // 如果已存在，先移除旧记录
            if (AllAssets.ContainsKey(guid))
            {
                RemoveAsset(guid);
            }
                
            // 创建并添加资产
            var asset = new ProjectBrowserAsset(path, guid, parentGuid);
            AllAssets[guid] = asset;
            pathToGuidCache[path] = guid;
            
            // 更新子节点索引
            if (!string.IsNullOrEmpty(parentGuid))
            {
                if (!_childrenIndex.ContainsKey(parentGuid))
                    _childrenIndex[parentGuid] = new List<string>();
                    
                if (!_childrenIndex[parentGuid].Contains(guid))
                    _childrenIndex[parentGuid].Add(guid);
            }
            
            // 为当前节点创建子节点索引列表（如果不存在）
            if (!_childrenIndex.ContainsKey(guid))
                _childrenIndex[guid] = new List<string>();
        }
        
        // 获取子资产列表
        public static List<ProjectBrowserAsset> GetChildren(string guid)
        {
            List<ProjectBrowserAsset> result = new List<ProjectBrowserAsset>();
            
            if (_childrenIndex.TryGetValue(guid, out var childrenGuids))
            {
                foreach (var childGuid in childrenGuids)
                {
                    // 尝试获取子资产，如果不存在则尝试加载
                    ProjectBrowserAsset child = null;
                    if (AllAssets.TryGetValue(childGuid, out child))
                    {
                        result.Add(child);
                    }
                    else
                    {
                        child = FindByGuid(childGuid);
                        if (child != null)
                        {
                            result.Add(child);
                        }
                    }
                }
            }
            
            return result;
        }
        
        // 获取资产的子资产GUID列表
        public static List<string> GetChildrenGuids(string guid)
        {
            if (_childrenIndex.TryGetValue(guid, out var children))
                return new List<string>(children);
            return new List<string>();
        }
        
        // 检查文件夹是否包含新资产
        public static bool DoesDirectoryContainNewAssets(string guid)
        {
            // 检查此资产是否已标记为新资产
            if (NewAssets.ContainsKey(guid))
                return true;
            
            // 确保当前文件夹已被加载
            if (!AllAssets.ContainsKey(guid))
            {
                FindByGuid(guid);
            }
                
            if (!_childrenIndex.TryGetValue(guid, out var childrenGuids) || childrenGuids.Count == 0)
                return false;
                
            // 遍历所有子资产
            foreach (var childGuid in childrenGuids)
            {
                // 1. 检查子资产是否标记为新资产
                if (NewAssets.ContainsKey(childGuid))
                    return true;
                    
                // 尝试获取子资产，如果不存在则尝试加载
                ProjectBrowserAsset child = null;
                if (!AllAssets.TryGetValue(childGuid, out child))
                {
                    child = FindByGuid(childGuid);
                }
                
                if (child != null)
                {
                    // 2. 检查文件是否标记为新资产
                    if (child.IsNewAsset)
                        return true;
                        
                    // 3. 如果是文件夹，递归检查
                    if (AssetDatabase.IsValidFolder(child.Path) && DoesDirectoryContainNewAssets(childGuid))
                    return true;
                }
            }

            return false;
        }

        // 移除资产
        public static bool RemoveAsset(string guid)
        {
            if (!AllAssets.TryGetValue(guid, out var asset))
                return false;
                
            // 保存父GUID，后续检查父节点是否需要移除
            string parentGuid = asset.ParentGuid;
                
            // 从父节点的子节点列表中移除
            if (!string.IsNullOrEmpty(parentGuid) && _childrenIndex.TryGetValue(parentGuid, out var siblings))
            {
                siblings.Remove(guid);
            }
            
            // 递归移除所有子节点
            if (_childrenIndex.TryGetValue(guid, out var children))
            {
                // 创建一个副本以避免集合修改错误
                var childrenCopy = children.ToList();
                foreach (var childGuid in childrenCopy)
                {
                    RemoveAsset(childGuid);
                }
            }
            
            // 移除当前节点
            _childrenIndex.Remove(guid);
            AllAssets.Remove(guid);
            NewAssets.Remove(guid);
            
            // 检查父节点是否需要移除（如果没有子节点且不是顶级节点）
            if (!string.IsNullOrEmpty(parentGuid))
            {
                CheckAndRemoveEmptyParent(parentGuid);
            }
            
            return true;
        }
        
        // 检查并移除空父节点（没有子节点的文件夹）
        private static void CheckAndRemoveEmptyParent(string parentGuid)
        {
            // 检查是否为Assets或Packages（这些是顶级节点，不能移除）
            if (!_childrenIndex.TryGetValue(parentGuid, out var siblings) || 
                !AllAssets.TryGetValue(parentGuid, out var parentAsset))
                return;
            
            // 检查是否为Assets或Packages顶级目录
            string path = parentAsset.Path;
            if (path == "Assets" || path == "Packages")
                return;
            
            // 如果父节点没有子节点，移除它
            if (siblings.Count == 0)
            {
                // 调用RemoveAsset而不是直接删除，这样可以递归处理更高层次的父节点
                // 但是跳过子节点检查（因为我们已经知道它没有子节点）
                if (AllAssets.TryGetValue(parentGuid, out var asset))
                {
                    // 从父节点的子节点列表中移除
                    string grandParentGuid = asset.ParentGuid;
                    if (!string.IsNullOrEmpty(grandParentGuid) && _childrenIndex.TryGetValue(grandParentGuid, out var grandSiblings))
                    {
                        grandSiblings.Remove(parentGuid);
                    }
                    
                    // 移除当前节点
                    _childrenIndex.Remove(parentGuid);
                    AllAssets.Remove(parentGuid);
                    NewAssets.Remove(parentGuid);
                    
                    // 检查祖父节点是否需要移除
                    if (!string.IsNullOrEmpty(grandParentGuid))
                    {
                        CheckAndRemoveEmptyParent(grandParentGuid);
                    }
                }
            }
        }

        // 设置资产为新资产
        public static void SetAssetAsNew(string guid, bool isNew)
        {
            if (!AllAssets.TryGetValue(guid, out var asset))
                return;
                
            // 检查是否为文件夹
            bool isFolder = AssetDatabase.IsValidFolder(asset.Path);
            
            if (isNew)
            {
                // 文件直接标记为新资产
                // 文件夹只有在为空时才标记为新资产
                bool shouldMark = !isFolder || (isFolder && IsEmptyFolder(guid));
                
                if (shouldMark)
                {
                    asset.IsNewAsset = true;
                    if (!NewAssets.ContainsKey(guid))
                    {
                        NewAssets[guid] = asset;
                    }
                }
            }
            else
            {
                asset.IsNewAsset = false;
                NewAssets.Remove(guid);
            }
        }
        
        // 检查文件夹是否为空
        private static bool IsEmptyFolder(string guid)
        {
            if (!_childrenIndex.TryGetValue(guid, out var children) || children.Count == 0)
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
            
            var pathToGuidCache = new Dictionary<string, string>();
            
            // 合并处理需要删除的资产（已删除的 + 移动源资产）
            List<string> allDeletedAssets = new List<string>(deletedAssets);
            allDeletedAssets.AddRange(movedFromAssetPaths);
            
            // 先处理所有需要删除的资产（包括移动源）
            foreach (var deletedAsset in allDeletedAssets)
            {
                if (deletedAsset.StartsWith("Assets/") || deletedAsset.StartsWith("Packages/"))
                {
                    string guid = AssetDatabase.AssetPathToGUID(deletedAsset);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        RemoveAsset(guid);
                        ProjectBrowserExtender.Instance.RemoveAssetItem(guid);
                    }
                }
            }
            
            // 合并处理需要导入的资产（新导入的 + 移动目标）
            List<string> allImportedAssets = new List<string>(importedAssets);
            allImportedAssets.AddRange(movedAssets);
            
            // 过滤掉.meta文件和不以Assets/或Packages/开头的路径
            List<string> validAssets = allImportedAssets
                .Where(a => (a.StartsWith("Assets/") || a.StartsWith("Packages/")) && !a.EndsWith(".meta"))
                .ToList();
            
            // 首先确保所有资产都被添加到扁平结构中
            foreach (var asset in validAssets)
            {
                AddAssetToFlatStructure(asset, pathToGuidCache);
            }
            
            // 然后再次遍历并标记为新资产
            foreach (var asset in validAssets)
            {
                // 获取资产GUID
                string guid = AssetDatabase.AssetPathToGUID(asset);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }
                
                // 检查是否为文件夹
                bool isFolder = AssetDatabase.IsValidFolder(asset);
                
                // 再次确保资产已添加到结构中
                if (!AllAssets.ContainsKey(guid))
                {
                    AddAssetToFlatStructure(asset, pathToGuidCache);
                    
                    // 如果仍然不在，则跳过
                    if (!AllAssets.ContainsKey(guid))
                    {
                        continue;
                    }
                }
                
                // 标记为新资产
                ProjectBrowserAsset assetObj = AllAssets[guid];
                
                // 文件直接标记为新资产
                // 文件夹只有在为空时才标记为新资产
                bool shouldMark = !isFolder || (isFolder && IsEmptyFolder(guid));
                
                if (shouldMark)
                {
                    assetObj.IsNewAsset = true;
                    if (!NewAssets.ContainsKey(guid))
                    {
                        NewAssets[guid] = assetObj;
                    }
                }
            }
            
            // 处理完所有资产更新后，一次性清理缓存并刷新窗口
            NewAssetDrawer.ClearCache();
        }

        public static void ClearAsset(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;
                
            // 使用非递归方式清理所有子资产
            ClearAssetSafe(guid);
        }
        
        // 添加一个非递归实现，避免栈溢出
        private static void ClearAssetSafe(string startGuid)
        {
            if (string.IsNullOrEmpty(startGuid))
                return;
                
            // 使用HashSet跟踪已处理的GUID，防止循环引用
            HashSet<string> processedGuids = new HashSet<string>();
            
            // 使用队列进行广度优先遍历
            Queue<string> guidQueue = new Queue<string>();
            guidQueue.Enqueue(startGuid);
            
            while (guidQueue.Count > 0)
            {
                string currentGuid = guidQueue.Dequeue();
                
                // 如果已处理过此GUID，跳过
                if (processedGuids.Contains(currentGuid))
                    continue;
                    
                // 标记为已处理
                processedGuids.Add(currentGuid);
                
                // 将资产标记为非新资产
                SetAssetAsNew(currentGuid, false);
                
                // 获取子资产列表并加入队列
                if (_childrenIndex.TryGetValue(currentGuid, out var children))
                {
                    foreach (var childGuid in children)
                    {
                        if (!processedGuids.Contains(childGuid))
                        {
                            guidQueue.Enqueue(childGuid);
                        }
                    }
                }
            }
        }
        
        // 添加一个方法以兼容旧代码
        public static void ClearAsset(ProjectBrowserAsset asset)
        {
            if (asset == null)
                return;

            ClearAsset(asset.Guid);
        }
    }
}
