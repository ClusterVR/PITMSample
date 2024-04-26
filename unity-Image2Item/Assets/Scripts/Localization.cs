using Image2Item.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Image2Item
{
    public static class Localization
    {
        static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            var code = PlayerPrefsUtils.LocaleIdentifierCode;
            if (code != "")
            {
                var locale = LocalizationSettings.AvailableLocales.Locales.Find(x => x.Identifier.Code == code);
                if (locale != null)
                {
                    LocalizationSettings.SelectedLocale = locale;
                }
            }

            isInitialized = true;
        }

        public static string CurrentLocale
        {
            get
            {
                return LocalizationSettings.SelectedLocale.Identifier.Code;
            }
        }

        public static void ChangeLocale(string code)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales.Find(x => x.Identifier.Code == code);
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
                PlayerPrefsUtils.LocaleIdentifierCode = code;
            }
        }

        public static string GetString(string key)
        {
            var table = LocalizationSettings.StringDatabase.GetTable("TextResources");
            return table.GetEntry(key).Value;
        }
    }
}