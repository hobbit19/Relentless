// Copyright (C) 2016-2017 David Pol. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.Rendering;

using DG.Tweening;
using TMPro;

using CCGKit;
using GrandDevs.CZB;
using GrandDevs.CZB.Common;
using GrandDevs.CZB.Helpers;
using GrandDevs.Internal;

/// <summary>
/// The demo player is a subclass of the core HumanPlayer type which extends it with demo-specific
/// functionality. Most of which is straightforward updating of the user interface when receiving
/// new state from the server.
/// </summary>
public class DemoHumanPlayer : DemoPlayer
{
    public static DemoHumanPlayer Instance;

    [SerializeField]
    private GameObject creatureCardViewPrefab;

    [SerializeField]
    private GameObject spellCardViewPrefab;

    [SerializeField]
    private GameObject opponentCardPrefab;

    [SerializeField]
    private GameObject boardCreaturePrefab;

    [SerializeField]
    private GameObject spellTargetingArrowPrefab;

    [SerializeField]
    private GameObject fightTargetingArrowPrefab;

    [SerializeField]
    private GameObject opponentTargetingArrowPrefab;

    protected List<CardView> playerHandCards = new List<CardView>();
    protected List<GameObject> opponentHandCards = new List<GameObject>();
    protected List<BoardCreature> playerBoardCards = new List<BoardCreature>();
    protected List<BoardCreature> opponentBoardCards = new List<BoardCreature>();
    protected List<BoardCreature> playerGraveyardCards = new List<BoardCreature>();
    protected List<BoardCreature> opponentGraveyardCards = new List<BoardCreature>();

    protected BoardCreature currentCreature;
    protected CardView currentSpellCard;

    public GameUI gameUI;
    //protected PopupChat chatPopup;

    protected float accTime;
    protected float secsAccTime;

    public override List<BoardCreature> opponentBoardCardsList
    {
        get { return opponentBoardCards; }
    }

    public override List<BoardCreature> playerBoardCardsList
    {
        get { return playerBoardCards; }
    }

    private int _graveyardCardsCount = 0;

    public int GraveyardCardsCount
    {
        get { return _graveyardCardsCount; }
        set
        {
            _graveyardCardsCount = value;
            GameClient.Get<IPlayerManager>().UpdatePlayerGraveyard(_graveyardCardsCount);
        }
    }

    private int _opponentGraveyardCardsCount = 0;

    public int OpponentGraveyardCardsCount
    {
        get { return _opponentGraveyardCardsCount; }
        set
        {
            _opponentGraveyardCardsCount = value;
            GameClient.Get<IPlayerManager>().UpdateOpponentGraveyard(_opponentGraveyardCardsCount);
        }
    }

    public Stat lifeStat { get; protected set; }
    public Stat manaStat { get; protected set; }

    protected Stat opponentLifeStat { get; set; }
    protected Stat opponentManaStat { get; set; }

    public bool isCardSelected;
    protected GameObject currentCardPreview;
    protected bool isPreviewActive;
    protected int currentPreviewedCardId;
    protected Coroutine createPreviewCoroutine;

    protected AbilitiesController _abilitiesController;

    public Player opponent;

    private IUIManager _uiManager;
    private ISoundManager _soundManager;
    private ITimerManager _timerManager;
    private ParticlesController _particlesController;

    private bool _rearrangingTopBoard = false,
                 _rearrangingBottomBoard = false;

    private bool _battleDynamic = false;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        Assert.IsNotNull(creatureCardViewPrefab);
        Assert.IsNotNull(spellCardViewPrefab);
        Assert.IsNotNull(opponentCardPrefab);
        Assert.IsNotNull(boardCreaturePrefab);
        Assert.IsNotNull(spellTargetingArrowPrefab);
        Assert.IsNotNull(fightTargetingArrowPrefab);
        //Assert.IsNotNull(opponentTargetingArrowPrefab);

        opponentTargetingArrowPrefab = Resources.Load<GameObject>("Prefabs/Gameplay/OpponentTargetingArrow");

        isHuman = true;
    }

    protected override void Start()
    {
        base.Start();

        //playerInfo.numTurn

        //chatPopup = GameObject.Find("PopupChat").GetComponent<PopupChat>();

        GameClient.Get<IPlayerManager>().PlayerGraveyardCards = playerGraveyardCards;
        GameClient.Get<IPlayerManager>().OpponentGraveyardCards = opponentGraveyardCards;


        _abilitiesController = GameClient.Get<IGameplayManager>().GetController<AbilitiesController>();
        _particlesController = GameClient.Get<IGameplayManager>().GetController<ParticlesController>();


        _uiManager = GameClient.Get<IUIManager>();
        _soundManager = GameClient.Get<ISoundManager>();
        _timerManager = GameClient.Get<ITimerManager>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        _rearrangingTopBoard = false;
        _rearrangingBottomBoard = false;

        GraveyardCardsCount = 0;
        OpponentGraveyardCardsCount = 0;

        gameUI = GameObject.Find("GameUI").GetComponent<GameUI>();
        Assert.IsNotNull(gameUI);

        foreach (var entry in playerInfo.stats)
        {
            if (entry.Value.name == "Life")
            {
                lifeStat = entry.Value;
            }
            else if (entry.Value.name == "Mana")
            {
                manaStat = entry.Value;
            }
        }
        foreach (var entry in opponentInfo.stats)
        {
            if (entry.Value.name == "Life")
            {
                opponentLifeStat = entry.Value;
            }
            else if (entry.Value.name == "Mana")
            {
                opponentManaStat = entry.Value;
            }
        }

        lifeStat.onValueChanged += (oldValue, newValue) =>
        {
            CheckGameDynamic();
            gameUI.SetPlayerHealth(lifeStat.effectiveValue);
        };
        manaStat.onValueChanged += (oldValue, newValue) =>
        {
            gameUI.SetPlayerMana(manaStat.maxValue, manaStat.effectiveValue);
            UpdateHandCardsHighlight();
        };

        opponentLifeStat.onValueChanged += (oldValue, newValue) =>
        {
            CheckGameDynamic();
            gameUI.SetOpponentHealth(opponentLifeStat.effectiveValue);
        };
        opponentManaStat.onValueChanged += (oldValue, newValue) =>
        {
            gameUI.SetOpponentMana(opponentManaStat.maxValue, opponentManaStat.effectiveValue);
        };

        deckZone = playerInfo.namedZones["Deck"];
        deckZone.onZoneChanged += numCards =>
        {
            gameUI.SetPlayerDeckCards(numCards);
        };

        handZone = playerInfo.namedZones["Hand"];
        handZone.onZoneChanged += numCards =>
        {
            gameUI.SetPlayerHandCards(numCards);
        };
        handZone.onCardAdded += card =>
        {
            //Debug.Log("%%%%%" + CurrentTurn);
            AddCardToHand(card);
            RearrangeHand();
        };
        handZone.onCardRemoved += card =>
        {
            var handCard = playerHandCards.Find(x => x.card == card);
            if (handCard != null)
            {
                playerHandCards.Remove(handCard);
                RearrangeHand();
            }
        };

        boardZone = playerInfo.namedZones["Board"];
        boardZone.onCardRemoved += card =>
        {
            var graveyardPos = GameObject.Find("GraveyardPlayer").transform.position + new Vector3(0.0f, -0.2f, 0.0f);
            var boardCard = playerBoardCards.Find(x => x.card == card);
            if (boardCard != null)
            {
              /*  if (!gameEnded)
                {
                    GameClient.Get<ITimerManager>().AddTimer((x) =>
                    {
                        var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(card.cardId);
                        GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARDS, 
                            libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_DEATH, Constants.ZOMBIES_SOUND_VOLUME, Enumerators.CardSoundType.DEATH);

                    }, null, Constants.DELAY_TO_PLAY_DEATH_SOUND_OF_CREATURE);
                } */

                boardCard.transform.localPosition = new Vector3(boardCard.transform.localPosition.x, boardCard.transform.localPosition.y, -0.2f);

                playerGraveyardCards.Add(boardCard);
    //            GameClient.Get<ITimerManager>().AddTimer((x) =>
    //            {
    //                RearrangeBottomBoard();
				//}, null, 2f);

             //   playerBoardCards.Remove(boardCard); //-------------------------------

                //boardCard.transform.DOMove(graveyardPos, 0.7f);
                boardCard.SetHighlightingEnabled(false);
                boardCard.StopSleepingParticles();
                boardCard.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                //boardCard.GetComponent<SortingGroup>().sortingOrder = playerGraveyardCards.Count;
                Destroy(boardCard.GetComponent<BoxCollider2D>());
            }
            else if (currentSpellCard != null && card == currentSpellCard.card)
            {                                           
                currentSpellCard.SetHighlightingEnabled(false);
                currentSpellCard.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                //currentSpellCard.GetComponent<SortingGroup>().sortingOrder = playerGraveyardCards.Count;
                Destroy(currentSpellCard.GetComponent<BoxCollider2D>());
                //currentSpellCard.transform.DOMove(graveyardPos - Vector3.right * 5, 0.5f);
                //currentSpellCard.transform.DOScale(new Vector2(0.6f, 0.6f), 0.5f);
                currentSpellCard.GetComponent<HandCard>().enabled = false;
                currentSpellCard = null;
            }
        };

        graveyardZone = playerInfo.namedZones["Graveyard"];
        graveyardZone.onZoneChanged += numCards =>
        {
            gameUI.SetPlayerGraveyardCards(numCards);
        };

        opponentDeckZone = opponentInfo.namedZones["Deck"];
        opponentDeckZone.onZoneChanged += numCards =>
        {
            gameUI.SetOpponentDeckCards(numCards);
        };

        opponentHandZone = opponentInfo.namedZones["Hand"];
        opponentHandZone.onZoneChanged += numCards =>
        {
            gameUI.SetOpponentHandCards(numCards);
        };
        opponentHandZone.onCardRemoved += card =>
        {
            var randomIndex = UnityEngine.Random.Range(0, opponentHandCards.Count);
            if (randomIndex < opponentHandCards.Count)
            {
                var randomCard = opponentHandCards[randomIndex];
                opponentHandCards.Remove(randomCard);
                Destroy(randomCard);
                RearrangeOpponentHand(true);
            }
        };

        opponentBoardZone = opponentInfo.namedZones["Board"];
        opponentBoardZone.onCardRemoved += card =>
        {
            var graveyardPos = GameObject.Find("GraveyardOpponent").transform.position + new Vector3(0.0f, -0.2f, 0.0f);
            var boardCard = opponentBoardCards.Find(x => x.card == card);
            if (boardCard != null)
            {
               /* if (!gameEnded)
                {

                    GameClient.Get<ITimerManager>().AddTimer((x) =>
                    {
                        var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(card.cardId);
                        GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARDS, libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_DEATH, Constants.ZOMBIES_SOUND_VOLUME, false, true);

                    }, null, Constants.DELAY_TO_PLAY_DEATH_SOUND_OF_CREATURE);
                } */

            //    GameClient.Get<ITimerManager>().AddTimer((param) =>
            //    {
                    boardCard.transform.localPosition = new Vector3(boardCard.transform.localPosition.x, boardCard.transform.localPosition.y, -0.2f);

                    opponentGraveyardCards.Add(boardCard);

                    //GameClient.Get<ITimerManager>().AddTimer((x) =>
                    //{
                    //    RearrangeTopBoard();
                    //}, null, 2f);
               //     opponentBoardCards.Remove(boardCard);
                    //boardCard.transform.DOMove(graveyardPos, 0.7f);
                    boardCard.SetHighlightingEnabled(false);
                    boardCard.StopSleepingParticles();
                    boardCard.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                    //boardCard.GetComponent<SortingGroup>().sortingOrder = opponentGraveyardCards.Count;
                    Destroy(boardCard.GetComponent<BoxCollider2D>());

             //   }, null, 3f, false);
            }
            else if (currentSpellCard != null && card == currentSpellCard.card)
            {
                currentSpellCard.SetHighlightingEnabled(false);
                currentSpellCard.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                //currentSpellCard.GetComponent<SortingGroup>().sortingOrder = opponentGraveyardCards.Count;
                Destroy(currentSpellCard.GetComponent<BoxCollider2D>());
                var sequence = DOTween.Sequence();
                sequence.PrependInterval(2.0f);
                sequence.Append(currentSpellCard.transform.DOMove(graveyardPos, 0.5f));
                sequence.Append(currentSpellCard.transform.DOScale(new Vector2(0.6f, 0.6f), 0.5f));
                sequence.OnComplete(() =>
                {
                    currentSpellCard = null;
                });
            }
        };

        opponentGraveyardZone = opponentInfo.namedZones["Graveyard"];
        opponentGraveyardZone.onZoneChanged += numCards =>
        {
            gameUI.SetOpponentGraveyardCards(numCards);
        };
    }

    private void CheckGameDynamic()
    {
        if (opponentLifeStat.effectiveValue > 9 && lifeStat.effectiveValue > 9)
        {
            if (_battleDynamic)
                _soundManager.CrossfaidSound(Enumerators.SoundType.BACKGROUND, null, true);
            _battleDynamic = false;
        }
        else
        {
            if(!_battleDynamic)
                _soundManager.CrossfaidSound(Enumerators.SoundType.BATTLEGROUND, null, true);
            _battleDynamic = true;
        }
    }

    public override void OnStartGame(StartGameMessage msg)
    {
        base.OnStartGame(msg);

        GameObject.Find("Player/Avatar").GetComponent<PlayerAvatar>().playerInfo = playerInfo;
        GameObject.Find("Opponent/Avatar").GetComponent<PlayerAvatar>().playerInfo = opponentInfo;

        for (var i = 0; i < opponentHandZone.numCards; i++)
        {
            AddCardToOpponentHand();
        }

        RearrangeOpponentHand();

        // Update the UI as appropriate.
        gameUI.SetPlayerHealth(lifeStat.effectiveValue);
        gameUI.SetOpponentHealth(opponentLifeStat.effectiveValue);
        gameUI.SetPlayerMana(manaStat.maxValue, manaStat.effectiveValue);
        gameUI.SetOpponentMana(opponentManaStat.maxValue, opponentManaStat.effectiveValue);

        gameUI.SetPlayerHandCards(handZone.cards.Count);
        gameUI.SetPlayerGraveyardCards(graveyardZone.numCards);
        gameUI.SetPlayerDeckCards(deckZone.numCards);
        gameUI.SetOpponentHandCards(opponentHandZone.numCards);
        gameUI.SetOpponentGraveyardCards(opponentGraveyardZone.numCards);
        gameUI.SetOpponentDeckCards(opponentDeckZone.numCards);

        // Set the player nicknames in the UI.
        for (var i = 0; i < msg.nicknames.Length; i++)
        {
            var nickname = msg.nicknames[i];
            if (i == msg.playerIndex)
            {
                gameUI.SetPlayerName(nickname);
            }
            else
            {
                gameUI.SetOpponentName(nickname);
            }
        }

        var gameScene = GameObject.Find("GameScene");
        if (gameScene != null)
        {
#if USING_MASTER_SERVER_KIT
            if (gameScene.GetComponent<MSK_GameScene>() != null)
            {
                gameScene.GetComponent<MSK_GameScene>().CloseWaitingWindow();
            }
#else
            //if (gameScene.GetComponent<GameScene>() != null)
            //{
            //    gameScene.GetComponent<GameScene>().CloseWaitingWindow();
            //}
            _uiManager.HidePopup<PreparingForBattlePopup>();
#endif
        }

        var endTurnButton = GameObject.Find("EndTurnButton");
        if (endTurnButton != null)
        {
            endTurnButton.GetComponent<EndTurnButton>().player = this;
        }

        GameClient.Get<IPlayerManager>().OnLocalPlayerSetUp();

        boardSkill = GameObject.Find("Player/Spell").GetComponent<BoardSkill>();
        boardSkill.ownerPlayer = this;
        var heroId = GameClient.Get<IDataManager>().CachedDecksData.decks[GameClient.Get<IGameplayManager>().PlayerDeckId].heroId;
        boardSkill.SetSkill(GameClient.Get<IDataManager>().CachedHeroesData.Heroes[heroId]);

        if (!opponentInfo.isHuman)
        {
            opponent = DemoAIPlayer.Instance;
            UpdateOpponentInfo();
        }

        EffectSolver.EffectActivateEvent += EffectActivateEventHandler;
    }


    private void UpdateOpponentInfo()
    {
        //   opponent.playerInfo = opponentInfo;
        //     opponent.opponentInfo = playerInfo;
        opponent.opponentBoardZone = boardZone;
        opponent.opponentHandZone = handZone;
        opponent.boardZone = opponentBoardZone;
        opponent.handZone = opponentHandZone;
        opponent.playerBoardCardsList = opponentBoardCardsList;
        opponent.opponentBoardCardsList = playerBoardCardsList;
        //opponent.EffectSolver = new EffectSolver(gameState, System.Environment.TickCount);
        //opponent.EffectSolver.SetTriggers(opponentInfo);
        //opponent.EffectSolver.SetTriggers(playerInfo);
    }

    public override void OnStartTurn(StartTurnMessage msg)
    {
        base.OnStartTurn(msg);

        if (GameClient.Get<IDataManager>().CachedUserLocalData.tutorial && !GameClient.Get<ITutorialManager>().IsTutorial)
            GameClient.Get<ITutorialManager>().StartTutorial();

        gameUI.SetPlayerActive(msg.isRecipientTheActivePlayer);
        gameUI.SetOpponentActive(!msg.isRecipientTheActivePlayer);
        gameUI.SetEndTurnButtonEnabled(msg.isRecipientTheActivePlayer);

        foreach (var card in opponentHandCards)
        {
            Destroy(card);
        }
        opponentHandCards.Clear();
        for (var i = 0; i < opponentHandZone.numCards; i++)
        {
            if (i == opponentHandZone.numCards - 1)
                RearrangeOpponentHand();

            AddCardToOpponentHand();
        }
        RearrangeOpponentHand(!msg.isRecipientTheActivePlayer, true);

        opponent.isActivePlayer = !msg.isRecipientTheActivePlayer;

        if (msg.isRecipientTheActivePlayer)
        {
            UpdateHandCardsHighlight();

            List<BoardCreature> creatures = new List<BoardCreature>();

            foreach (var card in playerBoardCards)
            {
                if (!card || !card.gameObject)
                {
                    creatures.Add(card);
                    continue;
                }

                card.OnStartTurn();
            }

            foreach (var item in creatures)
                playerBoardCards.Remove(item);
            creatures.Clear();
            creatures = null;

            if (CurrentBoardWeapon != null && !isPlayerStunned)
            {
                AlreadyAttackedInThisTurn = false;
                CurrentBoardWeapon.ActivateWeapon(false);
            }

            boardSkill.OnStartTurn();

            //var scene = GameObject.Find("GameScene").GetComponent<GameScene>();
            //scene.OpenPopup<PopupTurnStart>("PopupTurnStart", null, false);
            _uiManager.DrawPopup<YourTurnPopup>();

            gameUI.StartTurnCountdown(turnDuration);
        }
        else
        {
            foreach (var card in opponentBoardCards)
            {
                card.OnStartTurn();
            }

            foreach (var card in playerHandCards)
            {
                card.SetHighlightingEnabled(false);
            }
            foreach (var card in playerBoardCards)
            {
                card.SetHighlightingEnabled(false);
            }

            gameUI.HideTurnCountdown();
        }

        

        if (opponent != null)
        {
            UpdateOpponentInfo();
            opponent.CallOnStartTurnEvent();
        }

        CallOnStartTurnEvent();
    }

    public virtual void RearrangeHand(bool isMove = false)
    {
        
        var handWidth = 0.0f;
        var spacing = -1.5f; // -1
        foreach (var card in playerHandCards)
        {
            handWidth += spacing;
        }
        handWidth -= spacing;

        var pivot = new Vector3(6f, -7.5f, 0f); //1.115f, -8.05f, 0f
        var twistPerCard = -5;

		if (playerHandCards.Count == 1)
		{
			twistPerCard = 0;
		}

        var totalTwist = twistPerCard * playerHandCards.Count;
        float startTwist = ((totalTwist - twistPerCard) / 2f);
        var scalingFactor = 0.04f;
        Vector3 moveToPosition = Vector3.zero;
        for (var i = 0; i < playerHandCards.Count; i++)
        {
            var card = playerHandCards[i];
            var twist = startTwist - (i * twistPerCard);
            var nudge = Mathf.Abs(twist);
            nudge *= scalingFactor;
            moveToPosition = new Vector3(pivot.x - handWidth / 2, pivot.y - nudge, (playerHandCards.Count - i) * 0.1f);

            if (isMove)
                card.isNewCard = false;

            card.RearrangeHand(moveToPosition, Vector3.forward * twist);

            pivot.x += handWidth / playerHandCards.Count;
            card.GetComponent<SortingGroup>().sortingLayerName = "HandCards";
            card.GetComponent<SortingGroup>().sortingOrder = i;
        }
    }

    public virtual void RearrangeOpponentHand(bool isMove = false ,bool isNewCard = false)
    {
        var handWidth = 0.0f;
        var spacing = -1.0f;
        foreach (var card in opponentHandCards)
        {
            handWidth += spacing;
        }
        handWidth -= spacing;

        var pivot = new Vector3(-3.2f, 8.5f, 0f);
		var twistPerCard = 5;

		if (opponentHandCards.Count == 1)
		{
			twistPerCard = 0;
		}

		var totalTwist = twistPerCard * opponentHandCards.Count;
		float startTwist = ((totalTwist - twistPerCard) / 2f);
		var scalingFactor = 0.04f;
		Vector3 movePosition = Vector3.zero;
		Vector3 rotatePosition = Vector3.zero;
		for (var i = 0; i < opponentHandCards.Count; i++)
		{
			var card = opponentHandCards[i];
			var twist = startTwist - (i * twistPerCard);
			var nudge = Mathf.Abs(twist);
			nudge *= scalingFactor;
			movePosition = new Vector2(pivot.x - handWidth / 2, pivot.y);
			rotatePosition = new Vector3(0, 0, twist); // added multiplier, was: 0,0, twist

			if (isMove)
			{
				if (i == opponentHandCards.Count - 1 && isNewCard)
				{
					card.transform.position = new Vector3(-8.2f, 5.7f, 0); // OPPONENT DECK START POINT
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
			pivot.x += handWidth / opponentHandCards.Count;
			card.GetComponent<SortingGroup>().sortingOrder = i;
		}
    }

    public virtual void RearrangeTopBoard(Action onComplete = null)
    {
       
    }

    public virtual void RearrangeBottomBoard(Action onComplete = null)
    {
       
    }

    public override void OnEndTurn(EndTurnMessage msg)
    {
        base.OnEndTurn(msg);

        if (msg.isRecipientTheActivePlayer)
        {
            gameUI.SetEndTurnButtonEnabled(false);

            foreach (var card in playerBoardCards)
            {
                card.OnEndTurn();
            }

            GameObject.Find("Player/Spell").GetComponent<BoardSkill>().OnEndTurn();

            if (currentCreature != null)
            {
                playerBoardCards.Remove(currentCreature);
                RearrangeBottomBoard();

                playerInfo.namedZones["Hand"].AddCard(currentCreature.card);
                playerInfo.namedZones["Board"].RemoveCard(currentCreature.card);

                Destroy(currentCreature.gameObject);
                currentCreature = null;
            }

            if (currentSpellCard != null)
            {
                Destroy(currentSpellCard.GetComponent<BoardSpell>());
                currentSpellCard = null;
                RearrangeHand();
            }
        }
        else
        {
            foreach (var card in opponentBoardCards)
            {
                card.OnEndTurn();
            }
        }

        if (isHuman)
            CallOnEndTurnEvent();

        if (opponent != null)
            opponent.CallOnEndTurnEvent();
    }

    public override void StopTurn()
    {
        GameClient.Get<ITutorialManager>().ReportAction(Enumerators.TutorialReportAction.END_TURN);
		var msg = new StopTurnMessage();
        client.Send(NetworkProtocol.StopTurn, msg);
    }

    protected virtual void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (!gameStarted)
        {
            return;
        }

        //if (chatPopup.isVisible)
        //{
        //    return;
        //}

        if (GameClient.Get<ITutorialManager>().IsTutorial && (GameClient.Get<ITutorialManager>().CurrentStep != 8 && 
                                                              GameClient.Get<ITutorialManager>().CurrentStep != 17 &&
                                                              GameClient.Get<ITutorialManager>().CurrentStep != 19 &&
                                                              GameClient.Get<ITutorialManager>().CurrentStep != 27))
            return;


        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            if (isActivePlayer && currentSpellCard == null)
            {
                var hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
                var hitCards = new List<GameObject>();
                foreach (var hit in hits)
                {
                    if (hit.collider != null &&
                        hit.collider.gameObject != null &&
                        hit.collider.gameObject.GetComponent<CardView>() != null &&
                        !hit.collider.gameObject.GetComponent<CardView>().isPreview &&
                        hit.collider.gameObject.GetComponent<CardView>().CanBePlayed(this))
                    {
                        hitCards.Add(hit.collider.gameObject);
                    }
                }
                if (hitCards.Count > 0)
                {
                    DestroyCardPreview();
                    hitCards = hitCards.OrderByDescending(x => x.transform.position.z).ToList();
                    var topmostCardView = hitCards[hitCards.Count - 1].GetComponent<CardView>();
                    var topmostHandCard = topmostCardView.GetComponent<HandCard>();
                    if (topmostHandCard != null)
                    {
                        topmostCardView.GetComponent<HandCard>().OnSelected();
                        if (GameClient.Get<ITutorialManager>().IsTutorial)
                        {
                            GameClient.Get<ITutorialManager>().DeactivateSelectTarget();
                        }
                    }
                }
            }
        }
        else if (!isCardSelected)
        {
            var hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
            var hitCards = new List<GameObject>();
            var hitHandCard = false;
            var hitBoardCard = false;
            foreach (var hit in hits)
            {
                if (hit.collider != null &&
                    hit.collider.gameObject != null &&
                    hit.collider.gameObject.GetComponent<CardView>() != null)
                {
                    hitCards.Add(hit.collider.gameObject);
                    hitHandCard = true;
                }
            }
            if (!hitHandCard)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider != null &&
                        hit.collider.gameObject != null &&
                        hit.collider.gameObject.GetComponent<BoardCreature>() != null)
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
                    var topmostCardView = hitCards[hitCards.Count - 1].GetComponent<CardView>();
                    if (!topmostCardView.isPreview)
                    {
                        if (!isPreviewActive || topmostCardView.card.instanceId != currentPreviewedCardId)
                        {
                            DestroyCardPreview();
                            CreateCardPreview(topmostCardView.card, topmostCardView.transform.position, isActivePlayer);
                        }
                    }
                }
            }
            else if (hitBoardCard)
            {
                if (hitCards.Count > 0)
                {
                    hitCards = hitCards.OrderBy(x => x.GetComponent<SortingGroup>().sortingOrder).ToList();
                    var selectedBoardCreature = hitCards[hitCards.Count - 1].GetComponent<BoardCreature>();
                    if (!isPreviewActive || selectedBoardCreature.card.instanceId != currentPreviewedCardId)
                    {
                        DestroyCardPreview();
                        CreateCardPreview(selectedBoardCreature.card, selectedBoardCreature.transform.position, false);
                    }
                }
            }
            else
            {
                DestroyCardPreview();
            }
        }
    }


    public virtual GameObject AddCardToHand(RuntimeCard card)
    {
        var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(card.cardId);

        string cardSetName = string.Empty;
        foreach (var cardSet in GameClient.Get<IDataManager>().CachedCardsLibraryData.sets)
        {
            if (cardSet.cards.IndexOf(libraryCard) > -1)
                cardSetName = cardSet.name;
        }

        GameObject go = null;
        if ((Enumerators.CardKind)libraryCard.cardKind == Enumerators.CardKind.CREATURE)
        {
            go = MonoBehaviour.Instantiate(creatureCardViewPrefab as GameObject);
        }
        else if ((Enumerators.CardKind)libraryCard.cardKind == Enumerators.CardKind.SPELL)
        {
            go = MonoBehaviour.Instantiate(spellCardViewPrefab as GameObject);
        }

        var cardView = go.GetComponent<CardView>();
        cardView.PopulateWithInfo(card, cardSetName);

        cardView.CurrentTurn = CurrentTurn;

        if (CurrentTurn == 0)
        {
            cardView.SetDefaultAnimation(playerHandCards.Count);
            //if(playerHandCards.Count == 4)
            //    RearrangeHand();
        }

        var handCard = go.AddComponent<HandCard>();
        handCard.ownerPlayer = this;
        handCard.boardZone = GameObject.Find("PlayerBoard");

        cardView.transform.localScale = Vector3.one * .3f;
        playerHandCards.Add(cardView);

        //go.GetComponent<SortingGroup>().sortingOrder = playerHandCards.Count;

        return go;
    }

    public virtual GameObject AddCardToOpponentHand()
    {
        var go = Instantiate(opponentCardPrefab as GameObject);
        opponentHandCards.Add(go);
        go.GetComponent<SortingGroup>().sortingOrder = opponentHandCards.Count;

        return go;
    }

    

    private void RemoveCard(object[] param)
    {
        GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARD_BATTLEGROUND_TO_TRASH, Constants.CARDS_MOVE_SOUND_VOLUME, false, false);

        CardView card = param[0] as CardView;
        //BoardCreature currentCreature = null;
        //if (param.Length > 1)
        //    currentCreature = param[1] as BoardCreature;

        var go = card.gameObject;

        //if (!go.transform.Find("BackgroundBack").gameObject.activeSelf)
        //    return;

        var sortingGroup = card.GetComponent<SortingGroup>();

        

        Sequence animationSequence3 = DOTween.Sequence();
        //animationSequence3.Append(go.transform.DORotate(new Vector3(go.transform.eulerAngles.x, 90, 90), .2f));
        animationSequence3.Append(go.transform.DORotate(new Vector3(0, 90, 90), .3f));
        //go.transform.DOScale(new Vector3(.19f, .19f, .19f), .2f);
        go.transform.DOScale(new Vector3(.195f, .195f, .195f), .2f);
        animationSequence3.OnComplete(() =>
        {
           
            go.transform.Find("Back").gameObject.SetActive(true);
            Sequence animationSequence4 = DOTween.Sequence();
            //animationSequence4.Append(go.transform.DORotate(new Vector3(40f, 180, 90f), .3f));
            animationSequence4.Append(go.transform.DORotate(new Vector3(0, 180, 0f), .45f));
            //animationSequence4.AppendInterval(2f);

            //Changing layers to all child objects to set them Behind the Graveyard Card
            sortingGroup.sortingLayerName = "Foreground";
            sortingGroup.sortingOrder = 7;

            sortingGroup.gameObject.layer = 0;

            for (int i = 0; i < sortingGroup.transform.childCount; i++)
            {
                Transform child = sortingGroup.transform.GetChild(i);
                
                if (child.name != "Back")
                {
                    child.gameObject.SetActive(false);
                }
                else
                {
                    child.gameObject.layer = 0;
                }
            }
        });

        Sequence animationSequence2 = DOTween.Sequence();
        //animationSequence2.Append(go.transform.DOMove(new Vector3(-4.1f, -1, 0), .3f));
        animationSequence2.Append(go.transform.DOMove(new Vector3(-6.57f, -1, 0), 0.7f));


        animationSequence2.OnComplete(() =>
        {


            for (int i = 0; i < sortingGroup.transform.childCount; i++)
            {
                Transform child = sortingGroup.transform.GetChild(i);

                if (child.name == "Back")
                {
                    child.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                }
            }


            Sequence animationSequence5 = DOTween.Sequence();
            animationSequence5.Append(go.transform.DOMove(new Vector3(-6.57f, -4.352f, 0), .5f));
            animationSequence5.OnComplete(() => 
            {
                MonoBehaviour.Destroy(go);
            });
        });
    }

    private void PlayArrivalAnimationDelay(object[] param)
    {
        BoardCreature currentCreature = null;
        if (param != null)
        {
            currentCreature = param[0] as BoardCreature;
            currentCreature.PlayArrivalAnimation();
        }
    }

    private void RemoveOpponentCard(object[] param)
    {
        GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARD_BATTLEGROUND_TO_TRASH, Constants.CARDS_MOVE_SOUND_VOLUME, false, false);

        GameObject go = param[0] as GameObject;
        BoardCreature currentCreature = null;
        if (param.Length > 1)
            currentCreature = param[1] as BoardCreature;

        //if (!go.transform.Find("BackgroundBack").gameObject.activeSelf)
        //    return;
        var sortingGroup = go.GetComponent<SortingGroup>();

        Sequence animationSequence3 = DOTween.Sequence();
        //animationSequence3.Append(go.transform.DORotate(new Vector3(go.transform.eulerAngles.x, 0, 90), .2f));
        animationSequence3.Append(go.transform.DORotate(new Vector3(go.transform.eulerAngles.x, 0, -30f), .4f));
        go.transform.DOScale(new Vector3(1, 1, 1), .2f);
        animationSequence3.OnComplete(() =>
        {

            //    if (go.transform.Find("BackgroundBack") != null)
            //        go.transform.Find("BackgroundBack").gameObject.SetActive(true);
            //    //Sequence animationSequence4 = DOTween.Sequence();
            //    //animationSequence4.Append(go.transform.DORotate(new Vector3(40f, 180, 90f), .3f));
            //    //animationSequence4.AppendInterval(2f);
        });

        Sequence animationSequence2 = DOTween.Sequence();
        //animationSequence2.Append(go.transform.DOMove(new Vector3(-4.85f, 6.3f, 0), .3f));
        animationSequence2.Append(go.transform.DOMove(new Vector3(6.535f, 14f, 0), .6f));

        animationSequence2.OnComplete(() =>
        {
            go.layer = 0;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                go.transform.GetChild(i).gameObject.layer = 0;
            }

            //sortingGroup.sortingLayerName = "Default";
            sortingGroup.sortingOrder = 7; // Foreground layer

            Sequence animationSequence4 = DOTween.Sequence();
            animationSequence4.Append(go.transform.DORotate(new Vector3(go.transform.eulerAngles.x, 0f, 0f), .2f));

            Sequence animationSequence5 = DOTween.Sequence();
            animationSequence5.Append(go.transform.DOMove(new Vector3(6.535f, 6.306f, 0), .5f));
            animationSequence5.OnComplete(() =>
            {
                MonoBehaviour.Destroy(go);
            });
        });
    }

    private void CallAbility(GrandDevs.CZB.Data.Card libraryCard, CardView card, RuntimeCard runtimeCard, Enumerators.CardKind kind, object boardObject, Action<CardView> action, bool isPlayer, object target = null, HandCard handCard = null)
    {
        Vector3 postionOfCardView = Vector3.zero;

        if(card != null)
            postionOfCardView = card.transform.position;

        bool canUseAbility = false;
        ActiveAbility activeAbility = null;
        foreach (var item in libraryCard.abilities) //todo improve it bcoz can have queue of abilities with targets
        {
            activeAbility = _abilitiesController.CreateActiveAbility(item, kind, boardObject, isPlayer ? this : opponent, libraryCard);
            //Debug.Log(_abilitiesController.IsAbilityCanActivateTargetAtStart(item));
            if (_abilitiesController.IsAbilityCanActivateTargetAtStart(item))
                canUseAbility = true;
            else //if (_abilitiesController.IsAbilityCanActivateWithoutTargetAtStart(item))
                activeAbility.ability.Activate();
        }
        // Preemptively move the card so that the effect solver can properly check the availability of targets
        // by also taking into account this card (that is trying to be played).

        if (kind == Enumerators.CardKind.SPELL)
        {
            if (isPlayer)
                currentSpellCard = card;
        }
        else
        {
            if (isPlayer)
            {
                playerInfo.namedZones[Constants.ZONE_HAND].RemoveCard(runtimeCard);
                playerInfo.namedZones[Constants.ZONE_BOARD].AddCard(runtimeCard);

                if(currentCreature != null)
                currentCreature._fightTargetingArrowPrefab = fightTargetingArrowPrefab;
            }
            else
            {
                //opponentInfo.namedZones[Constants.ZONE_HAND].RemoveCard(runtimeCard);
                //opponentInfo.namedZones[Constants.ZONE_BOARD].AddCard(runtimeCard);
            }
        }

        if (kind != Enumerators.CardKind.SPELL)
            effectSolver.MoveCard(isPlayer ? netId : opponentInfo.netId, runtimeCard, Constants.ZONE_HAND, Constants.ZONE_BOARD);
        else
        {
            if (handCard != null && isPlayer)
            {
                handCard.gameObject.SetActive(false);
            }
        }

        if (canUseAbility)
        {
            var ability = libraryCard.abilities.Find(x => _abilitiesController.IsAbilityCanActivateTargetAtStart(x));

            if (_abilitiesController.CheckActivateAvailability(kind, ability, isPlayer ? this : opponent))
            {
                activeAbility.ability.Activate();

                if (isPlayer)
                {
                    activeAbility.ability.ActivateSelectTarget(callback: () =>
                    {
                        if (kind == Enumerators.CardKind.SPELL && isPlayer)
                        {         
                            handCard.gameObject.SetActive(true);
                            card.removeCardParticle.Play(); // move it when card should call hide action

                            effectSolver.MoveCard(isPlayer ? netId : opponentInfo.netId, runtimeCard, Constants.ZONE_HAND, Constants.ZONE_BOARD);

                            GameClient.Get<ITimerManager>().AddTimer(RemoveCard, new object[] { card }, 0.5f, false);

                            GameClient.Get<ITimerManager>().AddTimer((creat) =>
                            {
                                GraveyardCardsCount++;
                            }, null, 1.5f);
                        }

                        action?.Invoke(card);
                    },
                    failedCallback: () =>
                    {
                        if (kind == Enumerators.CardKind.SPELL && isPlayer)
                        {
                            handCard.gameObject.SetActive(true);
                            handCard.ResetToHandAnimation();
                            handCard.CheckStatusOfHighlight();

                            isCardSelected = false;
                            currentSpellCard = null;
                            gameUI.endTurnButton.SetEnabled(true);

                            RearrangeHand(true);
                        }
                        else
                        {
                            Debug.Log("RETURN CARD TO HAND MAYBE.. SHOULD BE CASE !!!!!");
                            action?.Invoke(card);
                        }
                    });
                }
                else
                {
                    if (target is BoardCreature)
                        activeAbility.ability.targetCreature = target as BoardCreature;
                    else if (target is PlayerAvatar)
                        activeAbility.ability.targetPlayer = target as PlayerAvatar;

                    activeAbility.ability.SelectedTargetAction(true);

                    RearrangeBottomBoard();
                    //  Debug.LogError(activeAbility.ability.abilityType.ToString() + " ABIITY WAS ACTIVATED!!!! on " + (target == null ? target : target.GetType()));
                }
            }
            else
            {
                CallPermanentAbilityAction(isPlayer, action, card, target, activeAbility);
            }
        }
        else
        {
            CallPermanentAbilityAction(isPlayer, action, card, target, activeAbility);
        }
    }

    private void CallPermanentAbilityAction(bool isPlayer, Action<CardView> action, CardView card, object target, ActiveAbility activeAbility)
    {
        if (isPlayer)
            action?.Invoke(card);
        else
        {
            if (activeAbility == null)
                return;
            if (target is BoardCreature)
                activeAbility.ability.targetCreature = target as BoardCreature;
            else if (target is PlayerAvatar)
                activeAbility.ability.targetPlayer = target as PlayerAvatar;

            activeAbility.ability.SelectedTargetAction(true);
        }

        RearrangeBottomBoard();
        RearrangeTopBoard();
    }

    private void CallCardPlay(CardView card)
    {
        PlayCreatureCard(card.card);
        currentCreature = null;
        gameUI.endTurnButton.SetEnabled(true);
    }

    private void CallSpellCardPlay(CardView card)
    {
        PlaySpellCard(card.card);
        currentSpellCard = null;
        gameUI.endTurnButton.SetEnabled(true);
    }

    protected void UpdateHandCardsHighlight()
    {
        if (boardSkill != null && isActivePlayer)
        {
            if (manaStat.effectiveValue >= boardSkill.manaCost)
                boardSkill.SetHighlightingEnabled(true);
            else
                boardSkill.SetHighlightingEnabled(false);
        }

        foreach (var card in playerHandCards)
        {
            if (card.CanBePlayed(this) && card.CanBeBuyed(this))
            {
                card.SetHighlightingEnabled(true);
            }
            else
            {
                card.SetHighlightingEnabled(false);
            }
        }
    }

    public override void OnEndGame(EndGameMessage msg)
    {
        base.OnEndGame(msg);

        GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.BACKGROUND, 128, Constants.BACKGROUND_SOUND_VOLUME, null, true);

        if (msg.winnerPlayerIndex == playerInfo.netId)
            GameObject.Find("Opponent/Avatar").GetComponent<PlayerAvatar>().OnAvatarDie();
        else
            GameObject.Find("Player/Avatar").GetComponent<PlayerAvatar>().OnAvatarDie();

        GameClient.Get<ITimerManager>().AddTimer((x) =>
        {
            if (msg.winnerPlayerIndex == playerInfo.netId)
            {
                _uiManager.DrawPopup<YouWonPopup>();
            }
            else
            {
                _uiManager.DrawPopup<YouLosePopup>();
            }
        }, null, 4f);
        _soundManager.CrossfaidSound(Enumerators.SoundType.BACKGROUND, null, true);


        EffectSolver.EffectActivateEvent -= EffectActivateEventHandler;
    }

    public override void OnCardMoved(CardMovedMessage msg)
    {
        base.OnCardMoved(msg);

        var randomIndex = UnityEngine.Random.Range(0, opponentHandCards.Count);
        var randomCard = opponentHandCards[randomIndex];
        opponentHandCards.Remove(randomCard);

        GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARD_FLY_HAND_TO_BATTLEGROUND, Constants.CARDS_MOVE_SOUND_VOLUME, false, false);

        randomCard.transform.DOMove(Vector3.up * 2.5f, 0.6f).OnComplete(() => 
        {
            //GameClient.Get<ITimerManager>().AddTimer(DestroyRandomCard, new object[] { randomCard }, 1f, false);
            //randomCard.GetComponent<Animator>().SetTrigger("RemoveCard");
            randomCard.transform.Find("RemoveCardParticle").GetComponent<ParticleSystem>().Play();
            
            

            randomCard.transform.DOScale(Vector3.one * 1.2f, 0.6f).OnComplete(() =>
            {
                RemoveOpponentCard(new object[] { randomCard });

                GameClient.Get<ITimerManager>().AddTimer(OnMovedCardCompleted, new object[] { msg }, 0.1f);

                GameClient.Get<ITimerManager>().AddTimer((creat) =>
                {
                    OpponentGraveyardCardsCount++;
                }, null, 1f);

                
            });
        });
        
        randomCard.transform.DORotate(Vector3.zero, 0.5f);

        RearrangeOpponentHand(true);
        gameUI.SetOpponentHandCards(opponentHandCards.Count);
    }

    private void DestroyRandomCard(object[] param)
    {
        GameObject randomCard = param[0] as GameObject;
        Destroy(randomCard);
    }

    private void OnMovedCardCompleted(object[] param)
    {
        CardMovedMessage msg = param[0] as CardMovedMessage;

        var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(msg.card.cardId);

        string cardSetName = string.Empty;
        foreach (var cardSet in GameClient.Get<IDataManager>().CachedCardsLibraryData.sets)
        {
            if (cardSet.cards.IndexOf(libraryCard) > -1)
                cardSetName = cardSet.name;
        }

        var opponentBoard = opponentInfo.namedZones[Constants.ZONE_BOARD];
        var runtimeCard = opponentBoard.cards[opponentBoard.cards.Count - 1];

        if ((Enumerators.CardKind)libraryCard.cardKind == Enumerators.CardKind.CREATURE)
        {
            effectSolver.SetDestroyConditions(runtimeCard);
            effectSolver.SetTriggers(runtimeCard);
            var boardCreature = Instantiate(boardCreaturePrefab);
            boardCreature.tag = "OpponentOwned";
            boardCreature.GetComponent<BoardCreature>().PopulateWithInfo(runtimeCard, cardSetName);
            boardCreature.transform.parent = GameObject.Find("OpponentBoard").transform;
            opponentBoardCards.Add(boardCreature.GetComponent<BoardCreature>());

            boardCreature.transform.position += Vector3.up * 2f; // Start pos before moving cards to the opponents board
            //PlayArrivalAnimation(boardCreature, libraryCard.cardType);
            RearrangeTopBoard(() =>
            {
                opponentHandZone.numCards -= 1;
                opponentManaStat.baseValue -= libraryCard.cost;

                BoardCreature targetCreature = null;
                PlayerAvatar targetPlayerAvatar = null;
                object target = null;

                if (msg.targetInfo != null && msg.targetInfo.Length > 0)
                {
                    var playerCard = opponentInfo.namedZones[Constants.ZONE_BOARD].cards.Find(x => x.instanceId == msg.targetInfo[0]);
                    var opponentCard = playerInfo.namedZones[Constants.ZONE_BOARD].cards.Find(x => x.instanceId == msg.targetInfo[0]);

                    if (opponentCard != null)
                        targetCreature = playerBoardCards.Find(x => x.card.instanceId == opponentCard.instanceId);
                    else if (playerCard != null)
                        targetCreature = opponentBoardCards.Find(x => x.card.instanceId == playerCard.instanceId);
                    else
                    {
                        var playerAvatar = GameObject.Find("PlayerAvatar").GetComponent<PlayerAvatar>();
                        var opponentAvatar = GameObject.Find("OpponentAvatar").GetComponent<PlayerAvatar>();

                        if (playerAvatar.playerInfo.id == msg.targetInfo[0])
                            targetPlayerAvatar = playerAvatar;
                        else if (opponentAvatar.playerInfo.id == msg.targetInfo[0])
                            targetPlayerAvatar = opponentAvatar;
                    }
                }


                bool createTargetArrow = false;

                if(libraryCard.abilities != null && libraryCard.abilities.Count > 0)
                    createTargetArrow = _abilitiesController.IsAbilityCanActivateTargetAtStart(libraryCard.abilities[0]);

                if (targetCreature != null)
                {
                    target = targetCreature;

                    CreateOpponentTarget(createTargetArrow, boardCreature.gameObject, targetCreature.gameObject,
                         () => { CallAbility(libraryCard, null, runtimeCard, Enumerators.CardKind.CREATURE, boardCreature.GetComponent<BoardCreature>(), null, false, target); });
                }
                else if (targetPlayerAvatar != null)
                {
                    target = targetPlayerAvatar;
                    
                    CreateOpponentTarget(createTargetArrow, boardCreature.gameObject, targetPlayerAvatar.gameObject,
                         () => { CallAbility(libraryCard, null, runtimeCard, Enumerators.CardKind.CREATURE, boardCreature.GetComponent<BoardCreature>(), null, false, target); });
                }
                else
                {
                    CallAbility(libraryCard, null, runtimeCard, Enumerators.CardKind.CREATURE, boardCreature.GetComponent<BoardCreature>(), null, false);
                }
            });


            boardCreature.GetComponent<BoardCreature>().PlayArrivalAnimation();
            //GameClient.Get<ITimerManager>().AddTimer(RemoveOpponentCard, new object[] { randomCard }, 0.1f, false);
        }
        else if ((Enumerators.CardKind)libraryCard.cardKind == Enumerators.CardKind.SPELL)
        {
            effectSolver.SetDestroyConditions(runtimeCard);
            effectSolver.SetTriggers(runtimeCard);
            var spellCard = Instantiate(spellCardViewPrefab);
            spellCard.transform.position = GameObject.Find("OpponentSpellsPivot").transform.position;
            spellCard.GetComponent<SpellCardView>().PopulateWithInfo(runtimeCard, cardSetName);
            spellCard.GetComponent<SpellCardView>().SetHighlightingEnabled(false);

            currentSpellCard = spellCard.GetComponent<SpellCardView>();

            var boardSpell = spellCard.AddComponent<BoardSpell>();

            spellCard.gameObject.SetActive(false);

            opponentManaStat.baseValue -= libraryCard.cost;

            BoardCreature targetCreature = null;
            PlayerAvatar targetPlayerAvatar = null;
            object target = null;

            var playerAvatar = GameObject.Find("PlayerAvatar").GetComponent<PlayerAvatar>();
            var opponentAvatar = GameObject.Find("OpponentAvatar").GetComponent<PlayerAvatar>();

            if (msg.targetInfo != null && msg.targetInfo.Length > 0)
            {
                var playerCard = playerInfo.namedZones[Constants.ZONE_BOARD].cards.Find(x => x.instanceId == msg.targetInfo[0]);
                var opponentCard = opponentInfo.namedZones[Constants.ZONE_BOARD].cards.Find(x => x.instanceId == msg.targetInfo[0]);

                if (playerCard != null)
                    targetCreature = playerBoardCards.Find(x => x.card.instanceId == playerCard.instanceId);
                else if (opponentCard != null)
                    targetCreature = opponentBoardCards.Find(x => x.card.instanceId == opponentCard.instanceId);
                else
                {                    
                    if (playerAvatar.playerInfo.id == msg.targetInfo[0])
                        targetPlayerAvatar = playerAvatar;
                    else if (opponentAvatar.playerInfo.id == msg.targetInfo[0])
                        targetPlayerAvatar = opponentAvatar;
                }
            }


            bool createTargetArrow = false;

            if (libraryCard.abilities != null && libraryCard.abilities.Count > 0)
                createTargetArrow = _abilitiesController.IsAbilityCanActivateTargetAtStart(libraryCard.abilities[0]);


            if (targetCreature != null)
            {
                target = targetCreature;

                CreateOpponentTarget(createTargetArrow, opponentAvatar.gameObject, targetCreature.gameObject,
                    () => { CallAbility(libraryCard, null, runtimeCard, Enumerators.CardKind.SPELL, boardSpell, null, false, target); });
            }
            else if (targetPlayerAvatar != null)
            {
                target = targetPlayerAvatar;

                CreateOpponentTarget(createTargetArrow, opponentAvatar.gameObject, targetPlayerAvatar.gameObject, 
                    () => { CallAbility(libraryCard, null, runtimeCard, Enumerators.CardKind.SPELL, boardSpell, null, false, target); });
            }
            else
            {
                CallAbility(libraryCard, null, runtimeCard, Enumerators.CardKind.SPELL, boardSpell, null, false);
            }

            //GameClient.Get<ITimerManager>().AddTimer(RemoveOpponentCard, new object[] { randomCard }, 0.1f, false);
        }
    }

    private void CreateOpponentTarget(bool createTargetArrow, GameObject startObj, GameObject targetObject, Action action)
    {
        if(!createTargetArrow)
        {
            action?.Invoke();
            return;
        }

        var targetingArrow = Instantiate(opponentTargetingArrowPrefab).GetComponent<OpponentTargetingArrow>();
        targetingArrow.opponentBoardZone = boardZone;
        targetingArrow.Begin(startObj.transform.position);

        targetingArrow.SetTarget(targetObject);

        StartCoroutine(RemoveOpponentTargetingArrow(targetingArrow, action));
    }

    private IEnumerator RemoveOpponentSpellCard(SpellCardView spellCard)
    {
        yield return new WaitForSeconds(2.0f);
    }

    private IEnumerator RemoveOpponentTargetingArrow(TargetingArrow arrow, Action action)
    {
        yield return new WaitForSeconds(1f);
        Destroy(arrow.gameObject);

        action?.Invoke();
    }

    public override void OnPlayerAttacked(PlayerAttackedMessage msg)
    {
        base.OnPlayerAttacked(msg);

        var attackingCard = opponentBoardCards.Find(x => x.card.instanceId == msg.attackingCardInstanceId);
        if (attackingCard != null)
        {
            var avatar = GameObject.Find("Player/Avatar").GetComponent<PlayerAvatar>(); ;
            CombatAnimation.PlayFightAnimation(attackingCard.gameObject, avatar.gameObject, 0.1f, () =>
            {
				PlayAttackVFX(attackingCard.card.type, avatar.transform.position, attackingCard.Damage.effectiveValue);

				effectSolver.FightPlayer(msg.attackingPlayerNetId, msg.attackingCardInstanceId);
                attackingCard.CreatureOnAttack(avatar);
            });
        }
    }

    public override void OnCreatureAttacked(CreatureAttackedMessage msg)
    {

        base.OnCreatureAttacked(msg);
        var attackingCard = opponentBoardCards.Find(x => x.card.instanceId == msg.attackingCardInstanceId);
        var attackedCard = playerBoardCards.Find(x => x.card.instanceId == msg.attackedCardInstanceId);

        if (attackingCard != null && attackedCard != null)
        {
            var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(attackingCard.card.cardId);
    //        GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARDS, libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_ATTACK, Constants.ZOMBIES_SOUND_VOLUME, false, true);


            attackingCard.transform.position = new Vector3(attackingCard.transform.position.x, attackingCard.transform.position.y, attackingCard.transform.position.z - 0.2f);

            CombatAnimation.PlayFightAnimation(attackingCard.gameObject, attackedCard.gameObject, 0.5f, () =>
            {
                GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARDS, libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_ATTACK, Constants.ZOMBIES_SOUND_VOLUME, false, true);


                PlayAttackVFX(attackingCard.card.type, attackedCard.transform.position, attackingCard.Damage.effectiveValue);

				effectSolver.FightCreature(msg.attackingPlayerNetId, attackingCard.card, attackedCard.card);
                attackingCard.CreatureOnAttack(attackedCard);

                attackingCard.transform.position = new Vector3(attackingCard.transform.position.x, attackingCard.transform.position.y, attackingCard.transform.position.z + 0.2f);
            });
        }
    }

	public void PlayAttackVFX(Enumerators.CardType type, Vector3 target, int damage)
	{
		GameObject effect;
		GameObject vfxPrefab;
        target = Utilites.CastVFXPosition(target);

        if (type == Enumerators.CardType.FERAL)
		{
			vfxPrefab = GameClient.Get<ILoadObjectsManager>().GetObjectByPath<GameObject>("Prefabs/VFX/FeralAttackVFX");
			effect = GameObject.Instantiate(vfxPrefab);
			effect.transform.position = target;
            _soundManager.PlaySound(Enumerators.SoundType.FERAL_ATTACK, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);

            _particlesController.RegisterParticleSystem(effect, true, 5f);

            if (damage > 3 && damage < 7)
			{
				GameClient.Get<ITimerManager>().AddTimer((a) =>
				{
					effect = GameObject.Instantiate(vfxPrefab);
					effect.transform.position = target;
					effect.transform.localScale = new Vector3(-1, 1, 1);
                    _particlesController.RegisterParticleSystem(effect, true, 5f);


                }, null, 0.5f, false);
			}
			if (damage > 6)
			{
				GameClient.Get<ITimerManager>().AddTimer((a) =>
				{
					effect = GameObject.Instantiate(vfxPrefab);
					effect.transform.position = target - Vector3.right;
					effect.transform.eulerAngles = Vector3.forward * 90;

                    _particlesController.RegisterParticleSystem(effect, true, 5f);

                }, null, 1.0f, false);
			}
            //GameClient.Get<ITimerManager>().AddTimer((a) =>
            //{
            //    _soundManager.PlaySound(Enumerators.SoundType.FERAL_ATTACK, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
            //}, null, 0.75f, false);
        }
        else if (type == Enumerators.CardType.HEAVY)
		{
            var soundType = Enumerators.SoundType.HEAVY_ATTACK_1;
            var prefabName = "Prefabs/VFX/HeavyAttackVFX";
            if (damage > 4)
            {
                prefabName = "Prefabs/VFX/HeavyAttack2VFX";
                soundType = Enumerators.SoundType.HEAVY_ATTACK_2;
            }
            vfxPrefab = GameClient.Get<ILoadObjectsManager>().GetObjectByPath<GameObject>(prefabName);
            effect = GameObject.Instantiate(vfxPrefab);
            effect.transform.position = target;

            _particlesController.RegisterParticleSystem(effect, true, 5f);

            _soundManager.PlaySound(soundType, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
           /* GameClient.Get<ITimerManager>().AddTimer((a) =>
                {
                }, null, 0.75f, false);*/
        }
		else
		{
			vfxPrefab = GameClient.Get<ILoadObjectsManager>().GetObjectByPath<GameObject>("Prefabs/VFX/WalkerAttackVFX");
			effect = GameObject.Instantiate(vfxPrefab);
			effect.transform.position = target;

            _particlesController.RegisterParticleSystem(effect, true, 5f);

            if (damage > 4)
			{
				GameClient.Get<ITimerManager>().AddTimer((a) =>
			   {
				   effect = GameObject.Instantiate(vfxPrefab);
				   effect.transform.position = target;

				   effect.transform.localScale = new Vector3(-1, 1, 1);
                   _particlesController.RegisterParticleSystem(effect, true, 5f);


               }, null, 0.5f, false);
              //  GameClient.Get<ITimerManager>().AddTimer((a) =>
              //  {
                    _soundManager.PlaySound(Enumerators.SoundType.WALKER_ATTACK_2, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
               // }, null, 0.75f, false);
            }
            else
            {
            //    GameClient.Get<ITimerManager>().AddTimer((a) =>
             //   {
                    _soundManager.PlaySound(Enumerators.SoundType.WALKER_ATTACK_1, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
           //     }, null, 0.75f, false);
            }
		}

	}



    public override void OnReceiveChatText(NetworkInstanceId senderNetId, string text)
    {
        //chatPopup.SendText(senderNetId, text);
    }

    public override void AddWeapon(GrandDevs.CZB.Data.Card card)
    {
        CurrentBoardWeapon = new BoardWeapon(GameObject.Find("Player").transform.Find("Weapon").gameObject, card);
    }

    public override void DestroyWeapon()
    {
        if(CurrentBoardWeapon != null)
        {
            CurrentBoardWeapon.Destroy();
        }

        CurrentBoardWeapon = null;
    }

    private void EffectActivateEventHandler(Enumerators.EffectActivateType effectActivateType, object[] param)
    {
        Debug.LogError("EffectActivateEventHandler");

        switch (effectActivateType)
        {
            case Enumerators.EffectActivateType.PLAY_SKILL_EFFECT:
                {
                    PlayerInfo player = (PlayerInfo)param[0];
                    int from = (int)param[2];
                    int to = (int)param[3];
                    int toType = (int)param[4];

                    Debug.LogError(player.id + " player.id");

                    if (player.Equals(opponent.playerInfo))
                    {
                        CreateOpponentTarget(true, GameObject.Find("Opponent/Spell"), GameObject.Find("Player/Avatar"), () =>
                        {
                            opponent.boardSkill.DoOnUpSkillAction();
                        });
                    }
                }
                break;
            default: break;
        }
    }
}