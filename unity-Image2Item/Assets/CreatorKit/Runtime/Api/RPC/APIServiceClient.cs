using System;
using System.Threading;
using System.Threading.Tasks;
using ClusterVR.CreatorKit.Editor.Api.AccessoryTemplate;
using ClusterVR.CreatorKit.Editor.Api.ItemTemplate;

namespace ClusterVR.CreatorKit.Editor.Api.RPC
{
    public sealed class Empty
    {
        public static readonly Empty Value = new Empty();
    }

    public static partial class APIServiceClient
    {
        public static Task<User.User> GetMyUser(string accessToken, CancellationToken cancellationToken)
        {
            return ApiClient.Get<Empty, User.User>(Empty.Value, accessToken,
                $"{Constants.ApiBaseUrl}/v1/account", cancellationToken);
        }

        public static Task<UploadItemTemplatePoliciesResponse> PostItemTemplatePolicies(
            UploadItemTemplatePoliciesPayload payload,
            string accessToken, Func<string, UploadItemTemplatePoliciesResponse> jsonDeserializer,
            CancellationToken cancellationToken)
        {
            return ApiClient.Post(payload, accessToken,
                $"{Constants.ApiBaseUrl}/v1/upload/item_template/policies", jsonDeserializer,
                cancellationToken);
        }

        public static Task<UploadAccessoryTemplatePoliciesResponse> PostAccessoryTemplatePolicies(
            UploadAccessoryTemplatePoliciesPayload payload,
            string accessToken, Func<string, UploadAccessoryTemplatePoliciesResponse> jsonDeserializer,
            CancellationToken cancellationToken)
        {
            return ApiClient.Post(payload, accessToken,
                $"{Constants.ApiBaseUrl}/v1/upload/accessory_template/policies", jsonDeserializer,
                cancellationToken);
        }
    }
}
