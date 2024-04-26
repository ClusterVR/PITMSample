using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Image2Item.Views
{
    public class LocaleView : MonoBehaviour
    {
        // TODO: 対応言語にあわせて自動生成する
        [SerializeField] Button japaneseButton;
        [SerializeField] Button englishButton;

        public IObservable<Unit> ChangeToJapaneseRequested() => japaneseButton.OnClickAsObservable();
        public IObservable<Unit> ChangeToEnglishRequested() => englishButton.OnClickAsObservable();

        void Start()
        {
            Localization.Initialize();

            japaneseButton.enabled = Localization.CurrentLocale != "ja";
            japaneseButton.image.enabled = japaneseButton.enabled;
            englishButton.enabled = Localization.CurrentLocale != "en";
            englishButton.image.enabled = englishButton.enabled;

            ChangeToJapaneseRequested().Subscribe(_ => ChangeLocaleToJapanese()).AddTo(this);
            ChangeToEnglishRequested().Subscribe(_ => ChangeLocaleToEnglish()).AddTo(this);
        }

        private void ChangeLocaleToJapanese()
        {
            Localization.ChangeLocale("ja");
            japaneseButton.enabled = false;
            englishButton.enabled = true;
            japaneseButton.image.enabled = japaneseButton.enabled;
            englishButton.image.enabled = englishButton.enabled;
        }

        private void ChangeLocaleToEnglish()
        {
            Localization.ChangeLocale("en");
            japaneseButton.enabled = true;
            englishButton.enabled = false;
            japaneseButton.image.enabled = japaneseButton.enabled;
            englishButton.image.enabled = englishButton.enabled;
        }
    }
}