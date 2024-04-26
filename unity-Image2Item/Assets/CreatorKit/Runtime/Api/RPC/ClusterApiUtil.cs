using UnityEngine;
using UnityEngine.Networking;

namespace ClusterVR.CreatorKit.Editor.Api.RPC
{
    public static class ClusterApiUtil
    {
        const string ClusterCreatorKit = "ClusterCreatorKit";
        const string JsonMimeType = "application/json";

        const string ContentTypeHeaderKey = "Content-Type";
        const string AccessTokenHeaderKey = "X-Cluster-Access-Token";
        const string AppVersionHeaderKey = "X-Cluster-App-Version";
        const string DeviceNameHeaderKey = "X-Cluster-Device";
        const string PlatformHeaderKey = "X-Cluster-Platform";
        const string AnalyticsHeaderKey = "X-Cluster-Analytics";
        const string UserAgentHeaderKey = "User-Agent";

        const string DeviceNameHeaderValue = "OpenImage2Item";
        static readonly string AppVersionHeaderValue = Application.version;
        static readonly string UserAgentHeaderValue = DeviceNameHeaderValue + "/" + Application.version;

        public static UnityWebRequest CreateUnityWebRequest(string accessToken, string url, string method)
        {
            var www = new UnityWebRequest(url, method);

            www.SetRequestHeader(ContentTypeHeaderKey, JsonMimeType);
            www.SetRequestHeader(AccessTokenHeaderKey, accessToken);
            www.SetRequestHeader(AppVersionHeaderKey, AppVersionHeaderValue);
            www.SetRequestHeader(DeviceNameHeaderKey, DeviceNameHeaderValue);
            www.SetRequestHeader(PlatformHeaderKey, GetPlatform());
            www.SetRequestHeader(UserAgentHeaderKey, UserAgentHeaderValue);

            return www;
        }

        public static UnityWebRequest CreateUnityWebRequestAsAnalytics(string accessToken, string url, string method)
        {
            var www = new UnityWebRequest(url, method);

            www.SetRequestHeader(ContentTypeHeaderKey, JsonMimeType);
            www.SetRequestHeader(AccessTokenHeaderKey, accessToken);
            www.SetRequestHeader(AppVersionHeaderKey, AppVersionHeaderValue);
            www.SetRequestHeader(DeviceNameHeaderKey, DeviceNameHeaderValue);
            www.SetRequestHeader(PlatformHeaderKey, GetPlatform());
            www.SetRequestHeader(AnalyticsHeaderKey, ClusterCreatorKit);
            www.SetRequestHeader(UserAgentHeaderKey, UserAgentHeaderValue);

            return www;
        }

        public static UnityWebRequest CreateUnityWebRequest(string url, string method)
        {
            var www = new UnityWebRequest(url, method);

            www.SetRequestHeader(ContentTypeHeaderKey, JsonMimeType);
            www.SetRequestHeader(AppVersionHeaderKey, AppVersionHeaderValue);
            www.SetRequestHeader(DeviceNameHeaderKey, DeviceNameHeaderValue);
            www.SetRequestHeader(PlatformHeaderKey, GetPlatform());
            www.SetRequestHeader(UserAgentHeaderKey, UserAgentHeaderValue);

            return www;
        }

        static string GetPlatform()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return "Win";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_MAC
            return "Mac";
#elif UNITY_EDITOR_LINUX
            return "Linux";
#else
            return "Unknown";
#endif
        }
    }
}
