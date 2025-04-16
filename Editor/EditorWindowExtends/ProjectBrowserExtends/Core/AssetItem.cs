using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yueby.EditorWindowExtends.ProjectBrowserExtends.Core
{
    public class AssetItem
    {
        public Rect Rect;
        public Rect OriginRect;

        public string Guid { get { return AssetDatabase.AssetPathToGUID(Path); } }
        public string Path { get; private set; }

        public bool IsFolder { get; private set; }
        public bool IsHover { get; private set; }
        public Object Asset { get; private set; }

        public AssetItem(string path, Rect rect, bool isPath)
        {
            OriginRect = rect;
            Path = path;
            Rect = rect;
            IsHover = rect.Contains(Event.current.mousePosition);
            Asset = AssetDatabase.LoadAssetAtPath(Path, typeof(Object));
            IsFolder = !string.IsNullOrEmpty(Path) && AssetDatabase.IsValidFolder(Path);
        }

        public AssetItem(string guid, Rect rect)
        {
            OriginRect = rect;
            Path = AssetDatabase.GUIDToAssetPath(guid);
            Rect = rect;
            IsHover = rect.Contains(Event.current.mousePosition);
            Asset = AssetDatabase.LoadAssetAtPath(Path, typeof(Object));
            IsFolder = !string.IsNullOrEmpty(Path) && AssetDatabase.IsValidFolder(Path);
        }

        public void Refresh(string path, Rect rect)
        {
            OriginRect = rect;
            Rect = rect;
            Path = path;
            IsHover = rect.Contains(Event.current.mousePosition);
        }
    }
}