using System;
using Loom.ZombieBattleground.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using log4net;
using Object = UnityEngine.Object;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.BackendCommunication;

namespace Loom.ZombieBattleground
{
    public class MainMenuWithNavigationPage : IUIElement
    {
        private static readonly ILog Log = Logging.GetLog(nameof(MainMenuWithNavigationPage));

        private IUIManager _uiManager;

        private ILoadObjectsManager _loadObjectsManager;

        private IAppStateManager _stateManager;
        
        private ISoundManager _soundManager;
        
        private IPlayerManager _playerManager;  
        
        private GameObject _selfPage;

        private Button _buttonPlay, 
                       _buttonChangeMode;

        private TextMeshProUGUI _textGameMode;

        private Image _imageOverlordPortrait;
        
        private bool _isReturnToTutorial;
        
        public enum GameMode
        {
            SOLO,
            VS
        }

        private GameMode _gameMode;

        private const GameMode DefaultGameMode = GameMode.SOLO;
        
        #region IUIElement

        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _stateManager = GameClient.Get<IAppStateManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _playerManager = GameClient.Get<IPlayerManager>();

            _gameMode = DefaultGameMode;
        }
        
        public void Update()
        {            
        }

        public void Show()
        {
            _selfPage = Object.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Pages/MainMenuWithNavigationPage"));
            _selfPage.transform.SetParent(_uiManager.Canvas.transform, false);            
            
            _buttonPlay = _selfPage.transform.Find("Anchor_BottomRight/Panel_BattleSwitch/Button_Battle").GetComponent<Button>();                        
            _buttonPlay.onClick.AddListener(ButtonPlayHandler);
            _buttonChangeMode = _selfPage.transform.Find("Anchor_BottomRight/Panel_Battle_Mode").GetComponent<Button>();
            _buttonChangeMode.onClick.AddListener(ButtonChangeModeHandler);
            
            _imageOverlordPortrait = _selfPage.transform.Find("Image_OverlordPortrait").GetComponent<Image>();
            
            _textGameMode = _selfPage.transform.Find("Anchor_BottomRight/Panel_Battle_Mode/Text_BattleMode").GetComponent<TextMeshProUGUI>();
            
            _isReturnToTutorial = GameClient.Get<ITutorialManager>().UnfinishedTutorial;

            SetGameMode(_gameMode);
            
            _uiManager.DrawPopup<SideMenuPopup>(SideMenuPopup.MENU.BATTLE);
            _uiManager.DrawPopup<AreaBarPopup>();       
            _uiManager.DrawPopup<DeckSelectionPopup>();

            AnimateOverlordPortrait(); 
            
            Deck deck = _uiManager.GetPopup<DeckSelectionPopup>().GetSelectedDeck();
            _buttonPlay.interactable = CheckIfSelectDeckContainEnoughCards(deck);

            _uiManager.GetPopup<DeckSelectionPopup>().SelectDeckEvent += OnSelectDeckEvent;
        }
        
        public void Hide()
        {
            if (_selfPage == null)
                return;

            _selfPage.SetActive(false);
            Object.Destroy(_selfPage);
            _selfPage = null;
            
            _uiManager.GetPopup<DeckSelectionPopup>().SelectDeckEvent -= OnSelectDeckEvent;

            OnHide();
        }

        public void Dispose()
        {
        }

        #endregion

        private void OnHide()
        {
            _uiManager.HidePopup<SideMenuPopup>();
            _uiManager.HidePopup<AreaBarPopup>();
            _uiManager.HidePopup<DeckSelectionPopup>();
        }
        
        private void OnSelectDeckEvent(Deck deck)
        {
            _buttonPlay.interactable = CheckIfSelectDeckContainEnoughCards(deck);
        }

        #region Buttons Handlers

        private void ButtonPlayHandler()
        {
            if (GameClient.Get<ITutorialManager>().BlockAndReport(_buttonPlay.name))
                return;

            if (_isReturnToTutorial)
            {
                GameClient.Get<ITutorialManager>().ReportActivityAction(Enumerators.TutorialActivityAction.BattleStarted);

                GameClient.Get<IMatchManager>().FindMatch();
                return;
            }

            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);

            StartMatch();        
        }

        private void ButtonChangeModeHandler()
        {
            if (GameClient.Get<ITutorialManager>().BlockAndReport(_buttonChangeMode.name))
                return;

            _uiManager.DrawPopup<GameModePopup>();
        }

        #endregion

        public void StartMatch()
        {
            if (GameClient.Get<ITutorialManager>().IsTutorial)
            {
                GameClient.Get<ITutorialManager>().ReportActivityAction(Enumerators.TutorialActivityAction.BattleStarted);
            }

            Deck deck = _uiManager.GetPopup<DeckSelectionPopup>().GetSelectedDeck();
            if (deck == null)
            {
                deck = _uiManager.GetPopup<DeckSelectionPopup>().GetDefaultDeck();
            }
            _uiManager.GetPage<GameplayPage>().CurrentDeckId = deck.Id;
            GameClient.Get<IGameplayManager>().CurrentPlayerDeck = deck;
            GameClient.Get<IMatchManager>().FindMatch();

            _buttonPlay.interactable = false;

            // Wait for 1 frame to prevent multiple trigger on button
            Sequence waitSequence = DOTween.Sequence();
            waitSequence.AppendInterval(Time.fixedDeltaTime);
            waitSequence.AppendCallback(
                () =>
                {
                    if (_buttonPlay != null)
                    {
                        _buttonPlay.interactable = true;
                    }
                });
        }
        
        private bool CheckIfSelectDeckContainEnoughCards(Deck deck)
        {
            if (GameClient.Get<ITutorialManager>().IsTutorial || Constants.DevModeEnabled)
                return true;
                
            return deck.GetNumCards() == Constants.MinDeckSize;
        }

        public void SetOverlordPortrait(Enumerators.Faction faction)
        {
            switch(faction)
            {
                case Enumerators.Faction.AIR:
                    _imageOverlordPortrait.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/MainMenu/OverlordPortrait/main_portrait_air");                  
                    break;
                case Enumerators.Faction.FIRE:
                    _imageOverlordPortrait.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/MainMenu/OverlordPortrait/main_portrait_fire");
                    break;
                case Enumerators.Faction.EARTH:
                    _imageOverlordPortrait.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/MainMenu/OverlordPortrait/main_portrait_earth");
                    break;
                case Enumerators.Faction.TOXIC:
                    _imageOverlordPortrait.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/MainMenu/OverlordPortrait/main_portrait_toxic");
                    break;
                case Enumerators.Faction.WATER:
                    _imageOverlordPortrait.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/MainMenu/OverlordPortrait/main_portrait_water");
                    break;
                case Enumerators.Faction.LIFE:
                    _imageOverlordPortrait.sprite = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UI/MainMenu/OverlordPortrait/main_portrait_life");
                    break;
                default:
                    Log.Info($"No OverlordPortrait found for faction {faction}");
                    return;
            }            
        }
        
        public void SetGameMode(GameMode gameMode)
        {
            _gameMode = gameMode;
            if(_gameMode == GameMode.SOLO)
            {
                GameClient.Get<IMatchManager>().MatchType = Enumerators.MatchType.LOCAL;
                _textGameMode.text = "SOLO";
            }
            else if(_gameMode == GameMode.VS)
            {
                GameClient.Get<IMatchManager>().MatchType = Enumerators.MatchType.PVP;
                _textGameMode.text = "VS\nCASUAL";                 
            }
        }
        
        private void AnimateOverlordPortrait()
        {
            _imageOverlordPortrait.transform.localScale = Vector3.one;
            _imageOverlordPortrait.transform.DOScale(1.05f, 3f).SetLoops(-1, LoopType.Yoyo);
        }
    }
}
