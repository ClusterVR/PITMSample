using ClusterVR.CreatorKit.Item.Implements;
using System;
using UnityEngine;

namespace Image2Item
{
    public enum RenderMode
    {
        Auto,
        Opaque,
        Transparent
    }

    public enum TextureType
    {
        Normal,
        PhotoPanel
    }

    [System.Serializable]
    public class TemplateItem
    {
        [SerializeField] Item item;
        [SerializeField] RenderMode[] renderModes;
        [SerializeField] TextureType textureType;
        [SerializeField] Sprite buttonImage;
        [SerializeField] int priority;

        public Item Item => item;
        public RenderMode[] RenderModes => renderModes;
        public TextureType TextureType => textureType;
        public Sprite ButtonImage => buttonImage;
        public int Priority => priority;
    }

    public class TemplateList : MonoBehaviour
    {
        [SerializeField, NonReorderable] TemplateItem[] templateItems;

        public TemplateItem[] TemplateItems => templateItems;

        public void Sort()
        {
            Array.Sort(templateItems, (x, y) => x.Priority.CompareTo(y.Priority));
        }
    }
}