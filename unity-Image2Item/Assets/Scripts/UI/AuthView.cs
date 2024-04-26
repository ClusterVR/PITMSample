using ClusterVR.CreatorKit.Editor.Api.RPC;
using Cysharp.Threading.Tasks;
using Image2Item.Models;
using System;
using System.Threading;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Image2Item.Views
{
    public sealed class AuthView : MonoBehaviour
    {
        [SerializeField] InputField accessTokenInputField;
        [SerializeField] Button authButton;
        [SerializeField] ObservableEventTrigger accessTokenUrlTrigger;
        [SerializeField] Text errorText;
        [SerializeField] UploadView uploadView;
        [SerializeField] Text usernameText;
        [SerializeField] Text versionText;

        readonly AuthModel authModel = new();
        CancellationTokenSource cancellationTokenSource;

        public IObservable<Unit> AuthRequested() => authButton.OnClickAsObservable();
        public IObservable<PointerEventData> UrlPointerEnterAsObservable() => accessTokenUrlTrigger.OnPointerEnterAsObservable();
        public IObservable<PointerEventData> UrlPointerExitAsObservable() => accessTokenUrlTrigger.OnPointerExitAsObservable();
        public IObservable<PointerEventData> UrlPointerClickAsObservable() => accessTokenUrlTrigger.OnPointerClickAsObservable();

        void Start()
        {
            // TODO: 別のところでやる
            Screen.SetResolution(1600, 900, false, 60);
            versionText.text = "Ver." + Application.version;
            Localization.Initialize();

            AuthRequested().Subscribe(_ => LoginAsync().Forget()).AddTo(this);
            UrlPointerEnterAsObservable().Subscribe(_ => UrlPointerEnter()).AddTo(this);
            UrlPointerExitAsObservable().Subscribe(_ => UrlPointerExit()).AddTo(this);
            UrlPointerClickAsObservable().Subscribe(_ => UrlPointerClick()).AddTo(this);

            authModel.IsProcessing.Select(x => !x).SubscribeToInteractable(authButton);
            authModel.ErrorMessage.SubscribeToText(errorText);
            authModel.ErrorMessage.Select(x => !string.IsNullOrEmpty(x)).Subscribe(x => errorText.gameObject.SetActive(x));
            authModel.UserInfo.Where(x => x != null).Select(x => "@" + x.Value.User.Username).SubscribeToText(usernameText);

            // SavedAccessTokenが保存されている場合は自動でログインを試みる
            var authInfo = PlayerPrefsUtils.SavedAccessToken;
            if (!string.IsNullOrEmpty(authInfo.Token))
            {
                accessTokenInputField.text = authInfo.Token;
                LoginAsync().Forget();
                // TODO: 無効なAccessTokenだったり初回で無かったりした場合は入力画面に移行
            }
        }

        async UniTaskVoid LoginAsync()
        {
            if (authModel.IsProcessing.Value) return;

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new();

            var authInfo = new AuthenticationInfo(accessTokenInputField.text);
            var result = await authModel.LoginAsync(authInfo, cancellationTokenSource);
            if (result == AuthModel.Result.Success)
            {
                uploadView.gameObject.SetActive(true);
                uploadView.SetUp();
                this.gameObject.SetActive(false);
            }
        }

        public void Logout()
        {
            authModel.Logout();
            accessTokenInputField.text = "";
            gameObject.SetActive(true);
        }

        void UrlPointerEnter()
        {
            // TODO: マウスポインタをHandにする
        }

        void UrlPointerExit()
        {
            // TODO: マウスポインタを元に戻す
        }

        void UrlPointerClick()
        {
            Application.OpenURL("https://cluster.mu/account/tokens");
        }

        void OnDestroy()
        {
            cancellationTokenSource.Cancel();
        }
    }
}