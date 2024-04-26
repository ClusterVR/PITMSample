using ClusterVR.CreatorKit.Editor.Api.RPC;
using ClusterVR.CreatorKit.Editor.Api.User;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UniRx;

namespace Image2Item.Models
{
    public class AuthModel
    {
        public enum Result
        {
            Success,
            UnknownError,
            AlreadyProcessing,
            InvalidToken,
            AuthenticationFailed,
            NetworkError,
        }

        readonly BoolReactiveProperty isProcessing = new(false);
        readonly ReactiveProperty<UserInfo?> userInfo = new ReactiveProperty<UserInfo?>();
        readonly StringReactiveProperty errorMessage = new();

        public IReadOnlyReactiveProperty<bool> IsProcessing => isProcessing;
        public IReadOnlyReactiveProperty<UserInfo?> UserInfo => userInfo;
        public IReadOnlyReactiveProperty<string> ErrorMessage => errorMessage;

        public async UniTask<Result> LoginAsync(AuthenticationInfo authInfo, CancellationTokenSource cancellationTokenSource)
        {
            if (isProcessing.Value)
            {
                errorMessage.Value = Localization.GetString("LoginAlreadyInProgressErrorText");
                return Result.AlreadyProcessing;
            }

            if (!authInfo.IsValid)
            {
                errorMessage.Value = Localization.GetString("InvalidAccessTokenErrorText");
                return Result.InvalidToken;
            }

            Constants.OverrideHost(authInfo.Host);

            isProcessing.Value = true;
            try
            {
                var user = await APIServiceClient.GetMyUser(authInfo.Token, cancellationTokenSource.Token);

                if (string.IsNullOrEmpty(user.Username))
                {
                    errorMessage.Value = Localization.GetString("AuthenticationFailedErrorText");
                    return Result.AuthenticationFailed;
                }

                PlayerPrefsUtils.SavedAccessToken = authInfo;
                userInfo.Value = new UserInfo(user, authInfo.Token);
                errorMessage.Value = "";
                return Result.Success;
            }
            catch (Exception e)
            {
                if (e is HttpException)
                {
                    errorMessage.Value = Localization.GetString("NetworkErrorText") + $" ({e.Message})";
                    return Result.NetworkError;
                }
                else
                {
                    errorMessage.Value = Localization.GetString("UnknownErrorText") + $" ({e.Message})";
                    return Result.UnknownError;
                }
            }
            finally
            {
                isProcessing.Value = false;
            }
        }

        public void Logout()
        {
            userInfo.Value = null;
            errorMessage.Value = "";
            PlayerPrefsUtils.DeleteSavedAccessToken();
        }
    }
}