using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using WalletConnectUnity.Core.Networking;
using WalletConnectUnity.Core.Utils;
using WalletConnectUnity.UI;

namespace WalletConnectUnity.Modal.Views
{
    public class ConnectView : WCModalView
    {
        [SerializeField] private RectTransform _listRootTransform;
        [SerializeField] private ApprovalView _approvalView;
        [SerializeField] private WalletSearchView _walletSearchView;

        [Header("QR Code")] [SerializeField] private bool _showQrCodeOnDesktop = true;
        [SerializeField] private RectTransform _qrCodeArea;
        [SerializeField] private GameObject _qrWalletIcon;
        [SerializeField] private RawImage _qrCodeRawImage;
        [SerializeField] private Color _qrCodeWaitColor = Color.gray;

        [Header("Settings")]
        [SerializeField] private ushort _walletsCounts = 5; // don't change in runtime

        [SerializeField] private bool _showAllWalletsButton = true;

        [Header("Asset References"), SerializeField]
        private Sprite _wcLogoSprite;

        [SerializeField] private Sprite _allWalletsSprite;
        [SerializeField] private Sprite _copyIconSprite;
        [SerializeField] private WCListSelect _listSelectPrefab;

        private readonly List<WCListSelect> _listItems = new(5);

        protected string Uri { get; private set; }

        private void Awake()
        {
            if (_listItems.Count == 0)
                for (var i = 0; i < _walletsCounts; i++)
                    _listItems.Add(Instantiate(_listSelectPrefab, _listRootTransform));

            if (_showAllWalletsButton)
                _listItems.Add(Instantiate(_listSelectPrefab, _listRootTransform));

            var sizeDelta = rootTransform.sizeDelta;
            sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + _listItems.Count * 64);

            rootTransform.sizeDelta = sizeDelta;

#if (!UNITY_IOS && !UNITY_ANDROID)
            // Resize to fit the QR code
            if (_showQrCodeOnDesktop)
            {
                _qrCodeArea.gameObject.SetActive(true);

                var newY = sizeDelta.y + _qrCodeArea.rect.height;
                sizeDelta = new Vector2(sizeDelta.x, newY);
            }
#endif

            rootTransform.sizeDelta = sizeDelta;
        }

        public override async void Show(WCModal modal, IEnumerator effectCoroutine, object options = null)
        {
            StartCoroutine(RefreshWalletsCoroutine());

            modal.Header.SetCustomLeftButton(_copyIconSprite, OnCopyLinkClick);

            base.Show(modal, effectCoroutine, options);

#if (!UNITY_IOS && !UNITY_ANDROID)
            await ShowQrCodeAndCopyButtonAsync();
#endif
        }

        public override void Hide()
        {
            base.Hide();
            StopAllCoroutines();

            parentModal.Header.RemoveCustomLeftButton();

            _qrCodeRawImage.texture = null;
            _qrCodeRawImage.color = _qrCodeWaitColor;
            _qrWalletIcon.SetActive(false);
        }

        private void OnCopyLinkClick()
        {
            parentModal.Header.Snackbar.Show(WCSnackbar.Type.Success, "Link copied");
            GUIUtility.systemCopyBuffer = Uri;
        }

        private IEnumerator RefreshWalletsCoroutine()
        {
            var index = 0;

            // TODO: show one recent wallet

            // ReSharper disable once UselessBinaryOperation
            var walletsToLoad = _walletsCounts - index;

            if (walletsToLoad != 0)
            {
                using var uwr = WalletConnectModal.WalletsRequestsFactory.GetWallets(1, walletsToLoad);

                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[WalletConnectUnity] Failed to get wallets: {uwr.error}", this);
                    yield break;
                }

                var response = JsonConvert.DeserializeObject<GetWalletsResponse>(uwr.downloadHandler.text);

                for (var i = index; i < _walletsCounts; i++)
                {
                    var wallet = response.Data[i - index];

                    var remoteSprite =
                        RemoteSprite.Create($"https://api.web3modal.com/getWalletImage/{wallet.ImageId}");
                    _listItems[i].Initialize(new WCListSelect.Params
                    {
                        title = wallet.Name,
                        remoteSprite = remoteSprite,
                        onClick = () =>
                        {
                            parentModal.OpenView(_approvalView, parameters: new ApprovalView.Params
                            {
                                walletIconRemoteSprite = remoteSprite,
                                walletData = wallet
                            });
                        },
                    });
                }
            }

            if (_showAllWalletsButton)
            {
                // "All Wallets" button
                _listItems[_walletsCounts].Initialize(new WCListSelect.Params
                {
                    title = "All wallets",
                    sprite = _allWalletsSprite,
                    onClick = () => { parentModal.OpenView(_walletSearchView); },
                    borderColor = new Color(0.2784f, 0.6313f, 1, 0.08f)
                });
            }
        }

        private async Task ShowQrCodeAndCopyButtonAsync()
        {
            WCLoadingAnimator.Instance.SubscribeGraphic(_qrCodeRawImage);

            var connectedData = await WalletConnectModal.ConnectionController.GetConnectionDataAsync();

            Uri = connectedData.Uri;

            WCLoadingAnimator.Instance.UnsubscribeGraphic(_qrCodeRawImage);

            var texture = QRCode.EncodeTexture(Uri);
            _qrCodeRawImage.texture = texture;
            _qrCodeRawImage.color = Color.white;

            _qrWalletIcon.SetActive(true);
        }
    }
}