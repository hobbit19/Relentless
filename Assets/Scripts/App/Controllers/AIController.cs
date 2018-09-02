using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LoomNetwork.CZB
{
    public class AiController : IController
    {
        public BoardCard CurrentSpellCard;

        private readonly int _minTurnForAttack = 0;

        private readonly Random _random = new Random();

        private IGameplayManager _gameplayManager;

        private IDataManager _dataManager;

        private ILoadObjectsManager _loadObjectsManager;

        private ISoundManager _soundManager;

        private ITutorialManager _tutorialManager;

        private ITimerManager _timerManager;

        private BattlegroundController _battlegroundController;

        private CardsController _cardsController;

        private ActionsQueueController _actionsQueueController;

        private AbilitiesController _abilitiesController;

        private BattleController _battleController;

        private AnimationsController _animationsController;

        private VfxController _vfxController;

        private SkillsController _skillsController;

        private Enumerators.AiType _aiType;

        private List<ActionItem> _allActions;

        private List<BoardUnit> _attackedUnitTargets;

        private List<BoardUnit> _unitsToIgnoreThisTurn;

        private GameObject _fightTargetingArrowPrefab; // rewrite

        private CancellationTokenSource _aiBrainCancellationTokenSource;

        public bool IsPlayerStunned { get; set; }

        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _timerManager = GameClient.Get<ITimerManager>();

            _abilitiesController = _gameplayManager.GetController<AbilitiesController>();
            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();
            _cardsController = _gameplayManager.GetController<CardsController>();
            _actionsQueueController = _gameplayManager.GetController<ActionsQueueController>();
            _battleController = _gameplayManager.GetController<BattleController>();
            _animationsController = _gameplayManager.GetController<AnimationsController>();
            _vfxController = _gameplayManager.GetController<VfxController>();
            _skillsController = _gameplayManager.GetController<SkillsController>();

            _gameplayManager.OnGameEndedEvent += OnGameEndedEventHandler;
            _gameplayManager.OnGameStartedEvent += OnGameStartedEventHandler;
        }

        public void Dispose()
        {
        }

        public void Update()
        {
        }

        public void ResetAll()
        {
            _aiBrainCancellationTokenSource?.Cancel();
        }

        public void InitializePlayer()
        {
            _gameplayManager.OpponentPlayer = new Player(GameObject.Find("Opponent"), true);

            _fightTargetingArrowPrefab = Resources.Load<GameObject>("Prefabs/Gameplay/Arrow/AttackArrowVFX_Object");

            _attackedUnitTargets = new List<BoardUnit>();
            _unitsToIgnoreThisTurn = new List<BoardUnit>();

            List<string> playerDeck = new List<string>();

            if (_gameplayManager.IsTutorial)
            {
                playerDeck.Add("MonZoon");
                playerDeck.Add("Burrrnn");
                playerDeck.Add("Golem");
                playerDeck.Add("Rockky");
                playerDeck.Add("Rockky");
            } else
            {
                int deckId = _gameplayManager.OpponentDeckId;
                foreach (DeckCardData card in _dataManager.CachedOpponentDecksData.Decks.First(d => d.Id == deckId).Cards)
                {
                    for (int i = 0; i < card.Amount; i++)
                    {
                        playerDeck.Add(card.CardName);

                        // playerDeck.Add("Tainted Goo");
                    }
                }
            }

            _gameplayManager.OpponentPlayer.SetDeck(playerDeck);

            _battlegroundController.UpdatePositionOfCardsInOpponentHand();

            _gameplayManager.OpponentPlayer.OnStartTurnEvent += OnStartTurnEventHandler;
            _gameplayManager.OpponentPlayer.OnEndTurnEvent += OnEndTurnEventHandler;
        }

        private void SetAiTypeByDeck()
        {
            OpponentDeck deck = _dataManager.CachedOpponentDecksData.Decks.First(d => d.Id == _gameplayManager.OpponentDeckId);
            _aiType = (Enumerators.AiType)Enum.Parse(typeof(Enumerators.AiType), deck.Type);
        }

        private void FillActions()
        {
            _allActions = new List<ActionItem>();

            List<Enumerators.AiActionType> allActionsType = _dataManager.CachedOpponentDecksData.Decks.First(d => d.Id == _gameplayManager.OpponentDeckId).OpponentActions;
            _allActions = _dataManager.CachedActionsLibraryData.GetActions(allActionsType.ToArray());
        }

        private void OnGameEndedEventHandler(Enumerators.EndGameType obj)
        {
            _aiBrainCancellationTokenSource?.Cancel();
        }

        private void OnGameStartedEventHandler()
        {
            if (!_gameplayManager.IsTutorial)
            {
                // _minTurnForAttack = _random.Next(1, 3);
                FillActions();

                SetAiTypeByDeck();
            }
        }

        private async void OnStartTurnEventHandler()
        {
            if (!_gameplayManager.CurrentTurnPlayer.Equals(_gameplayManager.OpponentPlayer) || !_gameplayManager.GameStarted)
            {
                _aiBrainCancellationTokenSource?.Cancel();
                return;
            }

            _aiBrainCancellationTokenSource = new CancellationTokenSource();
            Debug.Log("brain started");

            try
            {
                await DoAiBrain(_aiBrainCancellationTokenSource.Token);
            } catch (OperationCanceledException e)
            {
                Debug.Log("brain canceled!");
            }

            Debug.Log("brain finished");
        }

        private void OnEndTurnEventHandler()
        {
            if (!_gameplayManager.CurrentTurnPlayer.Equals(_gameplayManager.OpponentPlayer))

                return;

            _aiBrainCancellationTokenSource.Cancel();

            _attackedUnitTargets.Clear();
            _unitsToIgnoreThisTurn.Clear();
        }

        private async Task DoAiBrain(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1f));
            cancellationToken.ThrowIfCancellationRequested();
            await PlayCardsFromHand(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (_tutorialManager.IsTutorial && (_tutorialManager.CurrentStep == 11))
            {
                (_tutorialManager as TutorialManager).Paused = true;
            } else
            {
                await UseUnitsOnBoard(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await UsePlayerSkills(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (_gameplayManager.OpponentPlayer.SelfHero.HeroElement == Enumerators.SetType.Fire)
                {
                    await UseUnitsOnBoard(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    _battlegroundController.StopTurn();
                } else
                {
                    _battlegroundController.StopTurn();
                }
            }
        }

        // ai step 1
        private async Task PlayCardsFromHand(CancellationToken cancellationToken)
        {
            bool wasAction = false;
            foreach (WorkingCard card in GetUnitCardsInHand())
            {
                if (_gameplayManager.OpponentPlayer.BoardCards.Count >= Constants.MaxBoardUnits)
                {
                    break;
                }

                if (CardCanBePlayable(card) && CheckSpecialCardRules(card))
                {
                    PlayCardOnBoard(card);
                    wasAction = true;
                    await LetsThink(cancellationToken);
                    await LetsThink(cancellationToken);
                }

                // if (Constants.DEV_MODE)
                // break;
            }

            foreach (WorkingCard card in GetSpellCardsInHand())
            {
                if (CardCanBePlayable(card) && CheckSpecialCardRules(card))
                {
                    PlayCardOnBoard(card);
                    wasAction = true;
                    await LetsThink(cancellationToken);
                    await LetsThink(cancellationToken);
                }

                // if (Constants.DEV_MODE)
                // break;
            }

            if (wasAction)
            {
                await LetsThink(cancellationToken);
                await LetsThink(cancellationToken);
            }
        }

        // ai step 2
        private async Task UseUnitsOnBoard(CancellationToken cancellationToken)
        {
            // return;
            List<BoardUnit> unitsOnBoard = new List<BoardUnit>();
            List<BoardUnit> alreadyUsedUnits = new List<BoardUnit>();

            unitsOnBoard.AddRange(GetUnitsOnBoard());

            if (OpponentHasHeavyUnits())
            {
                foreach (BoardUnit unit in unitsOnBoard)
                {
                    while (UnitCanBeUsable(unit))
                    {
                        if (UnitCanBeUsable(unit))
                        {
                            BoardUnit attackedUnit = GetTargetOpponentUnit();
                            if (attackedUnit != null)
                            {
                                unit.DoCombat(attackedUnit);
                                alreadyUsedUnits.Add(unit);

                                await LetsThink(cancellationToken);
                                if (!OpponentHasHeavyUnits())
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            foreach (BoardUnit creature in alreadyUsedUnits)
            {
                unitsOnBoard.Remove(creature);
            }

            int totalValue = GetPlayerAttackingValue();
            if (((totalValue >= _gameplayManager.CurrentPlayer.Hp) || (_aiType == Enumerators.AiType.BlitzAi) || (_aiType == Enumerators.AiType.TimeBlitzAi)) && !_tutorialManager.IsTutorial)
            {
                foreach (BoardUnit unit in unitsOnBoard)
                {
                    while (UnitCanBeUsable(unit))
                    {
                        if (UnitCanBeUsable(unit))
                        {
                            unit.DoCombat(_gameplayManager.CurrentPlayer);
                            await LetsThink(cancellationToken);
                        }
                    }
                }
            } else
            {
                foreach (BoardUnit unit in unitsOnBoard)
                {
                    while (UnitCanBeUsable(unit))
                    {
                        if (UnitCanBeUsable(unit))
                        {
                            if ((GetPlayerAttackingValue() > GetOpponentAttackingValue()) && !_tutorialManager.IsTutorial)
                            {
                                unit.DoCombat(_gameplayManager.CurrentPlayer);
                                await LetsThink(cancellationToken);
                            } else
                            {
                                BoardUnit attackedCreature = GetRandomOpponentUnit();
                                if (attackedCreature != null)
                                {
                                    unit.DoCombat(attackedCreature);
                                    await LetsThink(cancellationToken);
                                } else
                                {
                                    unit.DoCombat(_gameplayManager.CurrentPlayer);
                                    await LetsThink(cancellationToken);
                                }
                            }
                        }
                    }
                }
            }
        }

        // ai step 3
        private async Task UsePlayerSkills(CancellationToken cancellationToken)
        {
            // return;
            bool wasAction = false;
            if (_gameplayManager.IsTutorial || _gameplayManager.OpponentPlayer.IsStunned)

                return;

            if (_skillsController.OpponentPrimarySkill.IsSkillReady)
            {
                DoBoardSkill(_skillsController.OpponentPrimarySkill);
                wasAction = true;
            }

            if (wasAction)
            {
                await LetsThink(cancellationToken);
            }

            if (_skillsController.OpponentSecondarySkill.IsSkillReady)
            {
                DoBoardSkill(_skillsController.OpponentSecondarySkill);
                wasAction = true;
            }

            if (wasAction)
            {
                await LetsThink(cancellationToken);
                await LetsThink(cancellationToken);
            }
        }

        // some thinking - delay between general actions
        private async Task LetsThink(CancellationToken cancellationToken)
        {
            await Task.Delay(Constants.DelayBetweenAiActions, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        private bool CardCanBePlayable(WorkingCard card)
        {
#if !DEV_MODE
            return (card.LibraryCard.Cost <= _gameplayManager.OpponentPlayer.Goo) && (_gameplayManager.OpponentPlayer.Turn > _minTurnForAttack);
#else
            return true;
#endif
        }

        private bool UnitCanBeUsable(BoardUnit unit)
        {
            return unit.UnitCanBeUsable();
        }

        private bool CheckSpecialCardRules(WorkingCard card)
        {
            if (card.LibraryCard.Abilities != null)
            {
                foreach (AbilityData ability in card.LibraryCard.Abilities)
                {
                    if (ability.AbilityType == Enumerators.AbilityType.AttackOverlord)
                    {
                        // smart enough HP to use goo carriers
                        if (ability.Value * 2 >= _gameplayManager.OpponentPlayer.Hp)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void PlayCardOnBoard(WorkingCard card)
        {
            bool needTargetForAbility = false;

            if ((card.LibraryCard.Abilities != null) && (card.LibraryCard.Abilities.Count > 0))
            {
                needTargetForAbility = card.LibraryCard.Abilities.FindAll(x => x.AbilityTargetTypes.Count > 0).Count > 0;
            }

            object target = null;

            if (needTargetForAbility)
            {
                target = GetAbilityTarget(card);
            }

            if ((card.LibraryCard.CardKind == Enumerators.CardKind.Creature) && (_battlegroundController.OpponentBoardCards.Count < Constants.MaxBoardUnits))
            {
                // if (libraryCard.abilities.Find(x => x.abilityType == Enumerators.AbilityType.CARD_RETURN) != null)
                // if (target.Count == 0)
                // return false;
                _gameplayManager.OpponentPlayer.RemoveCardFromHand(card);
                _gameplayManager.OpponentPlayer.AddCardToBoard(card);

                _cardsController.PlayOpponentCard(_gameplayManager.OpponentPlayer, card, target, PlayCardCompleteHandler);

                _cardsController.DrawCardInfo(card);
            } else if (card.LibraryCard.CardKind == Enumerators.CardKind.Spell)
            {
                if (((target != null) && needTargetForAbility) || !needTargetForAbility)
                {
                    _gameplayManager.OpponentPlayer.RemoveCardFromHand(card);
                    _gameplayManager.OpponentPlayer.AddCardToBoard(card);

                    _cardsController.PlayOpponentCard(_gameplayManager.OpponentPlayer, card, target, PlayCardCompleteHandler);
                    _cardsController.DrawCardInfo(card);
                }
            }

            _gameplayManager.OpponentPlayer.Goo -= card.LibraryCard.Cost;
        }

        private void PlayCardCompleteHandler(WorkingCard card, object target)
        {
            WorkingCard workingCard = _gameplayManager.OpponentPlayer.CardsOnBoard[_gameplayManager.OpponentPlayer.CardsOnBoard.Count - 1];

            if (card.LibraryCard.CardKind == Enumerators.CardKind.Creature)
            {
                BoardUnit boardUnitElement = new BoardUnit(GameObject.Find("OpponentBoard").transform);
                GameObject boardCreature = boardUnitElement.GameObject;
                boardCreature.tag = Constants.KTagOpponentOwned;
                boardCreature.transform.position = Vector3.zero;
                boardUnitElement.OwnerPlayer = card.Owner;

                boardUnitElement.SetObjectInfo(workingCard);
                _battlegroundController.OpponentBoardCards.Add(boardUnitElement);

                boardCreature.transform.position += Vector3.up * 2f; // Start pos before moving cards to the opponents board

                // PlayArrivalAnimation(boardCreature, libraryCard.cardType);
                _gameplayManager.OpponentPlayer.BoardCards.Add(boardUnitElement);

                _actionsQueueController.PostGameActionReport(_actionsQueueController.FormatGameActionReport(Enumerators.ActionType.PlayUnitCard, new object[] { boardUnitElement.OwnerPlayer, boardUnitElement }));

                boardUnitElement.PlayArrivalAnimation();

                _battlegroundController.UpdatePositionOfBoardUnitsOfOpponent(
                    () =>
                    {
                        bool createTargetArrow = false;

                        if ((card.LibraryCard.Abilities != null) && (card.LibraryCard.Abilities.Count > 0))
                        {
                            createTargetArrow = _abilitiesController.IsAbilityCanActivateTargetAtStart(card.LibraryCard.Abilities[0]);
                        }

                        if (target != null)
                        {
                            CreateOpponentTarget(
                                createTargetArrow,
                                false,
                                boardCreature.gameObject,
                                target,
                                () =>
                                {
                                    _abilitiesController.CallAbility(card.LibraryCard, null, workingCard, Enumerators.CardKind.Creature, boardUnitElement, null, false, null, target);
                                });
                        } else
                        {
                            _abilitiesController.CallAbility(card.LibraryCard, null, workingCard, Enumerators.CardKind.Creature, boardUnitElement, null, false, null);
                        }
                    });

                // Debug.Log("UpdatePositionOfBoardUnitsOfOpponent");

                // _battlegroundController.UpdatePositionOfBoardUnitsOfOpponent();
            } else if (card.LibraryCard.CardKind == Enumerators.CardKind.Spell)
            {
                GameObject spellCard = Object.Instantiate(_cardsController.SpellCardViewPrefab);
                spellCard.transform.position = GameObject.Find("OpponentSpellsPivot").transform.position;

                CurrentSpellCard = new SpellBoardCard(spellCard);

                CurrentSpellCard.Init(workingCard);
                CurrentSpellCard.SetHighlightingEnabled(false);

                BoardSpell boardSpell = new BoardSpell(spellCard, workingCard);

                spellCard.gameObject.SetActive(false);

                bool createTargetArrow = false;

                if ((card.LibraryCard.Abilities != null) && (card.LibraryCard.Abilities.Count > 0))
                {
                    createTargetArrow = _abilitiesController.IsAbilityCanActivateTargetAtStart(card.LibraryCard.Abilities[0]);
                }

                if (target != null)
                {
                    CreateOpponentTarget(
                        createTargetArrow,
                        false,
                        _gameplayManager.OpponentPlayer.AvatarObject,
                        target,
                        () =>
                        {
                            _abilitiesController.CallAbility(card.LibraryCard, null, workingCard, Enumerators.CardKind.Spell, boardSpell, null, false, null, target);
                        });
                } else
                {
                    _abilitiesController.CallAbility(card.LibraryCard, null, workingCard, Enumerators.CardKind.Spell, boardSpell, null, false, null);
                }
            }
        }

        private object GetAbilityTarget(WorkingCard card)
        {
            Card libraryCard = card.LibraryCard;

            object target = null;

            List<AbilityData> abilitiesWithTarget = new List<AbilityData>();

            bool needsToSelectTarget = false;
            foreach (AbilityData ability in libraryCard.Abilities)
            {
                foreach (Enumerators.AbilityTargetType item in ability.AbilityTargetTypes)
                {
                    switch (item)
                    {
                        case Enumerators.AbilityTargetType.OpponentCard:
                        {
                            if ((_gameplayManager.CurrentPlayer.BoardCards.Count > 1) || ((ability.AbilityType == Enumerators.AbilityType.CardReturn) && (_gameplayManager.CurrentPlayer.BoardCards.Count > 0)))
                            {
                                needsToSelectTarget = true;
                                abilitiesWithTarget.Add(ability);
                            }
                        }

                            break;
                        case Enumerators.AbilityTargetType.PlayerCard:
                        {
                            if ((_gameplayManager.OpponentPlayer.BoardCards.Count > 1) || (libraryCard.CardKind == Enumerators.CardKind.Spell) || ((ability.AbilityType == Enumerators.AbilityType.CardReturn) && (_gameplayManager.OpponentPlayer.BoardCards.Count > 0)))
                            {
                                needsToSelectTarget = true;
                                abilitiesWithTarget.Add(ability);
                            }
                        }

                            break;
                        case Enumerators.AbilityTargetType.Player:
                        case Enumerators.AbilityTargetType.Opponent:
                        case Enumerators.AbilityTargetType.All:
                        {
                            needsToSelectTarget = true;
                            abilitiesWithTarget.Add(ability);
                        }

                            break;
                    }
                }
            }

            if (needsToSelectTarget)
            {
                foreach (AbilityData ability in abilitiesWithTarget)
                {
                    switch (ability.AbilityType)
                    {
                        case Enumerators.AbilityType.AddGooVial:
                        {
                            target = _gameplayManager.OpponentPlayer;
                        }

                            break;
                        case Enumerators.AbilityType.CardReturn:
                        {
                            if (!AddRandomTargetUnit(true, ref target, false, true))
                            {
                                AddRandomTargetUnit(false, ref target, true, true);
                            }
                        }

                            break;
                        case Enumerators.AbilityType.DamageTarget:
                        {
                            CheckAndAddTargets(ability, ref target);
                        }

                            break;
                        case Enumerators.AbilityType.DamageTargetAdjustments:
                        {
                            if (!AddRandomTargetUnit(true, ref target))
                            {
                                target = _gameplayManager.CurrentPlayer;
                            }
                        }

                            break;
                        case Enumerators.AbilityType.MassiveDamage:
                        {
                            AddRandomTargetUnit(true, ref target);
                        }

                            break;
                        case Enumerators.AbilityType.ModificatorStats:
                        {
                            if (ability.Value > 0)
                            {
                                AddRandomTargetUnit(false, ref target);
                            } else
                            {
                                AddRandomTargetUnit(true, ref target);
                            }
                        }

                            break;
                        case Enumerators.AbilityType.Stun:
                        {
                            CheckAndAddTargets(ability, ref target);
                        }

                            break;
                        case Enumerators.AbilityType.StunOrDamageAdjustments:
                        {
                            CheckAndAddTargets(ability, ref target);
                        }

                            break;
                        case Enumerators.AbilityType.ChangeStat:
                        {
                            if (ability.Value > 0)
                            {
                                AddRandomTargetUnit(false, ref target);
                            } else
                            {
                                AddRandomTargetUnit(true, ref target);
                            }
                        }

                            break;
                        case Enumerators.AbilityType.Summon:
                        {
                        }

                            break;
                        case Enumerators.AbilityType.Weapon:
                        {
                            target = _gameplayManager.CurrentPlayer;
                        }

                            break;
                        case Enumerators.AbilityType.Spurt:
                        {
                            AddRandomTargetUnit(true, ref target);
                        }

                            break;
                        case Enumerators.AbilityType.SpellAttack:
                        {
                            CheckAndAddTargets(ability, ref target);
                        }

                            break;
                        case Enumerators.AbilityType.Heal:
                        {
                            List<BoardUnit> units = GetUnitsWithLowHp();

                            if (units.Count > 0)
                            {
                                target = units[_random.Next(0, units.Count)];
                            } else
                            {
                                target = _gameplayManager.OpponentPlayer;
                            }
                        }

                            break;
                        case Enumerators.AbilityType.Dot:
                        {
                            CheckAndAddTargets(ability, ref target);
                        }

                            break;
                        case Enumerators.AbilityType.DestroyUnitByType:
                        {
                            GetTargetByType(ability, ref target, false);
                        }

                            break;
                    }

                    return target; // hack to handle only one ability
                }

                return target;
            }

            return null;
        }

        private void CheckAndAddTargets(AbilityData ability, ref object target)
        {
            if (ability.AbilityTargetTypes.Contains(Enumerators.AbilityTargetType.OpponentCard))
            {
                AddRandomTargetUnit(true, ref target);
            } else if (ability.AbilityTargetTypes.Contains(Enumerators.AbilityTargetType.Opponent))
            {
                target = _gameplayManager.CurrentPlayer;
            }
        }

        private void GetTargetByType(AbilityData ability, ref object target, bool checkPlayerAlso)
        {
            if (ability.AbilityTargetTypes.Contains(Enumerators.AbilityTargetType.OpponentCard))
            {
                List<BoardUnit> targets = GetHeavyUnitsOnBoard(_gameplayManager.CurrentPlayer);

                if (targets.Count > 0)
                {
                    target = targets[UnityEngine.Random.Range(0, targets.Count)];
                }

                if (checkPlayerAlso && (target == null) && ability.AbilityTargetTypes.Contains(Enumerators.AbilityTargetType.PlayerCard))
                {
                    target = _gameplayManager.CurrentPlayer;

                    targets = GetHeavyUnitsOnBoard(_gameplayManager.OpponentPlayer);

                    if (targets.Count > 0)
                    {
                        target = targets[UnityEngine.Random.Range(0, targets.Count)];
                    }
                }
            }
        }

        private List<BoardUnit> GetHeavyUnitsOnBoard(Player player)
        {
            return player.BoardCards.FindAll(x => x.HasHeavy || x.HasBuffHeavy);
        }

        private bool AddRandomTargetUnit(bool opponent, ref object target, bool lowHp = false, bool addAttackIgnore = false)
        {
            BoardUnit boardUnit = null;

            if (opponent)
            {
                boardUnit = GetRandomOpponentUnit();
            } else
            {
                boardUnit = GetRandomUnit(lowHp);
            }

            if (boardUnit != null)
            {
                target = boardUnit;

                if (addAttackIgnore)
                {
                    _attackedUnitTargets.Add(boardUnit);
                }

                return true;
            }

            return false;
        }

        private int GetPlayerAttackingValue()
        {
            int power = 0;
            foreach (BoardUnit creature in _gameplayManager.OpponentPlayer.BoardCards)
            {
                if ((creature.CurrentHp > 0) && ((creature.NumTurnsOnBoard >= 1) || creature.IsFeralUnit()))
                {
                    power += creature.CurrentDamage;
                }
            }

            return power;
        }

        private int GetOpponentAttackingValue()
        {
            int power = 0;
            foreach (BoardUnit card in _gameplayManager.CurrentPlayer.BoardCards)
            {
                power += card.CurrentDamage;
            }

            return power;
        }

        private List<BoardUnit> GetUnitsWithLowHp(List<BoardUnit> unitsToIgnore = null)
        {
            List<BoardUnit> finalList = new List<BoardUnit>();

            List<BoardUnit> list = GetUnitsOnBoard();

            foreach (BoardUnit item in list)
            {
                if (item.CurrentHp < item.MaxCurrentHp)
                {
                    finalList.Add(item);
                }
            }

            if (unitsToIgnore != null)
            {
                finalList = finalList.FindAll(x => !unitsToIgnore.Contains(x));
            }

            finalList = finalList.OrderBy(x => x.CurrentHp).OrderBy(y => y.CurrentHp.ToString().Length).ToList();

            return finalList;
        }

        private List<WorkingCard> GetUnitCardsInHand()
        {
            List<WorkingCard> list = _gameplayManager.OpponentPlayer.CardsInHand.FindAll(x => x.LibraryCard.CardKind == Enumerators.CardKind.Creature);

            List<Card> cards = new List<Card>();

            foreach (WorkingCard item in list)
            {
                cards.Add(_dataManager.CachedCardsLibraryData.GetCard(item.CardId));
            }

            cards = cards.OrderBy(x => x.Cost).ThenBy(y => y.Cost.ToString().Length).ToList();

            List<WorkingCard> sortedList = new List<WorkingCard>();

            cards.Reverse();

            foreach (Card item in cards)
            {
                sortedList.Add(list.Find(x => (x.CardId == item.Id) && !sortedList.Contains(x)));
            }

            list.Clear();
            cards.Clear();

            return sortedList;
        }

        private List<WorkingCard> GetSpellCardsInHand()
        {
            return _gameplayManager.OpponentPlayer.CardsInHand.FindAll(x => x.LibraryCard.CardKind == Enumerators.CardKind.Spell);
        }

        private List<BoardUnit> GetUnitsOnBoard()
        {
            return _gameplayManager.OpponentPlayer.BoardCards.FindAll(x => x.CurrentHp > 0);
        }

        private BoardUnit GetRandomUnit(bool lowHp = false, List<BoardUnit> unitsToIgnore = null)
        {
            List<BoardUnit> eligibleUnits = null;

            if (!lowHp)
            {
                eligibleUnits = _gameplayManager.OpponentPlayer.BoardCards.FindAll(x => (x.CurrentHp > 0) && !_attackedUnitTargets.Contains(x));
            } else
            {
                eligibleUnits = _gameplayManager.OpponentPlayer.BoardCards.FindAll(x => (x.CurrentHp < x.MaxCurrentHp) && !_attackedUnitTargets.Contains(x));
            }

            if (unitsToIgnore != null)
            {
                eligibleUnits = eligibleUnits.FindAll(x => !unitsToIgnore.Contains(x));
            }

            if (eligibleUnits.Count > 0)
            {
                return eligibleUnits[_random.Next(0, eligibleUnits.Count)];
            }

            return null;
        }

        private BoardUnit GetTargetOpponentUnit()
        {
            List<BoardUnit> eligibleUnits = _gameplayManager.CurrentPlayer.BoardCards.FindAll(x => x.CurrentHp > 0);

            if (eligibleUnits.Count > 0)
            {
                List<BoardUnit> heavyUnits = eligibleUnits.FindAll(x => x.IsHeavyUnit());
                if ((heavyUnits != null) && (heavyUnits.Count >= 1))
                {
                    return heavyUnits[_random.Next(0, heavyUnits.Count)];
                }

                return eligibleUnits[_random.Next(0, eligibleUnits.Count)];
            }

            return null;
        }

        private BoardUnit GetRandomOpponentUnit(List<BoardUnit> unitsToIgnore = null)
        {
            List<BoardUnit> eligibleCreatures = _gameplayManager.CurrentPlayer.BoardCards.FindAll(x => (x.CurrentHp > 0) && !_attackedUnitTargets.Contains(x));

            if (unitsToIgnore != null)
            {
                eligibleCreatures = eligibleCreatures.FindAll(x => !unitsToIgnore.Contains(x));
            }

            if (eligibleCreatures.Count > 0)
            {
                return eligibleCreatures[_random.Next(0, eligibleCreatures.Count)];
            }

            return null;
        }

        private bool OpponentHasHeavyUnits()
        {
            List<BoardUnit> board = _gameplayManager.CurrentPlayer.BoardCards;
            List<BoardUnit> eligibleCreatures = board.FindAll(x => x.CurrentHp > 0);
            if (eligibleCreatures.Count > 0)
            {
                List<BoardUnit> provokeCreatures = eligibleCreatures.FindAll(x => x.IsHeavyUnit());
                return (provokeCreatures != null) && (provokeCreatures.Count >= 1);
            }

            return false;
        }

        private void DoBoardSkill(BoardSkill skill)
        {
            object target = null;

            Enumerators.AffectObjectType selectedObjectType = Enumerators.AffectObjectType.None;

            switch (skill.Skill.OverlordSkill)
            {
                case Enumerators.OverlordSkill.Harden:
                case Enumerators.OverlordSkill.StoneSkin:
                case Enumerators.OverlordSkill.Draw:
                {
                    selectedObjectType = Enumerators.AffectObjectType.Player;
                    target = _gameplayManager.OpponentPlayer;
                }

                    break;
                case Enumerators.OverlordSkill.HealingTouch:
                case Enumerators.OverlordSkill.Mend:
                {
                    target = _gameplayManager.OpponentPlayer;
                    selectedObjectType = Enumerators.AffectObjectType.Player;

                    if (_gameplayManager.OpponentPlayer.Hp > 13)
                    {
                        if (skill.Skill.ElementTargetTypes.Count > 0)
                        {
                            _unitsToIgnoreThisTurn = _gameplayManager.OpponentPlayer.BoardCards.FindAll(x => !skill.Skill.ElementTargetTypes.Contains(x.Card.LibraryCard.CardSetType));
                        }

                        List<BoardUnit> units = GetUnitsWithLowHp(_unitsToIgnoreThisTurn);

                        if (units.Count > 0)
                        {
                            target = units[0];
                            selectedObjectType = Enumerators.AffectObjectType.Character;
                        }
                    }
                }

                    break;
                case Enumerators.OverlordSkill.Rabies:
                {
                    _unitsToIgnoreThisTurn = _gameplayManager.OpponentPlayer.BoardCards.FindAll(x => ((skill.Skill.ElementTargetTypes.Count > 0) && !skill.Skill.ElementTargetTypes.Contains(x.Card.LibraryCard.CardSetType)) || (x.NumTurnsOnBoard > 0) || x.HasFeral);
                    BoardUnit unit = GetRandomUnit(false, _unitsToIgnoreThisTurn);

                    if (unit != null)
                    {
                        target = unit;
                        selectedObjectType = Enumerators.AffectObjectType.Character;
                    }
                }

                    break;
                case Enumerators.OverlordSkill.PoisonDart:
                case Enumerators.OverlordSkill.ToxicPower:
                case Enumerators.OverlordSkill.IceBolt:
                case Enumerators.OverlordSkill.Freeze:
                case Enumerators.OverlordSkill.FireBolt:
                {
                    target = _gameplayManager.CurrentPlayer;
                    selectedObjectType = Enumerators.AffectObjectType.Player;

                    BoardUnit unit = GetRandomOpponentUnit();

                    if (unit != null)
                    {
                        target = unit;
                        selectedObjectType = Enumerators.AffectObjectType.Character;
                    }
                }

                    break;
                case Enumerators.OverlordSkill.Push:
                {
                    if (skill.Skill.ElementTargetTypes.Count > 0)
                    {
                        _unitsToIgnoreThisTurn = _gameplayManager.OpponentPlayer.BoardCards.FindAll(x => !skill.Skill.ElementTargetTypes.Contains(x.Card.LibraryCard.CardSetType));
                    }

                    List<BoardUnit> units = GetUnitsWithLowHp(_unitsToIgnoreThisTurn);

                    if (units.Count > 0)
                    {
                        target = units[0];

                        _unitsToIgnoreThisTurn.Add(target as BoardUnit);

                        selectedObjectType = Enumerators.AffectObjectType.Character;
                    } else
                    {
                        BoardUnit unit = GetRandomOpponentUnit(_unitsToIgnoreThisTurn);

                        if (unit != null)
                        {
                            target = unit;

                            _unitsToIgnoreThisTurn.Add(target as BoardUnit);

                            selectedObjectType = Enumerators.AffectObjectType.Character;
                        } else
                        {
                            return;
                        }
                    }
                }

                    break;
                default: return;
            }

            // if (selectedObjectType != Enumerators.AffectObjectType.NONE)
            skill.StartDoSkill();

            if (selectedObjectType == Enumerators.AffectObjectType.Player)
            {
                skill.FightTargetingArrow = CreateOpponentTarget(
                    true,
                    skill.IsPrimary,
                    skill.SelfObject,
                    target as Player,
                    () =>
                    {
                        skill.FightTargetingArrow.SelectedPlayer = target as Player;
                        skill.EndDoSkill();
                    });
            } else
            {
                if (target != null)
                {
                    BoardUnit unit = target as BoardUnit;

                    skill.FightTargetingArrow = CreateOpponentTarget(
                        true,
                        skill.IsPrimary,
                        skill.SelfObject,
                        unit,
                        () =>
                        {
                            skill.FightTargetingArrow.SelectedCard = unit;
                            skill.EndDoSkill();
                        });
                }
            }
        }

        // rewrite
        private OpponentBoardArrow CreateOpponentTarget(bool createTargetArrow, bool isReverseArrow, GameObject startObj, object target, Action action)
        {
            if (!createTargetArrow)
            {
                action?.Invoke();
                return null;
            }

            OpponentBoardArrow targetingArrow = Object.Instantiate(_fightTargetingArrowPrefab).AddComponent<OpponentBoardArrow>();
            targetingArrow.Begin(startObj.transform.position);

            targetingArrow.SetTarget(target);

            MainApp.Instance.StartCoroutine(RemoveOpponentTargetingArrow(targetingArrow, action));

            return targetingArrow;
        }

        // rewrite
        private IEnumerator RemoveOpponentTargetingArrow(OpponentBoardArrow arrow, Action action)
        {
            yield return new WaitForSeconds(1f);
            arrow.Dispose();
            Object.Destroy(arrow.gameObject);

            action?.Invoke();
        }
    }
}
