﻿using UnityEditorInternal;
using UnityEngine;
using Yueby.EditorWindowExtends.Core;

namespace Yueby.EditorWindowExtends.AnimatorWindowExtends.Core
{
    public class LayerControllerViewDrawer : EditorExtenderDrawer<LayerControllerViewExtender, LayerControllerViewDrawer>
    {
        public ReorderableList ReorderableList { get; private set; }


        public virtual void Init(LayerControllerViewExtender extender, ReorderableList reorderableList)
        {
            base.Init(extender);
            ReorderableList = reorderableList;
        }

        public virtual void OnDrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
        }

        public virtual void OnDrawElementBackground(Rect rect, int index, bool isactive, bool isfocused)
        {
        }

        public virtual void OnMouseUp(ReorderableList list)
        {
        }

        public virtual void OnMouseDrag(ReorderableList list)
        {
        }

        public virtual void OnRemove(ReorderableList list)
        {
        }

        public virtual void OnSelect(ReorderableList list)
        {
        }

        public virtual void OnChanged(ReorderableList list)
        {
        }

        public virtual void OnAdd(ReorderableList list)
        {
        }
    }
}