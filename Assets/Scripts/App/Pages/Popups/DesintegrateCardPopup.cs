using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;
using TMPro;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class DesintigrateCardPopup : IUIPopup
    {
        public Transform CardTransform;

        private ILoadObjectsManager _loadObjectsManager;

        private IUIManager _uiManager;

        // private TextMeshProUGUI _description;
        private MenuButtonNoGlow _yesButton, _noButton, _backButton;

        private TextMeshProUGUI _buttonText;

        private CollectionCardData _cardData;

        public GameObject Self { get; private set; }

        public void Init()
        {
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _uiManager = GameClient.Get<IUIManager>();
        }

        public void Dispose()
        {
        }

        public void Hide()
        {
            /*if (cardTransform != null)
            {
                cardTransform.DOKill();
                cardTransform.DOScale(new Vector3(1f, 1f, 1f), 0.2f);
            }*/
            if (Self == null)

                return;

            Self.SetActive(false);
            Object.Destroy(Self);
            Self = null;
        }

        public void SetMainPriority()
        {
        }

        public void Show()
        {
            Self = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Popups/DesintegrateCardPopup"));
            Self.transform.SetParent(_uiManager.Canvas2.transform, false);

            _yesButton = Self.transform.Find("QuestionArea/YesButton").GetComponent<MenuButtonNoGlow>();
            _noButton = Self.transform.Find("QuestionArea/NoButton").GetComponent<MenuButtonNoGlow>();
            _backButton = Self.transform.Find("Button_Back").GetComponent<MenuButtonNoGlow>();

            _yesButton.OnClickEvent.AddListener(DesintegrateButtonHandler);
            _noButton.OnClickEvent.AddListener(CloseDesintegratePopup);
            _backButton.OnClickEvent.AddListener(CloseDesintegratePopup);

            // _description = _selfPage.transform.Find("DesintegrateArea/Description").GetComponent<TextMeshProUGUI>();
        }

        public void Show(object data)
        {
            Show();

            _cardData = data as CollectionCardData;

            // _description.text = _card.description;
            if (_cardData.Amount == 0)
            {
                _yesButton.GetComponent<MenuButtonNoGlow>().Interactable = false;
            } else
            {
                _yesButton.GetComponent<MenuButtonNoGlow>().Interactable = true;
            }
        }

        public void Update()
        {
        }

        private void CloseDesintegratePopup()
        {
            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.Click, Constants.SfxSoundVolume, false, false, true);
            Card libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.Cards.Find(card => card.Name == _cardData.CardName);
            _uiManager.DrawPopup<CardInfoPopup>(libraryCard);

            // (_uiManager.GetPopup<CardInfoPopup>() as CardInfoPopup).UpdateCardAmount();
            Hide();
        }

        private void DesintegrateButtonHandler()
        {
            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.Click, Constants.SfxSoundVolume, false, false, true);
            _cardData.Amount--;
            if (_cardData.Amount == 0)
            {
                _yesButton.GetComponent<MenuButtonNoGlow>().Interactable = false;
            }

            GameObject.Find("CardPreview").GetComponent<BoardCard>().UpdateAmount(_cardData.Amount);

            Card libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.Cards.Find(card => card.Name == _cardData.CardName);
            GameClient.Get<IPlayerManager>().ChangeGoo(5 * ((int)libraryCard.CardRank + 1));

            _uiManager.GetPage<CollectionPage>().UpdateGooValue();
        }
    }
}
