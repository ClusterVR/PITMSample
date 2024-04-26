using ClusterVR.CreatorKit.AccessoryExporter;
using ClusterVR.CreatorKit.Editor.Api.RPC;
using ClusterVR.CreatorKit.Editor.Validator.GltfItemExporter;
using ClusterVR.CreatorKit.ItemExporter;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using VGltf;

namespace Image2Item.Models
{
    public class UploadModel
    {
        public enum Result
        {
            Success,
            UnknownError,
        }

        public enum TextureType
        {
            Opaque = 0,
            CutOut = 1,
            Transparent = 2
        }

        readonly BoolReactiveProperty isProcessing = new(false);
        readonly StringReactiveProperty errorMessage = new();

        public IReadOnlyReactiveProperty<bool> IsProcessing => isProcessing;
        public IReadOnlyReactiveProperty<string> ErrorMessage => errorMessage;

        public UploadModel()
        {

        }

        public async UniTask<Result> UploadAsync(string verifiedToken, CancellationTokenSource cancellationTokenSource)
        {
            /*
            uploadService.SetAccessToken(verifiedToken);

            cancellationToken.ThrowIfCancellationRequested();

            var zipBinary = await BuildZippedItemBinary();
            //var itemTemplateId = await uploadService.UploadItemAsync(zipBinary, cancellationToken);
            //itemList.Add((itemView.Item, itemTemplateId));
            await uploadService.UploadItemAsync(zipBinary, cancellationToken);
            */
            await Task.Delay(1000);
            return Result.Success;
        }
    }

    public abstract class ItemUploadModel
    {
        protected IItemExporter itemExporter;
        protected IComponentValidator componentValidator;
        protected IGltfValidator gltfValidator;
        protected IItemTemplateBuilder builder;
        protected IItemUploadService uploadService;

        public async UniTask UploadAsync()
        {
            // TODO: エラーメッセージは例外で投げる
        }

        // from mu.cluster.cluster-creator-kit@2.1.0\Editor\Window\GltfItemExporter\View\ItemView.cs
        public void SetItem(GameObject item)
        {

        }
    }

    public class CraftItemUploadModel : ItemUploadModel
    {
        public CraftItemUploadModel()
        {
            this.itemExporter = new CraftItemExporter();
            this.componentValidator = new CraftItemComponentValidator();
            this.gltfValidator = new CraftItemValidator();
            this.builder = new CraftItemTemplateBuilder();
            this.uploadService = new UploadCraftItemTemplateService();
        }
    }

    public class AccessoryUploadModel : ItemUploadModel
    {
        public AccessoryUploadModel()
        {
            this.itemExporter = new AccessoryExporter();
            this.componentValidator = new AccessoryComponentValidator();
            this.gltfValidator = new AccessoryValidator();
            this.builder = new AccessoryTemplateBuilder();
            this.uploadService = new UploadAccessoryTemplateService();
        }
    }
}