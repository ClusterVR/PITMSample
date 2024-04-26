using System.Threading.Tasks;
using ClusterVR.CreatorKit.ItemExporter;
using UnityEngine;

namespace ClusterVR.CreatorKit.AccessoryExporter
{
    // accessory template の glb と icon 画像を zip アーカイブしたファイルを作る
    public sealed class AccessoryTemplateBuilder : IItemTemplateBuilder
    {
        const string GlbEntryName = "accessory_template.glb";
        const string IconEntryName = "icon.png";

        public byte[] Build(byte[] glbBinary, byte[] thumbnailBinary)
        {
            return ItemTemplateBuilder.Build(GlbEntryName, glbBinary, IconEntryName, thumbnailBinary);
        }
    }
}
