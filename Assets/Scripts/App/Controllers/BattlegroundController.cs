using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DG.Tweening;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Gameplay;
using Loom.ZombieBattleground.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class BattlegroundController : IController
    {
        public bool IsPreviewActive;

        public bool CardsZoomed = false;

        public Coroutine CreatePreviewCoroutine;

        public GameObject CurrentBoardCard;

        public int CurrentPreviewedCardId;

        public int CurrentTurn;

        public List<BoardUnitView> OpponentBoardCards = new List<BoardUnitView>();

        public List<BoardUnitView> OpponentGraveyardCards = new List<BoardUnitView>();

        public List<GameObject> OpponentHandCards = new List<GameObject>();

        public List<BoardUnitView> PlayerBoardCards = new List<BoardUnitView>();

        public GameObject PlayerBoardObject, OpponentBoardObject, PlayerGraveyardObject, OpponentGraveyardObject;

        public List<BoardUnitView> PlayerGraveyardCards = new List<BoardUnitView>();

        public List<BoardCard> PlayerHandCards = new List<BoardCard>();

        private AIController _aiController;

        private bool _battleDynamic;

        private CardsController _cardsController;

        private SkillsController _skillsController;

        private IDataManager _dataManager;

        private IGameplayManager _gameplayManager;

        private BoardUnitView _lastBoardUntilOnPreview;

        private PlayerController _playerController;

        private VfxController _vfxController;

        private IPlayerManager _playerManager;

        private ISoundManager _soundManager;

        private ITimerManager _timerManager;

        private ITutorialManager _tutorialManager;

        private IUIManager _uiManager;

        private Sequence _rearrangingTopRealTimeSequence, _rearrangingBottomRealTimeSequence;

        public event Action<int> PlayerGraveyardUpdated;

        public event Action<int> OpponentGraveyardUpdated;

        public event Action TurnStarted;

        public event Action TurnEnded;

        public PvPManager _pvpManager;

        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _timerManager = GameClient.Get<ITimerManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _uiManager = GameClient.Get<IUIManager>();
            _playerManager = GameClient.Get<IPlayerManager>();
            _pvpManager = GameClient.Get<PvPManager>();

            _playerController = _gameplayManager.GetController<PlayerController>();
            _vfxController = _gameplayManager.GetController<VfxController>();
            _cardsController = _gameplayManager.GetController<CardsController>();
            _aiController = _gameplayManager.GetController<AIController>();
            _skillsController = _gameplayManager.GetController<SkillsController>();

            _gameplayManager.GameEnded += GameEndedHandler;

            _gameplayManager.GameInitialized += OnGameInitializedHandler;

            _pvpManager.OnGetEndTurnAction += OnGetEndTurnHandler;
        }

        private void OnGetEndTurnHandler()
        {
            StopTurn();
        }

        public void Dispose()
        {
        }

        public void Update()
        {
            if (_gameplayManager.IsGameStarted && !_gameplayManager.IsGameEnded)
            {
                CheckGameDynamic();

                foreach (BoardUnitView item in PlayerBoardCards)
                {
                    item.Update();
                }

                foreach (BoardUnitView item in OpponentBoardCards)
                {
                    item.Update();
                }
            }
        }

        public void ResetAll()
        {
            if (CreatePreviewCoroutine != null)
            {
                MainApp.Instance.StopCoroutine(CreatePreviewCoroutine);
            }

            CreatePreviewCoroutine = null;

            if (CurrentBoardCard != null && CurrentBoardCard)
            {
                Object.Destroy(CurrentBoardCard);
            }

            CurrentBoardCard = null;

            ClearBattleground();
        }

        public void KillBoardCard(BoardUnitModel cardToDestroyModel)
        {
            BoardUnitView cardToDestroy = GetBoardUnitViewByModel(cardToDestroyModel);
            if (cardToDestroy == null)
                return;

            if (_lastBoardUntilOnPreview != null && cardToDestroy == _lastBoardUntilOnPreview)
            {
                DestroyCardPreview();
            }

            cardToDestroy.Transform.position = new Vector3(cardToDestroy.Transform.position.x,
                cardToDestroy.Transform.position.y, cardToDestroy.Transform.position.z + 0.2f);

            _timerManager.AddTimer(
                x =>
                {
                    //cardToDestroy.Transform.DOShakePosition(.7f, 0.25f, 10, 90, false, false);
                    CreateDeadAnimation(cardToDestroy);


                    string cardDeathSoundName =
                        cardToDestroy.Model.Card.LibraryCard.Name.ToLower() + "_" + Constants.CardSoundDeath;
                    float soundLength = 0f;

                    if (!cardToDestroy.Model.OwnerPlayer.Equals(_gameplayManager.CurrentTurnPlayer))
                    {
                        _soundManager.PlaySound(Enumerators.SoundType.CARDS, cardDeathSoundName,
                            Constants.ZombieDeathVoDelayBeforeFadeout, Constants.ZombiesSoundVolume,
                            Enumerators.CardSoundType.DEATH);
                        soundLength = _soundManager.GetSoundLength(Enumerators.SoundType.CARDS, cardDeathSoundName);
                    }

                    _timerManager.AddTimer(
                        t =>
                        {
                            cardToDestroy.Model.OwnerPlayer.BoardCards.Remove(cardToDestroy);
                            cardToDestroy.Model.OwnerPlayer.RemoveCardFromBoard(cardToDestroy.Model.Card);
                            cardToDestroy.Model.OwnerPlayer.AddCardToGraveyard(cardToDestroy.Model.Card);

                            cardToDestroy.Model.InvokeUnitDied();
                            cardToDestroy.Transform.DOKill();
                            Object.Destroy(cardToDestroy.GameObject);

                            _timerManager.AddTimer(
                                f =>
                                {
                                    UpdatePositionOfBoardUnitsOfOpponent();
                                    UpdatePositionOfBoardUnitsOfPlayer(_gameplayManager.CurrentPlayer.BoardCards);
                                },
                                null,
                                Time.deltaTime);
                        },
                        null,
                        soundLength);
                });
        }

        private void CreateDeadAnimation(BoardUnitView cardToDestroy)
        {
            _vfxController.CreateDeathZombieAnimation(cardToDestroy);
        }

        public void CheckGameDynamic()
        {
            if (!_battleDynamic)
            {
                _soundManager.CrossfaidSound(Enumerators.SoundType.BATTLEGROUND, null, true);
            }

            _battleDynamic = true;
        }

        public void UpdateGraveyard(int index, Player player)
        {
            if (player.IsLocalPlayer)
            {
                PlayerGraveyardUpdated?.Invoke(index);
            }
            else
            {
                OpponentGraveyardUpdated?.Invoke(index);
            }
        }

        public void ClearBattleground()
        {
            PlayerHandCards.Clear();
            OpponentHandCards.Clear();

            PlayerBoardCards.Clear();
            OpponentBoardCards.Clear();

            PlayerGraveyardCards.Clear();
            OpponentGraveyardCards.Clear();
        }

        public void InitializeBattleground()
        {
            CurrentTurn = Constants.FirstGameTurnIndex;

#if DEV_MODE
            _gameplayManager.OpponentPlayer.Health = 1;
            _gameplayManager.CurrentPlayer.Health = 99;
#endif

            _playerManager.OpponentGraveyardCards = OpponentGraveyardCards;

            PlayerBoardObject = GameObject.Find("PlayerBoard");
            OpponentBoardObject = GameObject.Find("OpponentBoard");
            PlayerGraveyardObject = GameObject.Find("GraveyardPlayer");
            OpponentGraveyardObject = GameObject.Find("GraveyardOpponent");
        }

        public void StartGameplayTurns()
        {
            StartTurn();

            if (!_gameplayManager.IsTutorial)
            {
                Player player = _gameplayManager.CurrentTurnPlayer.IsLocalPlayer ?
                    _gameplayManager.OpponentPlayer :
                    _gameplayManager.CurrentPlayer;
                _cardsController.AddCardToHand(player);
            }
        }

        public void GameEndedHandler(Enumerators.EndGameType endGameType)
        {
            CurrentTurn = 0;

            ClearBattleground();
        }

        public void StartTurn()
        {
            if (_gameplayManager.IsGameEnded)
                return;

            CurrentTurn++;

            _gameplayManager.CurrentTurnPlayer.Turn++;

            if (_dataManager.CachedUserLocalData.Tutorial && !_tutorialManager.IsTutorial)
            {
                Debug.Log("_dataManager.CachedUserLocalData.Tutorial = " + _dataManager.CachedUserLocalData.Tutorial);
                Debug.Log("_tutorialManager.IsTutorial = " + _tutorialManager.IsTutorial);
                _tutorialManager.StartTutorial();
            }

            _uiManager.GetPage<GameplayPage>().SetEndTurnButtonStatus(_gameplayManager.IsLocalPlayerTurn());

            UpdatePositionOfCardsInOpponentHand();
            _playerController.IsActive = _gameplayManager.IsLocalPlayerTurn();

            if (_gameplayManager.IsLocalPlayerTurn())
            {
                List<BoardUnitView> creatures = new List<BoardUnitView>();

                foreach (BoardUnitView card in PlayerBoardCards)
                {
                    if (_playerController == null || !card.GameObject)
                    {
                        creatures.Add(card);
                        continue;
                    }

                    card.Model.OnStartTurn();
                }

                foreach (BoardUnitView item in creatures)
                {
                    PlayerBoardCards.Remove(item);
                }

                creatures.Clear();

                foreach (BoardUnitView card in PlayerBoardCards)
                {
                    card.SetHighlightingEnabled(true);
                }
            }
            else
            {
                foreach (BoardUnitView card in OpponentBoardCards)
                {
                    card.Model.OnStartTurn();
                }

                foreach (BoardCard card in PlayerHandCards)
                {
                    card.SetHighlightingEnabled(false);
                }

                foreach (BoardUnitView card in PlayerBoardCards)
                {
                    card.SetHighlightingEnabled(false);
                }
            }

            _gameplayManager.CurrentPlayer.InvokeTurnStarted();
            _gameplayManager.OpponentPlayer.InvokeTurnStarted();

            _playerController.UpdateHandCardsHighlight();

            _tutorialManager.ReportAction(Enumerators.TutorialReportAction.START_TURN);

            TurnStarted?.Invoke();
        }

        public async void EndTurn()
        {
            if (_gameplayManager.IsGameEnded)
                return;

            if (_gameplayManager.IsLocalPlayerTurn())
            {
                _uiManager.GetPage<GameplayPage>().SetEndTurnButtonStatus(false);

                foreach (BoardUnitView card in PlayerBoardCards)
                {
                    card.Model.OnEndTurn();
                }

                if (GameClient.Get<IMatchManager>().MatchType == Enumerators.MatchType.PVP)
                {
                    await _gameplayManager.GetController<OpponentController>().ActionEndTurn(_gameplayManager.CurrentPlayer);
                }
            }
            else
            {
                foreach (BoardUnitView card in OpponentBoardCards)
                {
                    card.Model.OnEndTurn();
                }
            }

            //_gameplayManager.CurrentPlayer.InvokeTurnEnded();
            //_gameplayManager.OpponentPlayer.InvokeTurnEnded();

            _gameplayManager.CurrentTurnPlayer = _gameplayManager.IsLocalPlayerTurn() ?
                _gameplayManager.OpponentPlayer :
                _gameplayManager.CurrentPlayer;

            _tutorialManager.ReportAction(Enumerators.TutorialReportAction.END_TURN);

            TurnEnded?.Invoke();
        }

        public void StopTurn()
        {
            EndTurn();

            if (_gameplayManager.IsLocalPlayerTurn())
            {
                _uiManager.DrawPopup<YourTurnPopup>();

                _timerManager.AddTimer((x) =>
                {
                    StartTurn();
                }, null, 4f);
            }
            else
            {
                StartTurn();
            }            
        }

        public void RemovePlayerCardFromBoardToGraveyard(WorkingCard card)
        {
            BoardUnitView boardCard = PlayerBoardCards.Find(x => x.Model.Card == card);
            if (boardCard == null)
                return;

            boardCard.Transform.localPosition = new Vector3(boardCard.Transform.localPosition.x,
                boardCard.Transform.localPosition.y, -0.2f);

            PlayerBoardCards.Remove(boardCard);
            PlayerGraveyardCards.Add(boardCard);

            boardCard.SetHighlightingEnabled(false);
            boardCard.StopSleepingParticles();
            boardCard.GameObject.GetComponent<SortingGroup>().sortingLayerID = SRSortingLayers.BoardCards;

            Object.Destroy(boardCard.GameObject.GetComponent<BoxCollider2D>());

        }

        public void RemoveOpponentCardFromBoardToGraveyard(WorkingCard card)
        {
            Vector3 graveyardPos = OpponentGraveyardObject.transform.position + new Vector3(0.0f, -0.2f, 0.0f);
            BoardUnitView boardCard = OpponentBoardCards.Find(x => x.Model.Card == card);
            if (boardCard != null)
            {
                if (boardCard.Transform != null)
                {
                    boardCard.Transform.localPosition = new Vector3(boardCard.Transform.localPosition.x,
                        boardCard.Transform.localPosition.y, -0.2f);
                }

                OpponentBoardCards.Remove(boardCard);

                boardCard.SetHighlightingEnabled(false);
                boardCard.StopSleepingParticles();
                if (boardCard.GameObject != null)
                {
                    boardCard.GameObject.GetComponent<SortingGroup>().sortingLayerID = SRSortingLayers.BoardCards;
                    Object.Destroy(boardCard.GameObject.GetComponent<BoxCollider2D>());
                }

                Debug.Log("Destroy = " + boardCard.Model.CurrentHp + "_" + boardCard.Model.Card.LibraryCard.Name);
            }
            else if (_aiController.CurrentSpellCard != null && card == _aiController.CurrentSpellCard.WorkingCard)
            {
                _aiController.CurrentSpellCard.SetHighlightingEnabled(false);
                _aiController.CurrentSpellCard.GameObject.GetComponent<SortingGroup>().sortingLayerID = SRSortingLayers.BoardCards;
                Object.Destroy(_aiController.CurrentSpellCard.GameObject.GetComponent<BoxCollider2D>());
                Sequence sequence = DOTween.Sequence();
                sequence.PrependInterval(2.0f);
                sequence.Append(_aiController.CurrentSpellCard.Transform.DOMove(graveyardPos, 0.5f));
                sequence.Append(_aiController.CurrentSpellCard.Transform.DOScale(new Vector2(0.6f, 0.6f), 0.5f));
                sequence.OnComplete(
                    () =>
                    {
                        _aiController.CurrentSpellCard = null;
                    });
            }
        }

        public void UpdatePositionOfBoardUnitsOfPlayer(List<BoardUnitView> cardsList, Action onComplete = null)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            if (_rearrangingBottomRealTimeSequence != null)
            {
                _rearrangingBottomRealTimeSequence.Kill();
                _rearrangingBottomRealTimeSequence = null;
            }

            float boardWidth = 0.0f;
            float spacing = 0.2f;
            float cardWidth = 0.0f;
            for (int i = 0; i < cardsList.Count; i++)
            {
                cardWidth = 2.5f;
                boardWidth += cardWidth;
                boardWidth += spacing;
            }

            boardWidth -= spacing;

            List<Vector2> newPositions = new List<Vector2>(cardsList.Count);
            Vector3 pivot = PlayerBoardObject.transform.position;

            for (int i = 0; i < cardsList.Count; i++)
            {
                newPositions.Add(new Vector2(pivot.x - boardWidth / 2 + cardWidth / 2, pivot.y - 1.7f));
                pivot.x += boardWidth / cardsList.Count;
            }

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < cardsList.Count; i++)
            {
                BoardUnitView card = cardsList[i];
                sequence.Insert(0, card.Transform.DOMove(newPositions[i], 0.4f).SetEase(Ease.OutSine));
            }

            _rearrangingBottomRealTimeSequence = sequence;
            sequence.OnComplete(
                () =>
                {
                    onComplete?.Invoke();
                });
        }

        public void UpdatePositionOfBoardUnitsOfOpponent(Action onComplete = null)
        {
            if (_gameplayManager.IsGameEnded)
                return;

            if (_rearrangingTopRealTimeSequence != null)
            {
                _rearrangingTopRealTimeSequence.Kill();
                _rearrangingTopRealTimeSequence = null;
            }

            List<BoardUnitView> opponentBoardCards = _gameplayManager.OpponentPlayer.BoardCards;

            float boardWidth = 0.0f;
            float spacing = 0.2f;
            float cardWidth = 0.0f;

            for (int i = 0; i < opponentBoardCards.Count; i++)
            {
                cardWidth = 2.5f;
                boardWidth += cardWidth;
                boardWidth += spacing;
            }

            boardWidth -= spacing;

            List<Vector2> newPositions = new List<Vector2>(opponentBoardCards.Count);
            Vector3 pivot = OpponentBoardObject.transform.position;

            for (int i = 0; i < opponentBoardCards.Count; i++)
            {
                newPositions.Add(new Vector2(pivot.x - boardWidth / 2 + cardWidth / 2, pivot.y + 0.0f));
                pivot.x += boardWidth / opponentBoardCards.Count;
            }

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < opponentBoardCards.Count; i++)
            {
                BoardUnitView card = opponentBoardCards[i];
                sequence.Insert(0, card.Transform.DOMove(newPositions[i], 0.4f).SetEase(Ease.OutSine));
            }

            _rearrangingTopRealTimeSequence = sequence;
            sequence.OnComplete(
                () =>
                {
                    onComplete?.Invoke();
                });
        }

        // rewrite
        public void CreateCardPreview(object target, Vector3 pos, bool highlight = true)
        {
            IsPreviewActive = true;

            switch (target)
            {
                case BoardCard card:
                    CurrentPreviewedCardId = card.WorkingCard.InstanceId;
                    break;
                case BoardUnitView unit:
                    _lastBoardUntilOnPreview = unit;
                    CurrentPreviewedCardId = unit.Model.Card.InstanceId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }

            CreatePreviewCoroutine = MainApp.Instance.StartCoroutine(CreateCardPreviewAsync(target, pos, highlight));
        }

        // rewrite
        public IEnumerator CreateCardPreviewAsync(object target, Vector3 pos, bool highlight)
        {
            yield return new WaitForSeconds(0.3f);

            WorkingCard card = null;

            switch (target)
            {
                case BoardCard card1:
                    card = card1.WorkingCard;
                    break;
                case BoardUnitView unit:
                    card = unit.Model.Card;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }

            BoardCard boardCard;
            switch (card.LibraryCard.CardKind)
            {
                case Enumerators.CardKind.CREATURE:
                    CurrentBoardCard = Object.Instantiate(_cardsController.CreatureCardViewPrefab);
                    boardCard = new UnitBoardCard(CurrentBoardCard);
                    break;
                case Enumerators.CardKind.SPELL:
                    CurrentBoardCard = Object.Instantiate(_cardsController.ItemCardViewPrefab);
                    boardCard = new SpellBoardCard(CurrentBoardCard);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            boardCard.Init(card);
            if (highlight)
            {
                highlight = boardCard.CanBePlayed(card.Owner) && boardCard.CanBeBuyed(card.Owner);
            }

            boardCard.SetHighlightingEnabled(highlight);
            boardCard.IsPreview = true;

            InternalTools.SetLayerRecursively(boardCard.GameObject, 0);

            switch (target)
            {
                case BoardUnitView boardUnit:
                    boardCard.DrawTooltipInfoOfUnit(boardUnit);
                    break;
                case BoardCard tooltipCard:
                    boardCard.DrawTooltipInfoOfCard(tooltipCard);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }

            Vector3 newPos = pos;
            newPos.y += 2.0f;
            CurrentBoardCard.transform.position = newPos;
            CurrentBoardCard.transform.localRotation = Quaternion.Euler(Vector3.zero);

            Vector3 sizeOfCard = Vector3.one;

            sizeOfCard = !InternalTools.IsTabletScreen() ? new Vector3(.8f, .8f, .8f) : new Vector3(.4f, .4f, .4f);

            CurrentBoardCard.transform.localScale = sizeOfCard;

            CurrentBoardCard.GetComponent<SortingGroup>().sortingLayerID = SRSortingLayers.GameUI3;
            CurrentBoardCard.layer = LayerMask.NameToLayer("Default");
            CurrentBoardCard.transform.DOMoveY(newPos.y + 1.0f, 0.1f);
        }

        // rewrite
        public void DestroyCardPreview()
        {
            if (!IsPreviewActive)
                return;

            GameClient.Get<ICameraManager>().FadeOut(null, 1, true);

            MainApp.Instance.StartCoroutine(DestroyCardPreviewAsync());
            if (CreatePreviewCoroutine != null)
            {
                MainApp.Instance.StopCoroutine(CreatePreviewCoroutine);
            }

            IsPreviewActive = false;
        }

        // rewrite
        public IEnumerator DestroyCardPreviewAsync()
        {
            if (CurrentBoardCard != null)
            {
                _lastBoardUntilOnPreview = null;
                GameObject oldCardPreview = CurrentBoardCard;
                foreach (SpriteRenderer renderer in oldCardPreview.GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.DOFade(0.0f, 0.2f);
                }

                foreach (TextMeshPro text in oldCardPreview.GetComponentsInChildren<TextMeshPro>())
                {
                    text.DOFade(0.0f, 0.2f);
                }

                yield return new WaitForSeconds(0.5f);
                Object.Destroy(oldCardPreview.gameObject);
            }
        }

        public void UpdatePositionOfCardsInPlayerHand(bool isMove = false)
        {
            float handWidth = 0.0f;
            float spacing = -1.5f;
            float scaling = 0.25f;
            Vector3 pivot = new Vector3(6f, -7.5f, 0f);
            int twistPerCard = -5;

            if (CardsZoomed)
            {
                spacing = -2.6f;
                scaling = 0.31f;
                pivot = new Vector3(-1.3f, -6.5f, 0f);
                twistPerCard = -3;
            }

            for (int i = 0; i < PlayerHandCards.Count; i++)
            {
                handWidth += spacing;
            }

            handWidth -= spacing;

            if (PlayerHandCards.Count == 1)
            {
                twistPerCard = 0;
            }

            int totalTwist = twistPerCard * PlayerHandCards.Count;
            float startTwist = (totalTwist - twistPerCard) / 2f;
            float scalingFactor = 0.04f;
            Vector3 moveToPosition = Vector3.zero;

            for (int i = 0; i < PlayerHandCards.Count; i++)
            {
                BoardCard card = PlayerHandCards[i];
                float twist = startTwist - i * twistPerCard;
                float nudge = Mathf.Abs(twist);

                nudge *= scalingFactor;
                moveToPosition = new Vector3(pivot.x - handWidth / 2, pivot.y - nudge,
                    (PlayerHandCards.Count - i) * 0.1f);

                if (isMove)
                {
                    card.IsNewCard = false;
                }

                card.UpdateCardPositionInHand(moveToPosition, Vector3.forward * twist, Vector3.one * scaling);

                pivot.x += handWidth / PlayerHandCards.Count;

                card.GameObject.GetComponent<SortingGroup>().sortingLayerID = SRSortingLayers.HandCards;
                card.GameObject.GetComponent<SortingGroup>().sortingOrder = i;
            }
        }

        public void UpdatePositionOfCardsInOpponentHand(bool isMove = false, bool isNewCard = false)
        {
            float handWidth = 0.0f;
            float spacing = -1.0f;

            for (int i = 0; i < OpponentHandCards.Count; i++)
            {
                handWidth += spacing;
            }

            handWidth -= spacing;

            Vector3 pivot = new Vector3(-3.2f, 8.5f, 0f);
            int twistPerCard = 5;

            if (OpponentHandCards.Count == 1)
            {
                twistPerCard = 0;
            }

            int totalTwist = twistPerCard * OpponentHandCards.Count;
            float startTwist = (totalTwist - twistPerCard) / 2f;

            for (int i = 0; i < OpponentHandCards.Count; i++)
            {
                GameObject card = OpponentHandCards[i];
                float twist = startTwist - i * twistPerCard;

                Vector3 movePosition = new Vector2(pivot.x - handWidth / 2, pivot.y);
                Vector3 rotatePosition = new Vector3(0, 0, twist);

                if (isMove)
                {
                    if (i == OpponentHandCards.Count - 1 && isNewCard)
                    {
                        card.transform.position = new Vector3(-8.2f, 5.7f, 0);
                        card.transform.eulerAngles = Vector3.forward * 90f;
                    }

                    card.transform.DOMove(movePosition, 0.5f);
                    card.transform.DORotate(rotatePosition, 0.5f);
                }
                else
                {
                    card.transform.position = movePosition;
                    card.transform.rotation = Quaternion.Euler(rotatePosition);
                }

                pivot.x += handWidth / OpponentHandCards.Count;

                card.GetComponent<SortingGroup>().sortingOrder = i;
            }
        }

        /// <summary>
        /// Gets an active <see cref="BoardUnitView"/> that has <paramref name="boardUnitModel"/> attached to it.
        /// </summary>
        /// <param name="boardUnitModel"></param>
        /// <returns></returns>
        public BoardUnitView GetBoardUnitViewByModel(BoardUnitModel boardUnitModel)
        {
            BoardUnitView cardToDestroy =
                OpponentBoardCards
                    .Concat(OpponentBoardCards)
                    .Concat(OpponentGraveyardCards)
                    .Concat(PlayerBoardCards)
                    .Concat(PlayerGraveyardCards)
                    .First(x => x.Model == boardUnitModel);
            return cardToDestroy;
        }

        public BoardUnitView GetBoardUnitFromHisObject(GameObject unitObject)
        {
            BoardUnitView unit = PlayerBoardCards.Find(x => x.GameObject.Equals(unitObject));

            if (unit == null)
            {
                unit = OpponentBoardCards.Find(x => x.GameObject.Equals(unitObject));
            }

            return unit;
        }

        public BoardCard GetBoardCardFromHisObject(GameObject cardObject)
        {
            BoardCard card = PlayerHandCards.Find(x => x.GameObject.Equals(cardObject));

            return card;
        }

        public void DestroyBoardUnit(BoardUnitModel unit)
        {
            _gameplayManager.GetController<BattleController>().CheckOnKillEnemyZombie(unit);

            unit?.Die();
        }

        public void TakeControlUnit(Player to, BoardUnitModel unit)
        {
            // implement functionality of the take control
        }

        public BoardUnitView CreateBoardUnit(Player owner, WorkingCard card)
        {
            GameObject playerBoard = owner.IsLocalPlayer ? PlayerBoardObject : OpponentBoardObject;

            BoardUnitView boardUnitView = new BoardUnitView(new BoardUnitModel(), playerBoard.transform);
            boardUnitView.Transform.tag = owner.IsLocalPlayer ? SRTags.PlayerOwned : SRTags.OpponentOwned;
            boardUnitView.Transform.SetParent(playerBoard.transform);
            boardUnitView.Transform.position = new Vector2(1.9f * owner.BoardCards.Count, 0);
            boardUnitView.Model.OwnerPlayer = owner;
            boardUnitView.SetObjectInfo(card);

            boardUnitView.PlayArrivalAnimation();

            return boardUnitView;
        }


        public BoardObject GetTargetById(int id, Enumerators.AffectObjectType affectObjectType)
        {
            switch(affectObjectType)
            {
                case Enumerators.AffectObjectType.PLAYER:
                    return _gameplayManager.OpponentPlayer.Id == id ? _gameplayManager.OpponentPlayer : _gameplayManager.CurrentPlayer;
                case Enumerators.AffectObjectType.CHARACTER:
                    {
                        List<BoardUnitView> units = new List<BoardUnitView>();
                        units.AddRange(_gameplayManager.OpponentPlayer.BoardCards);
                        units.AddRange(_gameplayManager.CurrentPlayer.BoardCards);

                        BoardUnitView unit = units.Find(u => u.Model.Id == id);

                        units.Clear();

                        if (unit != null)
                            return unit.Model;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(affectObjectType), affectObjectType, null);
            }

            return null;
        }

        public BoardSkill GetSkillById(Player owner, int id)
        {
            if (!owner.IsLocalPlayer)
            {
                if (_skillsController.OpponentPrimarySkill.Id == id)
                    return _skillsController.OpponentPrimarySkill;
                else if (_skillsController.OpponentSecondarySkill.Id == id)
                    return _skillsController.OpponentSecondarySkill;
            }
            else
            {
                if (_skillsController.PlayerPrimarySkill.Id == id)
                    return _skillsController.PlayerPrimarySkill;
                else if (_skillsController.PlayerSecondarySkill.Id == id)
                    return _skillsController.PlayerSecondarySkill;
            }

            return null;
        }

        public BoardUnitModel GetBoardUnitById(Player owner, int id)
        {
            return owner.BoardCards.Find(u => u.Model.Id == id).Model;
        }

        public BoardObject GetBoardObjectById(int id)
        {
            List<BoardUnitView> units = new List<BoardUnitView>();
            units.AddRange(_gameplayManager.OpponentPlayer.BoardCards);
            units.AddRange(_gameplayManager.CurrentPlayer.BoardCards);

            BoardUnitModel unit = units.Find(u => u.Model.Id == id).Model;

            units.Clear();

            return unit;
        }

        #region specific setup of battleground

        public void SetupBattlegroundAsSpecific(SpecificBattlegroundInfo specificBattlegroundInfo)
        {
            SetupOverlordsAsSpecific(specificBattlegroundInfo);
            SetupOverlordsHandsAsSpecific(specificBattlegroundInfo.PlayerInfo.CardsInHand, specificBattlegroundInfo.OpponentInfo.CardsInHand);
            SetupOverlordsDecksAsSpecific(specificBattlegroundInfo.PlayerInfo.CardsInDeck, specificBattlegroundInfo.OpponentInfo.CardsInDeck);
            SetupOverlordsBoardUnitsAsSpecific(specificBattlegroundInfo.PlayerInfo.CardsOnBoard, specificBattlegroundInfo.OpponentInfo.CardsOnBoard);
            SetupGeneralUIAsSpecific(specificBattlegroundInfo);
        }

        private void SetupOverlordsAsSpecific(SpecificBattlegroundInfo specificBattlegroundInfo)
        {
            _gameplayManager.OpponentPlayer.Health = specificBattlegroundInfo.OpponentInfo.Health;
            _gameplayManager.OpponentPlayer.GooOnCurrentTurn = specificBattlegroundInfo.OpponentInfo.MaximumGoo;
            _gameplayManager.OpponentPlayer.Goo = specificBattlegroundInfo.OpponentInfo.CurrentGoo;
            _gameplayManager.GetController<AIController>().SetAiType(specificBattlegroundInfo.OpponentInfo.AiType);

            _gameplayManager.CurrentPlayer.Health = specificBattlegroundInfo.PlayerInfo.Health;
            _gameplayManager.CurrentPlayer.GooOnCurrentTurn = specificBattlegroundInfo.PlayerInfo.MaximumGoo;
            _gameplayManager.CurrentPlayer.Goo = specificBattlegroundInfo.PlayerInfo.CurrentGoo;
        }

        private void SetupOverlordsHandsAsSpecific(List<string> playerCards, List<string> opponentCards)
        {
            foreach (string cardName in playerCards)
                _gameplayManager.CurrentPlayer.AddCardToHand(_cardsController.GetWorkingCardFromName(_gameplayManager.CurrentPlayer, cardName), true);

            foreach (string cardName in opponentCards)
                _gameplayManager.OpponentPlayer.AddCardToHand(_cardsController.GetWorkingCardFromName(_gameplayManager.OpponentPlayer, cardName), true);
        }

        private void SetupOverlordsDecksAsSpecific(List<string> playerCards, List<string> opponentCards)
        {
            _gameplayManager.CurrentPlayer.SetDeck(playerCards);
            _gameplayManager.OpponentPlayer.SetDeck(opponentCards);
        }

        private void SetupOverlordsGraveyardsAsSpecific(List<string> playerCards, List<string> opponentCards)
        {
            // todo implement logic
        }

        private void SetupOverlordsBoardUnitsAsSpecific(List<SpecificBattlegroundInfo.UnitOnBoardInfo> playerCards,
                                                        List<SpecificBattlegroundInfo.UnitOnBoardInfo> opponentCards)
        {
            BoardUnitView workingUnitView = null;

            foreach (SpecificBattlegroundInfo.UnitOnBoardInfo cardInfo in playerCards)
            {
                workingUnitView = _cardsController.SpawnUnitOnBoard(_gameplayManager.CurrentPlayer, cardInfo.Name);
                workingUnitView.Model.CantAttackInThisTurnBlocker = !cardInfo.IsManuallyPlayable;
            }

            foreach (SpecificBattlegroundInfo.UnitOnBoardInfo cardInfo in opponentCards)
            {
                workingUnitView = _cardsController.SpawnUnitOnBoard(_gameplayManager.OpponentPlayer, cardInfo.Name);
                workingUnitView.Model.CantAttackInThisTurnBlocker = !cardInfo.IsManuallyPlayable;
            }
        }

        private void SetupGeneralUIAsSpecific(SpecificBattlegroundInfo specificBattlegroundInfo)
        {
            // todo implement logic
        }

        private void OnGameInitializedHandler()
        {
        }
    }

    #endregion
}
