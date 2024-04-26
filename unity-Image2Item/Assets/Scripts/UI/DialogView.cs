using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Image2Item.Views
{
    public sealed class DialogView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text messageText;
        [SerializeField] Button okButton;

        public IObservable<Unit> OkButtonClickedAsObservable() => okButton.OnClickAsObservable();

        void Start()
        {
            OkButtonClickedAsObservable().Subscribe(_ => OkButtonClicked()).AddTo(this);
        }

        void OkButtonClicked()
        {
            panel.SetActive(false);
        }

        public void Show(string text)
        {
            messageText.text = text;
            panel.SetActive(true);
        }
    }
}