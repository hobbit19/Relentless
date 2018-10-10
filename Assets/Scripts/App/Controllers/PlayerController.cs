using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Loom.ZombieBattleground
{
    public class PlayerController : IController
    {
        private IGameplayManager _gameplayManager;

        private IDataManager _dataManager;

        private ITutorialManager _tutorialManager;

        private ITimerManager _timerManager;

        private IMatchManager _matchManager;

        private CardsController _cardsController;

        private BattlegroundController _battlegroundController;
        private BoardArrowController _boardArrowController;

        private bool _startedOnClickDelay;

        private bool _isPreviewHandCard;

        private float _delayTimerOfClick;

        private bool _cardsZooming;

        private BoardCard _topmostBoardCard;

        private BoardUnitView _selectedBoardUnitView;

        private PointerEventSolver _pointerEventSolver;

        public bool IsPlayerStunned { get; set; }

        public bool IsCardSelected { get; set; }

        public bool IsActive { get; set; }

        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _timerManager = GameClient.Get<ITimerManager>();
            _matchManager = GameClient.Get<IMatchManager>();

            _cardsController = _gameplayManager.GetController<CardsController>();
            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();
            _boardArrowController = _gameplayManager.GetController<BoardArrowController>();

            _gameplayManager.GameStarted += GameStartedHandler;
            _gameplayManager.GameEnded += GameEndedHandler;

            _pointerEventSolver = new PointerEventSolver();
            _pointerEventSolver.DragStarted += PointerSolverDragStartedHandler;
            _pointerEventSolver.Clicked += PointerEventSolverClickedHandler;
            _pointerEventSolver.Ended += PointerEventSolverEndedHandler;
        }

        public void Dispose()
        {
        }

        public void Update()
        {
            if (!_gameplayManager.IsGameStarted || _gameplayManager.IsGameEnded)
                return;

            if (_tutorialManager.IsTutorial && _tutorialManager.CurrentTutorialDataStep != null && !_tutorialManager.CurrentTutorialDataStep.CanHandleInput)
                return;

            _pointerEventSolver.Update();

            HandleInput();
        }

        public void ResetAll()
        {
            StopHandTimer();
            _timerManager.StopTimer(SetStatusZoomingFalse);
            _timerManager.StopTimer(UpdateCardDistribution);
        }

        public void InitializePlayer(int playerId)
        {
            _gameplayManager.CurrentPlayer = new Player(playerId, GameObject.Find("Player"), false);

            if (!_gameplayManager.IsSpecificGameplayBattleground)
            {
                List<string> playerDeck = new List<string>();

                int deckId = _gameplayManager.PlayerDeckId;
                foreach (DeckCardData card in _dataManager.CachedDecksData.Decks.First(d => d.Id == deckId).Cards)
                {
                    for (int i = 0; i < card.Amount; i++)
                    {
#if DEV_MODE

// playerDeck.Add("Whizpar");
// playerDeck.Add("Nail Bomb");
#endif

                        playerDeck.Add(card.CardName);
                    }
                }

                if (_matchManager.MatchType == Enumerators.MatchType.PVP)
                {
                    _gameplayManager.CurrentPlayer.SetDeck(playerDeck, !GameClient.Get<IPvPManager>().IsCurrentPlayer());
                }
                else
                {
                    _gameplayManager.CurrentPlayer.SetDeck(playerDeck, false);
                }
            }

            _gameplayManager.CurrentPlayer.TurnStarted += OnTurnStartedStartedHandler;
            _gameplayManager.CurrentPlayer.TurnEnded += OnTurnEndedEndedHandler;
        }

        public void SetHand()
        {
            _gameplayManager.CurrentPlayer.SetFirstHand(_gameplayManager.IsTutorial || _gameplayManager.IsSpecificGameplayBattleground);

            _timerManager.AddTimer(UpdateCardDistribution);

            _battlegroundController.UpdatePositionOfCardsInPlayerHand();
        }

        private void UpdateCardDistribution(object[] param)
        {
            _cardsController.UpdatePositionOfCardsForDistribution(_gameplayManager.CurrentPlayer);
        }

        public virtual void GameStartedHandler()
        {
        }

        public virtual void GameEndedHandler(Enumerators.EndGameType endGameType)
        {
            IsActive = false;
            IsPlayerStunned = false;
            IsCardSelected = false;
        }

        public void HideCardPreview()
        {
            StopHandTimer();
            _battlegroundController.DestroyCardPreview();

            _delayTimerOfClick = 0f;
            _startedOnClickDelay = false;
            _topmostBoardCard = null;
            _selectedBoardUnitView = null;
        }

        public void HandCardPreview(object[] param)
        {
            Vector3 cardPosition;

            if (!InternalTools.IsTabletScreen())
            {
                cardPosition = new Vector3(-9f, -3f, 0f);
            }
            else
            {
                cardPosition = new Vector3(-6f, -2.5f, 0f);
            }

            _battlegroundController.CreateCardPreview(param[0], cardPosition, false);
        }

        public void OnTurnEndedEndedHandler()
        {
        }

        public void OnTurnStartedStartedHandler()
        {
        }

        public void UpdateHandCardsHighlight()
        {
            if (_gameplayManager.CurrentTurnPlayer.Equals(_gameplayManager.CurrentPlayer))
            {
                foreach (BoardCard card in _battlegroundController.PlayerHandCards)
                {
                    if (card.CanBeBuyed(_gameplayManager.CurrentPlayer))
                    {
                        card.SetHighlightingEnabled(true);
                    }
                    else
                    {
                        card.SetHighlightingEnabled(false);
                    }
                }
            }
        }

        private void HandleInput()
        {
            if (_boardArrowController.IsBoardArrowNowInTheBattle || !_gameplayManager.CanDoDragActions || _gameplayManager.IsGameplayInputBlocked)
                return;

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
                List<GameObject> hitCards = new List<GameObject>();
                bool hitHandCard = false;
                bool hitBoardCard = false;
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject != null &&
                        _battlegroundController.GetBoardCardFromHisObject(hit.collider.gameObject) != null)
                    {
                        hitCards.Add(hit.collider.gameObject);
                        hitHandCard = true;
                    }
                }

                if (!hitHandCard)
                {
                    foreach (RaycastHit2D hit in hits)
                    {
                        if (hit.collider != null && hit.collider.name.Contains("BoardCreature"))
                        {
                            hitCards.Add(hit.collider.gameObject);
                            hitBoardCard = true;
                        }
                    }
                }

                if (hitHandCard)
                {
                    if (hitCards.Count > 0)
                    {
                        hitCards = hitCards.OrderBy(x => x.GetComponent<SortingGroup>().sortingOrder).ToList();

                        BoardCard topmostBoardCard =
                            _battlegroundController.GetBoardCardFromHisObject(hitCards[hitCards.Count - 1]);
                        if (topmostBoardCard != null && !topmostBoardCard.IsPreview)
                        {
                            float delta = Application.isMobilePlatform ?
                                Constants.PointerMinDragDelta * 2f :
                                Constants.PointerMinDragDeltaMobile;

                            _startedOnClickDelay = true;
                            _isPreviewHandCard = true;
                            _topmostBoardCard = topmostBoardCard;

                            _pointerEventSolver.PushPointer(delta);
                        }
                    }
                }
                else if (hitBoardCard)
                {
                    if (hitCards.Count > 0)
                    {
                        StopHandTimer();

                        hitCards = hitCards.OrderBy(x => x.GetComponent<SortingGroup>().sortingOrder).ToList();
                        BoardUnitView selectedBoardUnitView =
                            _battlegroundController.GetBoardUnitFromHisObject(hitCards[hitCards.Count - 1]);
                        if (selectedBoardUnitView != null && (!_battlegroundController.IsPreviewActive ||
                            selectedBoardUnitView.Model.Card.Id != _battlegroundController.CurrentPreviewedCardId))
                        {
                            float delta = Application.isMobilePlatform ?
                                Constants.PointerMinDragDelta * 2f :
                                Constants.PointerMinDragDeltaMobile;
                            _pointerEventSolver.PushPointer(delta);

                            _startedOnClickDelay = true;
                            _isPreviewHandCard = false;
                            _selectedBoardUnitView = selectedBoardUnitView;
                        }
                    }
                }
                else
                {
                    if (_battlegroundController.IsPreviewActive)
                    {
                        HideCardPreview();
                    }
                    else
                    {
                        _timerManager.StopTimer(SetStatusZoomingFalse);
                        _cardsZooming = true;
                        _timerManager.AddTimer(SetStatusZoomingFalse);

                        _battlegroundController.CardsZoomed = false;
                        _battlegroundController.UpdatePositionOfCardsInPlayerHand();
                    }
                }
            }

            if (_startedOnClickDelay)
            {
                _delayTimerOfClick += Time.deltaTime;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _pointerEventSolver.PopPointer();
            }

            if (_boardArrowController.CurrentBoardArrow != null && _boardArrowController.CurrentBoardArrow.IsDragging())
            {
                _battlegroundController.DestroyCardPreview();
            }
        }

        private void PointerSolverDragStartedHandler()
        {
            _topmostBoardCard?.HandBoardCard?.OnSelected();
            if (_tutorialManager.IsTutorial)
            {
                _tutorialManager.DeactivateSelectTarget();
            }

            if (_boardArrowController.CurrentBoardArrow == null)
            {
                HideCardPreview();
            }
        }

        private void PointerEventSolverClickedHandler()
        {
            if (_battlegroundController.CardsZoomed || _topmostBoardCard == null)
            {
                CheckCardPreviewShow();
            }
            else if (!_gameplayManager.IsTutorial)
            {
                _timerManager.StopTimer(SetStatusZoomingFalse);
                _cardsZooming = true;
                _timerManager.AddTimer(SetStatusZoomingFalse, null, .8f);

                _battlegroundController.CardsZoomed = true;
                _battlegroundController.UpdatePositionOfCardsInPlayerHand();
            }
        }

        private void PointerEventSolverEndedHandler()
        {
            _delayTimerOfClick = 0f;
            _startedOnClickDelay = false;

            _topmostBoardCard = null;
            _selectedBoardUnitView = null;
        }

        private void CheckCardPreviewShow()
        {
            if (_isPreviewHandCard)
            {
                if (_topmostBoardCard != null && !_cardsZooming)
                {
                    StopHandTimer();
                    _battlegroundController.DestroyCardPreview();

                    if (_boardArrowController.CurrentBoardArrow != null &&
                        _boardArrowController.CurrentBoardArrow is AbilityBoardArrow)
                    {
                    }
                    else
                    {
                        HandCardPreview(new object[]
                        {
                            _topmostBoardCard
                        });
                    }
                }
            }
            else
            {
                if (_selectedBoardUnitView != null && !_selectedBoardUnitView.Model.IsAttacking)
                {
                    StopHandTimer();
                    _battlegroundController.DestroyCardPreview();

                    if (_boardArrowController.CurrentBoardArrow != null &&
                        _boardArrowController.CurrentBoardArrow is AbilityBoardArrow)
                    {
                    }
                    else
                    {
                        HandCardPreview(new object[]
                        {
                            _selectedBoardUnitView
                        });
                    }
                }
            }
        }

        private void StopHandTimer()
        {
            GameClient.Get<ITimerManager>().StopTimer(HandCardPreview);
        }

        private void SetStatusZoomingFalse(object[] param)
        {
            _cardsZooming = false;
        }
    }
}
