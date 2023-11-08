using System;
using System.Collections;
using UnityEngine;

namespace WalletConnectUnity.UI
{
    public class WCModalView : MonoBehaviour
    {
        [Header("Scene References")] [SerializeField]
        private Canvas _canvas;

        [SerializeField] private RectTransform _rootTransform;

        protected WCModal parentModal;

        public virtual float GetRequiredHeight()
        {
            return _rootTransform.rect.height;
        }

        public virtual void Show(WCModal modal, IEnumerator effectCoroutine, object options = null)
        {
            parentModal = modal;
            StartCoroutine(ShowAfterEffectRoutine(effectCoroutine));
        }

        public virtual void Hide()
        {
            _canvas.enabled = false;
        }

        protected virtual IEnumerator ShowAfterEffectRoutine(IEnumerator effectCoroutine)
        {
            yield return StartCoroutine(effectCoroutine);
            _canvas.enabled = true;
        }

#if UNITY_EDITOR
        public void Reset()
        {
            _canvas = GetComponent<Canvas>();
            _rootTransform = GetComponent<RectTransform>();
        }
#endif
    }

    public class ViewParams
    {
    }
}