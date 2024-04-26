using B83.Win32;
using ClusterVR.CreatorKit.AccessoryExporter;
using ClusterVR.CreatorKit.Editor.Api.RPC;
using ClusterVR.CreatorKit.Editor.Validator.GltfItemExporter;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.ItemExporter;
using ClusterVR.CreatorKit.ItemExporter.ExporterHooks;
using Cysharp.Threading.Tasks;
using Image2Item.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using VGltf;
using Object = UnityEngine.Object;

// TODO: ヘッダに独自の値を入れる＆バージョニング
// TODO: 書き換え用のサンプルテクスチャの書き出し機能
// TODO: 外部glbを読み込んでテクスチャを差し替えられるようにする
// TODO: マウスで回転させられるように
// TODO: 商品画像用の書き出しができるように（背景透過で）

namespace Image2Item.Views
{
    public class UploadView : MonoBehaviour
    {
        [SerializeField] Button uploadButton;
        [SerializeField] GameObject instanceBase;
        [SerializeField] TemplateList templateList;
        [SerializeField] DialogView dialogView;
        //[SerializeField] GameObject loadingAnimation;
        [SerializeField] Text usernameText;
        [SerializeField] Button logoutButton;
        [SerializeField] AuthView authView;
        [SerializeField] Text dragDropNoticeText;
        [SerializeField] InputField itemNameInputField;
        [SerializeField] RawImage previewImage;
        [SerializeField] Button previewBackground;
        [SerializeField] GameObject selectButtonTemplate;
        [SerializeField] GameObject selectButtonRoot;
        [SerializeField] ToggleGroup renderModeToggleGroup;

        List<Button> selectButtons = new();
        List<Image> selectImages = new();
        readonly BoolReactiveProperty isUploading = new(false);
        //readonly UploadModel uploadModel = new();
        CancellationTokenSource cancellationTokenSource;
        ReactiveProperty<Texture2D> userTexture = new();
        ReactiveProperty<Texture2D> userTexturePhotoPanelL = new();
        ReactiveProperty<Texture2D> userTexturePhotoPanelR = new();
        TemplateItem currentTemplateItem;
        UploadModel.TextureType currentTextureType = UploadModel.TextureType.Opaque;

        public IObservable<Unit> ImageSelected() => previewBackground.OnClickAsObservable();
        public IObservable<Unit> UploadRequested() => uploadButton.OnClickAsObservable();
        public IObservable<Unit> LogoutRequested() => logoutButton.OnClickAsObservable();
        public IObservable<int> RenderModeChanged() => renderModeToggleGroup.ObserveEveryValueChanged(
            group => Array.IndexOf(group.GetComponentsInChildren<Toggle>(), group.GetFirstActiveToggle()));

        // TOOD: ここはもうちょい整理
        const int ThumbnailSize = 1024;

        Texture2D thumbnail;
        GltfContainer gltfContainer;
        IItemExporter itemExporter;
        IComponentValidator componentValidator;
        IGltfValidator gltfValidator;
        IItemTemplateBuilder builder;
        IItemUploadService uploadService;

        public GameObject Item { get; private set; }

        public bool IsValid { get; private set; }

        string Name { get; set; }
        Vector3Int Size { get; set; }

        readonly List<ValidationMessage> validationMessages = new List<ValidationMessage>();

        const float ItemPreviewMagnificationLimitDiagonalSize = 0.8f;

        void Start()
        {
            ImageSelected().Subscribe(_ =>
            {
                var od = new OpenDialog();
                od.Title = Localization.GetString("SelectTextureText");
                od.Filter = Localization.GetString("PngFileText") + "\0*.png\0\0";
                if (od.ShowDialog())
                {
                    SetUserTexture(od.FileName);
                }
            }).AddTo(this);
            UploadRequested().Subscribe(_ => UploadAsync().Forget()).AddTo(this);
            LogoutRequested().Subscribe(_ => authView.Logout()).AddTo(this);
            RenderModeChanged().Subscribe(i => SetRenderMode((UploadModel.TextureType)i)).AddTo(this);

            templateList.Sort();
            var buttonX = -206;
            var buttonY = 411;
            foreach (var template in templateList.TemplateItems)
            {
                var selectButton = Instantiate(selectButtonTemplate);
                selectButton.transform.SetParent(selectButtonRoot.transform);
                var rectTransform = selectButton.GetComponent<RectTransform>();
                rectTransform.localPosition = new Vector3(buttonX, buttonY, 0);
                buttonX += 116;

                var button = selectButton.GetComponent<Button>();
                selectButtons.Add(button);
                button.OnClickAsObservable().Subscribe(_ => SwitchPrefab(template)).AddTo(this);

                var image = selectButton.GetComponent<Image>();
                image.sprite = template.ButtonImage;

                var selectedImage = selectButton.transform.Find("Selected").GetComponent<Image>();
                selectImages.Add(selectedImage);
            }

            isUploading.Select(x => !x).SubscribeToInteractable(uploadButton).AddTo(this);
            isUploading.Select(x => !x).SubscribeToInteractable(logoutButton).AddTo(this);
            isUploading.Select(x => !x).SubscribeToInteractable(itemNameInputField).AddTo(this);

            foreach (var button in selectButtons)
            {
                isUploading.Select(x => !x).SubscribeToInteractable(button).AddTo(this);
            }

            userTexture.Select(x => x != null).Subscribe(x => previewImage.gameObject.SetActive(x)).AddTo(this);
            userTexture.Select(x => x == null).Subscribe(x => dragDropNoticeText.gameObject.SetActive(x)).AddTo(this);
        }

        public void SetUp()
        {
            // リスト先頭のアイテムを選択
            // TODO: TemplateList側でdefaultIndexを持つ
            SwitchPrefab();
        }

        async UniTaskVoid UploadAsync()
        {
            //if (uploadModel.IsProcessing.Value) return;

            if (isUploading.Value) return;

            isUploading.Value = true;
            try
            {
                if (userTexture.Value == null)
                {
                    dialogView.Show(Localization.GetString("TextureNotSpecifiedErrorText"));
                    return;
                }

                if (string.IsNullOrEmpty(itemNameInputField.text))
                {
                    dialogView.Show(Localization.GetString("ItemNameNotSpecifiedErrorText"));
                    return;
                }

                if (itemNameInputField.text.Length > 64)
                {
                    dialogView.Show(Localization.GetString("ItemNameTooLongErrorText"));
                    return;
                }

                // 先にcancellationTokenSourceをクリア
                UploadCancelRequest();
                cancellationTokenSource = new();

                // TODO: Modelに切り分ける
                //await uploadModel.UploadAsync("", cancellationTokenSource);

                // サムネイル生成とValidationのため
                // TODO: SetItem時点では処理はせずにここで行うようにする
                var go = instanceBase.transform.GetChild(0).gameObject;
                go.GetComponent<Item>().ItemName = itemNameInputField.text;
                SetItem(go);

                if (!IsValid)
                {
                    var error = "";
                    foreach (var message in validationMessages)
                    {
                        error += message.Message + "\n";
                        switch (message.Type)
                        {
                            case ValidationMessage.MessageType.Error:
                                Debug.LogError(message.Message);
                                break;
                            case ValidationMessage.MessageType.Info:
                                Debug.Log(message.Message);
                                break;
                            case ValidationMessage.MessageType.Warning:
                                Debug.LogWarning(message.Message);
                                break;
                        }
                    }
                    dialogView.Show(error);
                    return;
                }

                UploadCancelRequest();
                cancellationTokenSource = new CancellationTokenSource();

                var authInfo = PlayerPrefsUtils.SavedAccessToken;
                try
                {
                    await UploadItemAsync(authInfo.Token, cancellationTokenSource.Token);
                    // TODO: これはオプションにしたい
                    Application.OpenURL(uploadService.UploadedItemsManagementUrl);
                    dialogView.Show(Localization.GetString("UploadCompleteText"));
                }
                catch (OperationCanceledException)
                {
                    dialogView.Show(Localization.GetString("UploadInterruptedErrorText"));
                }
                catch (Exception e)
                {
                    dialogView.Show(Localization.GetString("UploadFailedErrorText") + $": {e.Message}");
                }
            }
            finally
            {
                isUploading.Value = false;
            }
        }

        void SwitchPrefab(TemplateItem templateItem = null)
        {
            if (templateItem == null)
            {
                templateItem = templateList.TemplateItems.First();
            }
            currentTemplateItem = templateItem;

            var index = Array.IndexOf(templateList.TemplateItems, currentTemplateItem);
            for (var i = 0; i < selectImages.Count; i++)
            {
                selectImages[i].enabled = i == index;
            }


            foreach (Transform child in instanceBase.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            var go = Instantiate(currentTemplateItem.Item.gameObject);
            go.transform.SetParent(instanceBase.transform);

            itemNameInputField.text = go.GetComponent<Item>().ItemName;

            SetItem(go);
        }

        // from mu.cluster.cluster-creator-kit@2.1.0\Editor\Window\GltfItemExporter\View\ItemView.cs
        public void SetItem(GameObject item)
        {
            // TODO: IItemが取れなければ弾く
            
            // MEMO: AccessoryItemを持ってたらアクセサリ、それ以外はクラフトアイテムと判定する

            // クラフトアイテムの場合
            if (item.GetComponent<AccessoryItem>() == null)
            {

                this.itemExporter = new CraftItemExporter();
                this.componentValidator = new CraftItemComponentValidator();
                this.gltfValidator = new CraftItemValidator();
                this.builder = new CraftItemTemplateBuilder();
                this.uploadService = new UploadCraftItemTemplateService();
            }
            // アクセサリーの場合
            else
            {
                this.itemExporter = new AccessoryExporter();
                this.componentValidator = new AccessoryComponentValidator();
                this.gltfValidator = new AccessoryValidator();
                this.builder = new AccessoryTemplateBuilder();
                this.uploadService = new UploadAccessoryTemplateService();
            }

            // TODO: 表示がクラフトアイテム（=Standard）の場合のみ、Opaque, Cutout, Fade, Transparentのラジオボタンを表示
            // TODO: 表示がアクセサリ（= MToon）の場合のみ、Opaque, Cutout, Transparent, TransparentWithZWriteのラジオボタンを表示

            try
            {
                Item = item;
                var itemComponent = item.GetComponent<IItem>();

                if (itemComponent != null)
                {
                    Name = itemComponent.ItemName;
                    Size = itemComponent.Size;
                }
                else
                {
                    Name = "";
                    Size = Vector3Int.zero;
                }

                if (userTexture.Value != null)
                {
                    SwitchMaterialSettings();
                }

                gltfContainer = ValidateAndBuildGltfContainer();
                AdjustCamera(EncapsulationBounds(item));
                CreateThumbnail(item);
            }
            catch (Exception e)
            {
                Clear();
                validationMessages.Add(new ValidationMessage(Localization.GetString("LoadingPrefabErrorText"),
                    ValidationMessage.MessageType.Error));
                Debug.LogException(e);
            }
        }

        static Bounds EncapsulationBounds(GameObject go)
        {
            return go.GetComponentsInChildren<Renderer>()
                .Select(r => r.bounds)
                .Aggregate(((result, current) =>
                {
                    result.Encapsulate(current);
                    return result;
                }));
        }

        void AdjustCamera(Bounds bounds)
        {
            var camera = GameObject.Find("PreviewCamera").GetComponent<Camera>();
            var rot = Quaternion.Euler(30f, 135f, 0f);
            var pos = bounds.center + Mathf.Max(10f, bounds.size.magnitude) * (rot * Vector3.back);
            camera.transform.SetPositionAndRotation(pos, rot);
            camera.orthographicSize = Mathf.Max(bounds.size.magnitude, ItemPreviewMagnificationLimitDiagonalSize) * 0.6f;
        }

        void CreateThumbnail(GameObject gameObject)
        {
            Object.DestroyImmediate(thumbnail);

            thumbnail = new Texture2D(ThumbnailSize, ThumbnailSize)
            {
                hideFlags = HideFlags.DontSave
            };

            var camera = GameObject.Find("PreviewCamera").GetComponent<Camera>();
            var renderTexture = camera.targetTexture;

            var currentActiveRenderTexture = RenderTexture.active;

            try
            {
                RenderTexture.active = renderTexture;
                camera.Render();
                thumbnail.ReadPixels(new Rect(0, 0, ThumbnailSize, ThumbnailSize), 0, 0);

                var colors = thumbnail.GetPixels();
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i].r = Mathf.LinearToGammaSpace(colors[i].r);
                    colors[i].g = Mathf.LinearToGammaSpace(colors[i].g);
                    colors[i].b = Mathf.LinearToGammaSpace(colors[i].b);
                }
                thumbnail.SetPixels(colors);

                thumbnail.Apply();
            }
            finally
            {
                RenderTexture.active = currentActiveRenderTexture;
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public async Task<byte[]> BuildZippedItemBinary()
        {
            var glbBinary = await gltfContainer.ExportAsync();
            var thumbnailBinary = thumbnail.EncodeToPNG();

            return builder.Build(glbBinary, thumbnailBinary);
        }

        void Clear()
        {
            Item = null;
            gltfContainer = null;
            validationMessages.Clear();
            IsValid = false;
            if (thumbnail != null)
            {
                Object.Destroy(thumbnail);
                thumbnail = null;
            }
            ClearTexture(userTexture);
            ClearTexture(userTexturePhotoPanelL);
            ClearTexture(userTexturePhotoPanelR);
        }

        void ClearTexture(ReactiveProperty<Texture2D> texture)
        {
            if (texture.Value != null)
            {
                Object.Destroy(texture.Value);
                texture.Value = null;
            }
        }

        GltfContainer ValidateAndBuildGltfContainer()
        {
            GltfContainer container = null;
            validationMessages.Clear();

            validationMessages.AddRange(GameObjectValidator.Validate(Item.gameObject));
            validationMessages.AddRange(componentValidator.Validate(Item));

            var buildGlbContainerValidationMessages = gltfValidator.Validate(Item).ToList();
            validationMessages.AddRange(buildGlbContainerValidationMessages);
            if (buildGlbContainerValidationMessages.All(message => message.Type != ValidationMessage.MessageType.Error))
            {
                try
                {
                    container = itemExporter.ExportAsGltfContainer(Item);
                    validationMessages.AddRange(gltfValidator.Validate(container));
                }
                catch (Exception e)
                {
                    if (TryGetReadableMessageOfGltfContainerException(e, out var message))
                    {
                        validationMessages.Add(new ValidationMessage(message, ValidationMessage.MessageType.Error));
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            validationMessages.Sort((a, b) => a.Type.CompareTo(b.Type));
            IsValid = validationMessages.All(message => message.Type != ValidationMessage.MessageType.Error);

            return container;
        }

        bool TryGetReadableMessageOfGltfContainerException(Exception exception, out string message)
        {
            switch (exception)
            {
                case MissingAudioClipException e:
                    message = Localization.GetString("AudioClipNotFoundErrorText") + $" (Id: {e.Id})";
                    return true;
                case ExtractAudioDataFailedException e:
                    message = Localization.GetString("FailedRetrieveAudioClipInfoErrorText") + $" (Id: {e.Id})";
                    return true;
                default:
                    message = default;
                    return false;
            }
        }

        void UploadCancelRequest()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        async Task UploadItemAsync(string verifiedToken, CancellationToken cancellationToken)
        {
            uploadService.SetAccessToken(verifiedToken);

            cancellationToken.ThrowIfCancellationRequested();

            var zipBinary = await BuildZippedItemBinary();
            //var itemTemplateId = await uploadService.UploadItemAsync(zipBinary, cancellationToken);
            //itemList.Add((itemView.Item, itemTemplateId));
            await uploadService.UploadItemAsync(zipBinary, cancellationToken);
        }

        void OnEnable()
        {
            UnityDragAndDropHook.InstallHook();
            UnityDragAndDropHook.OnDroppedFiles += OnDroppedFiles;
        }

        void OnDisable()
        {
            UnityDragAndDropHook.OnDroppedFiles -= OnDroppedFiles;
            UnityDragAndDropHook.UninstallHook();
        }

        const uint PngSignature = 0x474E5059;
        const ushort JpegSignature = 0xD8FF;
        const uint MaxTextureSize = 8192;

        void OnDroppedFiles(List<string> files, POINT pos)
        {
            if (isUploading.Value) return;
            if (files == null || files.Count == 0) return;

            if (files[0].EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || files[0].EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || files[0].EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                SetUserTexture(files[0]);
            }
        }

        void SetUserTexture(string filename)
        {
            // TODO: 全部読まずにサクッとヘッダを見て判別したい

            var bytes = File.ReadAllBytes(filename);
            ClearTexture(userTexture);
            ClearTexture(userTexturePhotoPanelL);
            ClearTexture(userTexturePhotoPanelR);

            userTexture.Value = new Texture2D(2, 2);

            // TODO: 拡張子は正しいけど中身が壊れてる系をcatchできるように

            userTexture.Value.LoadImage(bytes);

            // この時点でサイズが大きければ弾く
            if (userTexture.Value.width > MaxTextureSize || userTexture.Value.height > MaxTextureSize)
            {
                dialogView.Show(string.Format(Localization.GetString("TextureSizeTooLargeErrrorText"), MaxTextureSize));
                ClearTexture(userTexture);
                ClearTexture(userTexturePhotoPanelL);
                ClearTexture(userTexturePhotoPanelR);
                return;
            }

            CreatePhotoPanelTextures(userTexture.Value);

            SwitchMaterialSettings();
        }

        // TODO: PhotoPanel用の分割テクスチャを生成
        void CreatePhotoPanelTextures(Texture2D texture)
        {
            // 領域を計算
            var x = 0;
            var y = 0;
            var width = texture.width;
            var height = (int)(width / 16f * 9);
            // 縦に長い
            if (texture.height > height)
            {
                y = (texture.height - height) / 2;
            }
            // 横に長い
            else if (texture.height < height)
            {
                height = texture.height;
                width = (int)(height / 9f * 16);
                x = (texture.width - width) / 2;
            }

            var tempTexture = new Texture2D(width / 2, height);
            try
            {
                var textureSize = Math.Min(1024, height);

                userTexturePhotoPanelL.Value = new Texture2D(textureSize, textureSize);
                var pixels = texture.GetPixels(x, y, width / 2, height);
                tempTexture.SetPixels(pixels);
                tempTexture.Apply();
                Graphics.ConvertTexture(tempTexture, userTexturePhotoPanelL.Value);
                userTexturePhotoPanelL.Value.wrapMode = TextureWrapMode.Clamp;

                userTexturePhotoPanelR.Value = new Texture2D(textureSize, textureSize);
                pixels = texture.GetPixels(x + width / 2, y, width / 2, height);
                tempTexture.SetPixels(pixels);
                tempTexture.Apply();
                Graphics.ConvertTexture(tempTexture, userTexturePhotoPanelR.Value);
                userTexturePhotoPanelR.Value.wrapMode = TextureWrapMode.Clamp;
            }
            finally
            {
                Object.Destroy(tempTexture);
            }
        }

        void SetRenderMode(UploadModel.TextureType textureType)
        {
            // TODO: シェーダーにあわせてRenderModeを変更
            currentTextureType = textureType;
            SwitchMaterialSettings();
        }

        // Auto => アルファを使っているかどうかを見て、OpaqueとTransparentを自動で切り替える
        void SwitchMaterialSettings()
        {
            if (currentTemplateItem == null) return;

            var textureType = currentTextureType;

            // 左右分割テクスチャ
            if (currentTemplateItem.TextureType == TextureType.PhotoPanel)
            {
                var renderer = Item.GetComponentInChildren<MeshRenderer>();
                if (renderer.sharedMaterials.Length != 2) return;

                // TODO: Texture2DとTextureTypeを渡して設定するメソッドを生やす
                switch (textureType)
                {
                    case UploadModel.TextureType.Opaque:
                        SetStandardOpaque(renderer.sharedMaterials[0]);
                        break;
                    case UploadModel.TextureType.CutOut:
                        SetStandardCutout(renderer.sharedMaterials[0]);
                        break;
                    case UploadModel.TextureType.Transparent:
                        SetStandardTransparent(renderer.sharedMaterials[0]);
                        break;
                }
                renderer.sharedMaterials[0].SetTexture("_MainTex", userTexturePhotoPanelL.Value);

                switch (textureType)
                {
                    case UploadModel.TextureType.Opaque:
                        SetStandardOpaque(renderer.sharedMaterials[1]);
                        break;
                    case UploadModel.TextureType.CutOut:
                        SetStandardCutout(renderer.sharedMaterials[1]);
                        break;
                    case UploadModel.TextureType.Transparent:
                        SetStandardTransparent(renderer.sharedMaterials[1]);
                        break;
                }
                renderer.sharedMaterials[1].SetTexture("_MainTex", userTexturePhotoPanelR.Value);

                return;
            }

            var materialIndex = 0;
            //var isTransparent = HasAlpha(userTexture.Value);
            //var textureType = GetTextureType(userTexture.Value);
            var renderers = Item.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    var renderMode = materialIndex < currentTemplateItem.RenderModes.Length ? currentTemplateItem.RenderModes[materialIndex] : RenderMode.Auto;

                    if (material.shader.name == "Standard")
                    {
                        switch (renderMode)
                        {
                            case RenderMode.Auto:
                                switch (textureType)
                                {
                                    case UploadModel.TextureType.Opaque:
                                        SetStandardOpaque(material);
                                        break;
                                    case UploadModel.TextureType.CutOut:
                                        SetStandardCutout(material);
                                        break;
                                    case UploadModel.TextureType.Transparent:
                                        SetStandardTransparent(material);
                                        break;
                                }
                                break;
                            case RenderMode.Opaque:
                                SetStandardOpaque(material);
                                break;
                            case RenderMode.Transparent:
                                SetStandardTransparent(material);
                                break;
                        }

                        material.SetTexture("_MainTex", userTexture.Value);
                    }
                    else if (material.shader.name == "VRM/MToon")
                    {
                        switch (renderMode)
                        {
                            case RenderMode.Auto:
                                switch (textureType)
                                {
                                    case UploadModel.TextureType.Opaque:
                                        SetMToonOpaque(material);
                                        break;
                                    case UploadModel.TextureType.CutOut:
                                        SetMToonCutout(material);
                                        break;
                                    case UploadModel.TextureType.Transparent:
                                        SetMToonTransparent(material);
                                        break;
                                }
                                break;
                            case RenderMode.Opaque:
                                SetMToonOpaque(material);
                                break;
                            case RenderMode.Transparent:
                                SetMToonTransparent(material);
                                break;
                        }

                        material.SetTexture("_MainTex", userTexture.Value);
                        material.SetTexture("_ShadeTexture", userTexture.Value);
                    }

                    materialIndex++;
                }
            }
        }

        void SetStandardOpaque(Material material)
        {
            material.SetFloat("_Mode", 0f);

            material.SetOverrideTag("RenderType", "Opaque");

            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);

            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            material.renderQueue = -1;
        }

        void SetStandardCutout(Material material)
        {
            material.SetFloat("_Mode", 1f);


            material.SetOverrideTag("RenderType", "TransparentCutout");

            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);

            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            material.renderQueue = 2450;
        }

        void SetStandardTransparent(Material material)
        {
            material.SetFloat("_Mode", 3f);


            material.SetOverrideTag("RenderType", "Transparent");

            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);

            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");

            material.renderQueue = (int)RenderQueue.Transparent;
        }

        void SetMToonOpaque(Material material)
        {
            material.SetFloat("_BlendMode", (int)MToon.RenderMode.Opaque);
            
            material.SetOverrideTag("RenderType", "Opaque");
            
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.SetInt("_AlphaToMask", 0);
            
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            
            material.renderQueue = -1;
        }

        void SetMToonCutout(Material material)
        {
            material.SetFloat("_BlendMode", (int)MToon.RenderMode.Cutout);

            material.SetOverrideTag("RenderType", "TransparentCutout");

            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.SetInt("_AlphaToMask", 1);

            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            material.renderQueue = 2450;
        }

        void SetMToonTransparent(Material material)
        {
            material.SetFloat("_BlendMode", (int)MToon.RenderMode.Transparent);

            material.SetOverrideTag("RenderType", "Transparent");

            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_AlphaToMask", 0);

            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            material.renderQueue = (int)RenderQueue.Transparent;
        }

        UploadModel.TextureType GetTextureType(Texture2D texture)
        {
            var pixels = texture.GetPixels32();

            if (pixels.All(x => x.a == 0xff))
            {
                return UploadModel.TextureType.Opaque;
            }

            if (pixels.All(x => x.a == 0 || x.a == 0xff))
            {
                return UploadModel.TextureType.CutOut;
            }

            return UploadModel.TextureType.Transparent;
        }
    }
}