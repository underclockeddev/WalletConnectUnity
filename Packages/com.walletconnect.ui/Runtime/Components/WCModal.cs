using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WalletConnectUnity.UI
{
    [AddComponentMenu("WalletConnect/UI/WC Modal")]
    public class WCModal : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Canvas _rootCanvas;

        [SerializeField] private CanvasScaler _rootCanvasScaler;
        [SerializeField] private Canvas _globalBackgroundCanvas;
        [SerializeField] private RectTransform _rootTransform;
        [SerializeField] private Image _modalMaskImage;
        [SerializeField] private Image _modalBorderImage;

        [field: SerializeField] public WCModalHeader Header { get; private set; }

        [Header("Settings")]
        [SerializeField] private TransformConfig _mobileTransformConfig;

        [SerializeField] private TransformConfig _desktopTransformConfig;

        [Header("Asset References")]
        [SerializeField] private Sprite _mobileModalMaskSprite;

        [SerializeField] private Sprite _mobileModalBorderSprite;

        public bool IsOpen => _rootCanvas.enabled;

        public event EventHandler Opened;
        public event EventHandler Closed;

        private readonly Stack<WCModalView> _viewsStack = new();
        private bool _hasGlobalBackground;

        private Coroutine _backInputCoroutine;

        private void Awake()
        {
            _hasGlobalBackground = _globalBackgroundCanvas != null;

            HandleConstantPhysicalSize();
        }

        public void OpenView(WCModalView view, WCModal modal = null, object parameters = null)
        {
            if (_viewsStack.Count == 0)
                EnableModal();

            if (_viewsStack.Count > 0)
                _viewsStack.Peek().Hide();

            modal ??= this;

            _viewsStack.Push(view);

            var resizeCoroutine = ResizeModalRoutine(view.GetRequiredHeight());
            view.Show(modal, resizeCoroutine, parameters);

            Header.Title = view.GetTitle();
        }

        public void CloseView()
        {
            if (_viewsStack.Count <= 0) return;

            var currentView = _viewsStack.Pop();
            currentView.Hide();

            if (_viewsStack.Count > 0)
            {
                var nextView = _viewsStack.Peek();
                Header.Title = nextView.GetTitle();
                var resizeCoroutine = ResizeModalRoutine(nextView.GetRequiredHeight());
                nextView.Show(this, resizeCoroutine);
            }
            else
            {
                DisableModal();
            }
        }

        public void CloseModal()
        {
            if (_viewsStack.Count > 0)
            {
                var lastView = _viewsStack.Pop();
                lastView.Hide();
            }

            _viewsStack.Clear();
            DisableModal();

            if (_backInputCoroutine != null)
                StopCoroutine(_backInputCoroutine);

            Closed?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerator ResizeModalRoutine(float targetHeight)
        {
            targetHeight = targetHeight + Header.Height;

#if UNITY_ANDROID || UNITY_IOS
            targetHeight += 20;
#endif

            var originalHeight = _rootTransform.sizeDelta.y;
            var elapsedTime = 0f;
            var duration = .25f; // TODO: serialize this

            while (elapsedTime < duration)
            {
                var lerp = Mathf.Lerp(originalHeight, targetHeight, elapsedTime / duration);
                _rootTransform.sizeDelta = new Vector2(_rootTransform.sizeDelta.x, lerp);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _rootTransform.sizeDelta = new Vector2(_rootTransform.sizeDelta.x, targetHeight);
        }

        private void HandleConstantPhysicalSize()
        {
            float targetDPI = 160;

#if (UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
            targetDPI = 96;
#endif

            if (Screen.dpi != 0)
                _rootCanvasScaler.scaleFactor = Screen.dpi / targetDPI;
            else
                _rootCanvasScaler.scaleFactor = 1f;
        }

        private void EnableModal()
        {
            ApplyTransformConfig(
#if UNITY_ANDROID || UNITY_IOS
                _mobileTransformConfig
#else
                _desktopTransformConfig
#endif
            );

#if UNITY_ANDROID || UNITY_IOS
            _modalMaskImage.sprite = _mobileModalMaskSprite;
            _modalBorderImage.sprite = _mobileModalBorderSprite;
#endif

            _rootCanvas.enabled = true;

            if (_hasGlobalBackground)
                _globalBackgroundCanvas.enabled = true;

            _backInputCoroutine = StartCoroutine(BackInputRoutine());

            Opened?.Invoke(this, EventArgs.Empty);
        }

        private void DisableModal()
        {
            _rootCanvas.enabled = false;

            if (_hasGlobalBackground)
                _globalBackgroundCanvas.enabled = false;
        }

        private void ApplyTransformConfig(TransformConfig config)
        {
            _rootTransform.anchorMin = config.anchorMin;
            _rootTransform.anchorMax = config.anchorMax;
            _rootTransform.sizeDelta = config.sizeDelta;
            _rootTransform.pivot = config.pivot;
        }

        private IEnumerator BackInputRoutine()
        {
            while (_rootCanvas.enabled)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (_viewsStack.Count > 0)
                    {
                        CloseView();
                    }
                    else
                    {
                        CloseModal();
                    }
                }

                yield return null;
            }
        }

        [Serializable]
        private struct TransformConfig
        {
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 sizeDelta;
            public Vector2 pivot;
        }
    }
}