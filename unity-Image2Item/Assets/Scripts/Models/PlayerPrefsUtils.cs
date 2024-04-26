using ClusterVR.CreatorKit.Editor.Api.RPC;
using UnityEngine;

namespace Image2Item.Models
{
    public static class PlayerPrefsUtils
    {
        const string AccessTokenSaveKey = "image2item_access_token";
        const string LocaleIdentifierCodeKey = "Locale";

        static AuthenticationInfo authInfo;
        static string locale;

        public static AuthenticationInfo SavedAccessToken
        {
            get
            {
                if (authInfo == null)
                {
                    authInfo = new AuthenticationInfo(PlayerPrefs.GetString(AccessTokenSaveKey, ""));
                }
                return authInfo;
            }
            set
            {
                authInfo = value;
                PlayerPrefs.SetString(AccessTokenSaveKey, value?.RawValue);
            }
        }

        public static void DeleteSavedAccessToken()
        {
            PlayerPrefs.DeleteKey(AccessTokenSaveKey);
            authInfo = null;
        }

        public static string LocaleIdentifierCode
        {
            get
            {
                return PlayerPrefs.GetString(LocaleIdentifierCodeKey, "");
            }
            set
            {
                PlayerPrefs.SetString(LocaleIdentifierCodeKey, value);
            }
        }
    }
}