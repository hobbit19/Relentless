using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Helpers;

namespace Loom.ZombieBattleground
{
    public class AbilitiesController : IController
    {
        private static readonly ILog Log = Logging.GetLog(nameof(AbilitiesController));

        private IGameplayManager _gameplayManager;

        private BattlegroundController _battlegroundController;

        private Dictionary<Enumerators.AbilityTrigger, List<ICardAbility>> _activeAbilities;

        public void Dispose()
        {
        }

        public void ResetAll()
        {
            UnsubscribeEvents();
        }

        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();

            _gameplayManager.GameInitialized += GameInitializedHandler;
            _gameplayManager.GameEnded += GameEndedHandler;
        }

        public void Update()
        {
        }

        private void GameInitializedHandler()
        {
            _activeAbilities = new Dictionary<Enumerators.AbilityTrigger, List<ICardAbility>>();

            for (int i = 1; i < Enum.GetNames(typeof(Enumerators.AbilityTrigger)).Length; i++)
            {
                _activeAbilities.Add((Enumerators.AbilityTrigger)i, new List<ICardAbility>());
            }

            SubscribeEvents();
        }

        private void GameEndedHandler(Enumerators.EndGameType obj)
        {
            foreach(KeyValuePair<Enumerators.AbilityTrigger, List<ICardAbility>> element in _activeAbilities)
            {
                foreach (ICardAbility ability in element.Value)
                {
                    ability.Dispose();
                }
            }

            _activeAbilities.Clear();

            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _battlegroundController.TurnStarted += TurnStartedHandler;
            _battlegroundController.TurnEnded += TurnEndedHandler;
            _battlegroundController.UnitDied += UnitDiedHandler;

            _gameplayManager.CurrentPlayer.CardPlayed += CardPlayedHandler;
            _gameplayManager.CurrentPlayer.PlayerCardsController.HandChanged += (count) =>
            {
                HandChangedHandler(_gameplayManager.CurrentPlayer);
            };
            _gameplayManager.CurrentPlayer.PlayerCardsController.BoardChanged += (count) =>
            {
                BoardChangedHandler(_gameplayManager.CurrentPlayer);
            };

            _gameplayManager.OpponentPlayer.CardPlayed += CardPlayedHandler;
            _gameplayManager.OpponentPlayer.PlayerCardsController.HandChanged += (count) =>
            {
                HandChangedHandler(_gameplayManager.OpponentPlayer);
            };
            _gameplayManager.OpponentPlayer.PlayerCardsController.BoardChanged += (count) =>
            {
                BoardChangedHandler(_gameplayManager.OpponentPlayer);
            };
        }

        private void UnsubscribeEvents()
        {
            _battlegroundController.TurnStarted -= TurnStartedHandler;
            _battlegroundController.TurnEnded -= TurnEndedHandler;
            _battlegroundController.UnitDied -= UnitDiedHandler;

            _gameplayManager.CurrentPlayer.CardPlayed -= CardPlayedHandler;
            _gameplayManager.CurrentPlayer.PlayerCardsController.HandChanged -= (count) =>
            {
                HandChangedHandler(_gameplayManager.CurrentPlayer);
            };
            _gameplayManager.CurrentPlayer.PlayerCardsController.BoardChanged -= (count) =>
            {
                BoardChangedHandler(_gameplayManager.CurrentPlayer);
            };

            _gameplayManager.OpponentPlayer.CardPlayed -= CardPlayedHandler;
            _gameplayManager.OpponentPlayer.PlayerCardsController.HandChanged -= (count) =>
            {
                HandChangedHandler(_gameplayManager.OpponentPlayer);
            };
            _gameplayManager.OpponentPlayer.PlayerCardsController.BoardChanged -= (count) =>
            {
                BoardChangedHandler(_gameplayManager.OpponentPlayer);
            };
        }

        public void InitializeAbilities(
            BoardUnitModel boardUnitModel,
            List<BoardObject> targets = null,
            Enumerators.AbilityTrigger triggerFilter = Enumerators.AbilityTrigger.Undefined)
        {
            List<BoardObject> selectedTargets;
            List<CardAbilityData.TriggerInfo> triggers;
            foreach (CardAbilitiesCombination combination in boardUnitModel.Card.Prototype.Abilities)
            {
                selectedTargets = targets;

                foreach (CardAbilityData cardAbilityData in combination.CardAbilities)
                {
                    triggers = GetAllTriggers(combination, cardAbilityData);

                    if (triggerFilter == Enumerators.AbilityTrigger.Undefined)
                    {
                        CardAbilityData.TriggerInfo triggerInfo;
                        if (TryGetTrigger(triggers, Enumerators.AbilityTrigger.Entry, out triggerInfo) ||
                            TryGetTrigger(triggers, Enumerators.AbilityTrigger.Entry, out triggerInfo))
                        {
                            selectedTargets = FilterTargets(boardUnitModel, triggerInfo, GetAllGenericParameters(combination, cardAbilityData),
                                                    GetTargets(boardUnitModel, combination, cardAbilityData, targets));
                        }
                        else if (selectedTargets == null)
                        {
                            selectedTargets = new List<BoardObject>();
                        }
                    }
                    else if (!HasTrigger(triggers, triggerFilter))
                        continue;

                    InitializeAbility(boardUnitModel, combination, cardAbilityData, selectedTargets);
                }
            }
        }

        public void EndAbility(ICardAbility cardAbility)
        {
            KeyValuePair<Enumerators.AbilityTrigger, List<ICardAbility>> keyValuePair =
                _activeAbilities.FirstOrDefault(element => element.Value.Contains(cardAbility));

            if(!keyValuePair.IsDefault() &&
                keyValuePair.Value != null &&
                keyValuePair.Value.Contains(cardAbility))
            {
                keyValuePair.Value.Remove(cardAbility);
            }
        }

        public void GiveAbilityToUnit(BoardUnitModel boardUnitModel, CardAbilityData cardAbilityData)
        {
            InitializeAbility(boardUnitModel, null, cardAbilityData, new List<BoardObject>() { boardUnitModel });
        }

        public void ReactivateAbilitiesOnUnit(BoardUnitModel boardUnitModel)
        {
            foreach (CardAbilitiesCombination combination in boardUnitModel.Card.Prototype.Abilities)
            {
                foreach (CardAbilityData cardAbilityData in combination.CardAbilities)
                {
                    InitializeAbility(boardUnitModel, combination, cardAbilityData, new List<BoardObject>(), true);
                }
            }
        }

        private void InitializeAbility(BoardUnitModel boardUnitModel,
                                       CardAbilitiesCombination combination,
                                       CardAbilityData cardAbilityData,
                                       List<BoardObject> targets,
                                       bool ignoreEntry = false)
        {
            if (cardAbilityData.Ability == Enumerators.AbilityType.Undefined)
                return;

            ICardAbility ability;
            ICardAbilityView abilityView = null;

            string abilityViewClassName, abilitySubViewClassName, abilityClassName;


            List<CardAbilityData.TriggerInfo> triggers = cardAbilityData.Triggers;

            if (triggers == null)
            {
                triggers = combination.DefaultTriggers;
            }

            if (triggers != null)
            {
                foreach (CardAbilityData.TriggerInfo trigger in triggers)
                {
                    if (ignoreEntry &&
                        (trigger.Trigger == Enumerators.AbilityTrigger.Entry ||
                        trigger.Trigger == Enumerators.AbilityTrigger.EntryWithSelection))
                        continue;

                    abilityClassName = $"{cardAbilityData.Ability.ToString()}Ability";

                    if (!InternalTools.IsTypeExists<ICardAbility>(abilityClassName))
                        continue;

                    abilityViewClassName = $"{abilityClassName}View";
                    abilitySubViewClassName = abilityViewClassName + boardUnitModel.Card.Prototype.MouldId;

                    if (InternalTools.IsTypeExists<ICardAbilityView>(abilitySubViewClassName))
                    {
                        abilityView = InternalTools.GetInstance<ICardAbilityView>(abilitySubViewClassName);
                    }
                    else if (InternalTools.IsTypeExists<ICardAbilityView>(abilityViewClassName))
                    {
                        abilityView = InternalTools.GetInstance<ICardAbilityView>(abilityViewClassName);
                    }

                    ability = InternalTools.GetInstance<ICardAbility>(abilityClassName);
                    ability.Init(boardUnitModel, combination, trigger, cardAbilityData, targets, abilityView);

                    _activeAbilities[trigger.Trigger].Add(ability);

                    CheckOnPermanentAbility(ability);
                    CheckOnEntryAbility(ability);
                }
            }
            else
            {
                Log.Warn($"Ability {cardAbilityData.Ability} havent triggers on card {boardUnitModel.InstanceId}");
            }
        }

        #region event callers

        public void ChangeCardOwner(BoardUnitModel boardUnitModel, Player newOwner)
        {
            foreach(List<ICardAbility> abilities in _activeAbilities.Values)
            {
                foreach (ICardAbility ability in abilities)
                {
                    ability.ChangePlayerOwner(newOwner);
                }
            }
        }

        public void UnitStatChanged(Enumerators.Stat stat, BoardUnitModel boardUnit, int from, int to)
        {
            foreach (ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.StatChanged])
            {
                if (ability.UnitModelOwner == boardUnit)
                {
                    ability.InsertTargets(FilterTargets(ability.UnitModelOwner, ability.MainTrigger, ability.GenericParameters,
                                                        GetTargets(ability.UnitModelOwner, ability.Combination, ability.CardAbilityData, new List<BoardObject>()
                    {
                        boardUnit
                    }, true)));
                    ability.DoAction(new List<GenericParameter>()
                    {
                        new GenericParameter(Enumerators.AbilityParameter.Stat, stat)
                    });
                }
            }
        }

        public void UnitBeganAttack(BoardUnitModel attacker)
        {
            foreach (ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.Attack])
            {
                if (ability.UnitModelOwner == attacker)
                {
                    ability.InsertTargets(FilterTargets(
                        ability.UnitModelOwner,
                        ability.MainTrigger,
                        ability.CardAbilityData.GenericParameters,
                        new List<BoardObject>()
                        {
                            attacker
                        }));
                    ability.DoAction(null);
                }
            }
        }

        public void UnitAttacked(BoardUnitModel attacker, BoardObject targetAttacked, int damage)
        {
            foreach (ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.Attack])
            {
                if (ability.UnitModelOwner == attacker)
                {
                    ability.InsertTargets(FilterTargets(ability.UnitModelOwner, ability.MainTrigger, ability.GenericParameters,
                                                        GetTargets(ability.UnitModelOwner, ability.Combination, ability.CardAbilityData, new List<BoardObject>()
                    {
                        targetAttacked
                    }, true)));
                    ability.DoAction(new List<GenericParameter>() { new GenericParameter(Enumerators.AbilityParameter.Damage, damage) });
                }
            }
        }

        public void UnitKilled(BoardUnitModel attacker, BoardObject targetKilled)
        {
            foreach (ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.KillUnit])
            {
                if (ability.UnitModelOwner == attacker)
                {
                    ability.InsertTargets(FilterTargets(ability.UnitModelOwner, ability.MainTrigger, ability.GenericParameters,
                                                         GetTargets(ability.UnitModelOwner, ability.Combination, ability.CardAbilityData, new List<BoardObject>()
                     {
                        targetKilled
                     }, true)));
                    ability.DoAction(null);
                }
            }
        }

        public void UnitBeingBeAttacked(BoardObject unitAttacked)
        {
            foreach (ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.AtDefense])
            {
                if (ability.UnitModelOwner == unitAttacked)
                {
                    ability.InsertTargets(new List<BoardObject>()
                    {
                        unitAttacked
                    });
                    ability.DoAction(null);
                }
            }
        }

        public void UnitWasAttacked(BoardUnitModel attacker, BoardObject targetAttacked, int damage)
        {
            foreach (ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.AtDefense])
            {
                if (ability.UnitModelOwner == attacker)
                {
                    ability.InsertTargets(FilterTargets(ability.UnitModelOwner, ability.MainTrigger, ability.GenericParameters,
                                                        GetTargets(ability.UnitModelOwner, ability.Combination, ability.CardAbilityData, new List<BoardObject>()
                    {
                        attacker
                    }, true)));
                    ability.DoAction(new List<GenericParameter>() { new GenericParameter(Enumerators.AbilityParameter.Damage, damage) });
                }
            }
        }

        private void CheckOnEntryAbility(ICardAbility cardAbility)
        {
            if (HasTrigger(cardAbility, Enumerators.AbilityTrigger.Entry) ||
                HasTrigger(cardAbility, Enumerators.AbilityTrigger.EntryWithSelection))
            {
                if (!HasSubTrigger(cardAbility, Enumerators.AbilitySubTrigger.Delay) &&
                    CheckSubTriggersToProceed(cardAbility))
                {
                    cardAbility.DoAction(null);
                }
            }
        }

        private void CheckOnPermanentAbility(ICardAbility cardAbility)
        {
            if (HasTrigger(cardAbility, Enumerators.AbilityTrigger.Permanent))
            {
                cardAbility.DoAction(null);
            }
        }

        private void HandleAbilitiesWithDelaySubTrigger()
        {
            IEnumerable<ICardAbility> list = _activeAbilities[Enumerators.AbilityTrigger.Entry].
                                                Concat(_activeAbilities[Enumerators.AbilityTrigger.EntryWithSelection]);

            foreach (ICardAbility ability in list)
            {
                if (HasSubTrigger(ability, Enumerators.AbilitySubTrigger.Delay))
                {
                    ability.IncreaseTurnsOnBoard();

                    if (TryGetParameterValue(ability.GenericParameters, Enumerators.AbilityParameter.Delay, out int delay))
                    {
                        if (ability.TurnsOnBoard == delay)
                        {
                            ability.DoAction(null);
                        }
                    }
                }
            }
        }

        #endregion


        #region event handlers

        private void UnitDiedHandler(BoardUnitModel boardUnit)
        {
            foreach (ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.Death])
            {
                if (ability.UnitModelOwner == boardUnit)
                {
                    ability.DoAction(null);
                }
            }

            foreach(ICardAbility ability in GetAbilitiesOnUnit(boardUnit))
            {
                ability.Dispose();
            }
        }

        private void TurnEndedHandler()
        {
            foreach(ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.End])
            {
                ability.DoAction(null);
            }
        }

        private void TurnStartedHandler()
        {
            foreach (ICardAbility ability in _activeAbilities[Enumerators.AbilityTrigger.Turn])
            {
                ability.DoAction(null);
            }

            HandleAbilitiesWithDelaySubTrigger();
        }

        private void HandChangedHandler(Player handOwner)
        {

        }

        private void BoardChangedHandler(Player boardOwner)
        {

        }

        private void CardPlayedHandler(BoardUnitModel boardUnit, int position)
        {

        }

        #endregion

        #region tools

        public List<ICardAbility> GetAbilitiesOnUnit(BoardUnitModel unitModel)
        {
            IEnumerable<List<ICardAbility>> abilities = _activeAbilities.Values.Select(item => item.FindAll(x => x.UnitModelOwner == unitModel));

            List<ICardAbility> filteredAbilities = new List<ICardAbility>();

            foreach (List<ICardAbility> list in abilities)
            {
                filteredAbilities.AddRange(list);
            }

            return filteredAbilities;
        }

        public bool HasSpecialTrigger(BoardUnitModel unitModel, Enumerators.AbilityTrigger abilityTrigger)
        {
            foreach (CardAbilitiesCombination combination in unitModel.Card.Prototype.Abilities)
            {
                foreach (CardAbilityData data in combination.CardAbilities)
                {
                    if (data.Triggers != null)
                    {
                        foreach (CardAbilityData.TriggerInfo trigger in data.Triggers)
                        {
                            if (trigger.Trigger == abilityTrigger)
                                return true;
                        }
                    }
                }

                if (combination.DefaultTriggers != null)
                {
                    foreach (CardAbilityData.TriggerInfo trigger in combination.DefaultTriggers)
                    {
                        if (trigger.Trigger == abilityTrigger)
                            return true;
                    }
                }
            }

            return false;
        }

        public bool HasSubTrigger(ICardAbility cardAbility, Enumerators.AbilitySubTrigger subTrigger)
        {
            List<CardAbilityData.TriggerInfo> triggerInfos = GetAllTriggers(cardAbility);

            if(triggerInfos == null)
                return false;

            CardAbilityData.TriggerInfo triggerInfo = triggerInfos.FirstOrDefault(trig => trig.Trigger == cardAbility.MainTrigger.Trigger);

            if (triggerInfo == default(CardAbilityData.TriggerInfo) || triggerInfo.SubTriggers == null)
                return false;

            return triggerInfo.SubTriggers.Contains(subTrigger);
        }

        public bool HasTrigger(ICardAbility cardAbility, Enumerators.AbilityTrigger trigger)
        {
            return HasTrigger(GetAllTriggers(cardAbility.Combination, cardAbility.CardAbilityData), trigger);
        }

        public bool HasTrigger(List<CardAbilityData.TriggerInfo> triggers, Enumerators.AbilityTrigger trigger)
        {
            return triggers?.FindAll(trig => trig.Trigger == trigger).Count > 0;
        }

        public bool TryGetTrigger(
            List<CardAbilityData.TriggerInfo> triggers,
            Enumerators.AbilityTrigger trigger,
            out CardAbilityData.TriggerInfo triggerInfo)
        {
            if (!HasTrigger(triggers, trigger))
            {
                triggerInfo = default(CardAbilityData.TriggerInfo);
                return false;
            }

            triggerInfo = triggers.Find(trig => trig.Trigger == trigger);

            return true;
        }

        #region get combination of default and seted data in card abilities

        public List<GenericParameter> GetAllGenericParameters(CardAbilitiesCombination combination, CardAbilityData cardAbilityData)
        {
            if (cardAbilityData.GenericParameters == null &&
                combination.DefaultGenericParameters == null)
                return null;

            List<GenericParameter> genericParameters = new List<GenericParameter>();

            if (cardAbilityData.GenericParameters != null)
            {
                genericParameters.AddRange(cardAbilityData.GenericParameters);
            }

            if (combination.DefaultGenericParameters != null)
            {
                genericParameters.AddRange(combination.DefaultGenericParameters);
            }

            return genericParameters;
        }

        public List<CardAbilityData.TriggerInfo> GetAllTriggers(CardAbilitiesCombination combination, CardAbilityData cardAbilityData)
        {
            if (cardAbilityData.GenericParameters == null &&
                combination.DefaultGenericParameters == null)
                return null;

            List<CardAbilityData.TriggerInfo> triggers = new List<CardAbilityData.TriggerInfo>();

            if (cardAbilityData.Triggers != null)
            {
                triggers.AddRange(cardAbilityData.Triggers);
            }

            if (combination.DefaultTriggers != null)
            {
                triggers.AddRange(combination.DefaultTriggers);
            }

            return triggers;
        }

        public List<CardAbilityData.TargetInfo> GetAllTargets(CardAbilitiesCombination combination, CardAbilityData cardAbilityData)
        {
            if (cardAbilityData.Targets == null &&
                combination.DefaultTargets == null)
                return null;

            List<CardAbilityData.TargetInfo> targets = new List<CardAbilityData.TargetInfo>();

            if (cardAbilityData.Targets != null)
            {
                targets.AddRange(cardAbilityData.Targets);
            }

            if (combination.DefaultTargets != null)
            {
                targets.AddRange(combination.DefaultTargets);
            }

            return targets;
        }

        public List<GenericParameter> GetAllGenericParameters(ICardAbility cardAbility)
        {
            return GetAllGenericParameters(cardAbility.Combination, cardAbility.CardAbilityData);
        }

        public List<CardAbilityData.TriggerInfo> GetAllTriggers(ICardAbility cardAbility)
        {
            return GetAllTriggers(cardAbility.Combination, cardAbility.CardAbilityData);

        }

        public List<CardAbilityData.TargetInfo> GetAllTargets(ICardAbility cardAbility)
        {
            return GetAllTargets(cardAbility.Combination, cardAbility.CardAbilityData);
        }

        #endregion

        #region Sub triggers handling

        private bool CheckSubTriggersToProceed(ICardAbility cardAbility)
        {
            List<CardAbilityData.TriggerInfo> triggersInfos = GetAllTriggers(cardAbility);

            if (triggersInfos != null)
            {
                foreach (CardAbilityData.TriggerInfo trigger in triggersInfos)
                {
                    if (trigger.SubTriggers != null)
                    {
                        foreach (Enumerators.AbilitySubTrigger subTrigger in trigger.SubTriggers)
                        {
                            switch (subTrigger)
                            {
                                default: return true;
                            }
                        }
                    }
                }
            }

            return true;
        }

        #endregion

        #region targets filtering

        public List<BoardObject> GetTargets(
        BoardUnitModel modelCaller,
        CardAbilitiesCombination combination,
        CardAbilityData cardAbilityData,
        List<BoardObject> targets,
        bool insert = false)
        {
            if (targets != null && !insert)
                return targets;

            targets = targets ?? new List<BoardObject>();

            List<CardAbilityData.TargetInfo> targetsInfo = cardAbilityData.Targets;

            if (targetsInfo == null)
            {
                targetsInfo = combination.DefaultTargets;
            }

            if (targetsInfo != null)
            {
                foreach (CardAbilityData.TargetInfo targetInfo in targetsInfo)
                {
                    switch (targetInfo.Target)
                    {
                        case Enumerators.Target.ItSelf:
                            targets.Add(modelCaller);
                            break;
                        case Enumerators.Target.Opponent:
                            targets.Add(_battlegroundController.GetOpponentPlayerForUnit(modelCaller));
                            break;
                        case Enumerators.Target.Player:
                            targets.Add(modelCaller.OwnerPlayer);
                            break;
                        case Enumerators.Target.PlayerCard:
                            switch (targetInfo.TargetFilter)
                            {
                                case Enumerators.TargetFilter.TargetAdjustments:
                                    if (targets.Count > 0 && targets[0] is BoardUnitModel unitModel)
                                    {
                                        targets.AddRange(_battlegroundController.GetAdjacentUnitsToUnit(unitModel));
                                    }
                                    break;
                                case Enumerators.TargetFilter.FromBoard:
                                    targets.AddRange(modelCaller.OwnerPlayer.PlayerCardsController.CardsOnBoard);
                                    break;
                                case Enumerators.TargetFilter.FromHand:
                                    targets.AddRange(modelCaller.OwnerPlayer.PlayerCardsController.CardsInHand);
                                    break;
                                case Enumerators.TargetFilter.FromDeck:
                                    targets.AddRange(modelCaller.OwnerPlayer.PlayerCardsController.CardsInDeck);
                                    break;
                                case Enumerators.TargetFilter.FromGraveyard:
                                    targets.AddRange(modelCaller.OwnerPlayer.PlayerCardsController.CardsInGraveyard);
                                    break;
                            }
                            break;
                        case Enumerators.Target.OpponentCard:
                            switch (targetInfo.TargetFilter)
                            {
                                case Enumerators.TargetFilter.TargetAdjustments:
                                    if (targets.Count > 0 && targets[0] is BoardUnitModel unitModel)
                                    {
                                        targets.AddRange(_battlegroundController.GetAdjacentUnitsToUnit(unitModel));
                                    }
                                    break;
                                case Enumerators.TargetFilter.FromBoard:
                                    targets.AddRange(_battlegroundController.GetOpponentPlayerForUnit(modelCaller).PlayerCardsController.CardsOnBoard);
                                    break;
                                case Enumerators.TargetFilter.FromHand:
                                    targets.AddRange(_battlegroundController.GetOpponentPlayerForUnit(modelCaller).PlayerCardsController.CardsInHand);
                                    break;
                                case Enumerators.TargetFilter.FromDeck:
                                    targets.AddRange(_battlegroundController.GetOpponentPlayerForUnit(modelCaller).PlayerCardsController.CardsInDeck);
                                    break;
                                case Enumerators.TargetFilter.FromGraveyard:
                                    targets.AddRange(_battlegroundController.GetOpponentPlayerForUnit(modelCaller).PlayerCardsController.CardsInGraveyard);
                                    break;
                            }
                            break;
                        case Enumerators.Target.All:
                            targets.Add(_gameplayManager.CurrentPlayer);
                            targets.Add(_gameplayManager.OpponentPlayer);
                            targets.AddRange(_gameplayManager.CurrentPlayer.PlayerCardsController.CardsOnBoard);
                            targets.AddRange(_gameplayManager.OpponentPlayer.PlayerCardsController.CardsOnBoard);
                            break;
                    }
                }
            }
            else
            {
                Log.Warn($"Ability {cardAbilityData.Ability} havent targets on card {modelCaller.InstanceId}");
            }

            return targets;
        }

        public List<BoardObject> FilterTargets(
            BoardUnitModel boardUnitModel,
            CardAbilityData.TriggerInfo trigger,
            IReadOnlyList<GenericParameter> genericParameters,
            List<BoardObject> targets)
        {
            List<BoardObject> filteredTargets = new List<BoardObject>();

            if (TryGetParameterValue(genericParameters,
                                     Enumerators.AbilityParameter.TargetFaction,
                                     out Enumerators.Faction targetFaction))
            {
                foreach (BoardObject target in targets)
                {
                    switch (target)
                    {
                        case BoardUnitModel unitModel:
                            if (unitModel.Faction != targetFaction)
                            {
                                filteredTargets.Add(target);
                            }
                            break;
                        case Player player:
                            if (player.SelfOverlord.Faction != targetFaction)
                            {
                                filteredTargets.Add(target);
                            }
                            break;
                    }
                }
            }


            if (TryGetParameterValue(genericParameters,
                                     Enumerators.AbilityParameter.TargetStatus,
                                     out Enumerators.UnitSpecialStatus targetStatus))
            {
                foreach (BoardObject target in targets)
                {
                    switch (target)
                    {
                        case BoardUnitModel unitModel:
                            if (unitModel.UnitSpecialStatus != targetStatus)
                            {
                                filteredTargets.Add(target);
                            }
                            break;
                    }
                }
            }

            if (TryGetParameterValue(genericParameters,
                         Enumerators.AbilityParameter.TargetType,
                         out Enumerators.CardType targetType))
            {
                foreach (BoardObject target in targets)
                {
                    switch (target)
                    {
                        case BoardUnitModel unitModel:
                            if (unitModel.InitialUnitType != targetType)
                            {
                                filteredTargets.Add(target);
                            }
                            break;
                    }
                }
            }

            filteredTargets.ForEach((target) => { targets.Remove(target); });
            filteredTargets.Clear();

            if (trigger.SubTriggers != null)
            {
                foreach (Enumerators.AbilitySubTrigger subTrigger in trigger.SubTriggers)
                {
                    switch (subTrigger)
                    {
                        case Enumerators.AbilitySubTrigger.RandomUnit:
                            if (TryGetParameterValue(genericParameters, Enumerators.AbilityParameter.Count, out int countParameter))
                            {
                                filteredTargets.AddRange(targets);

                                for (int i = 0; i < countParameter; i++)
                                {
                                    if (i < filteredTargets.Count)
                                    {
                                        filteredTargets.RemoveAt(MTwister.IRandom(0, filteredTargets.Count));
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            filteredTargets.ForEach((target) => { targets.Remove(target); });
            filteredTargets.Clear();

            return targets;
        }

        #endregion

        #region parameter tools

        public T GetParameterValue<T>(
            IReadOnlyList<GenericParameter> genericParameters,
            Enumerators.AbilityParameter abilityParameter)
        {
            GenericParameter parameter = genericParameters?.FirstOrDefault(param => param.AbilityParameter == abilityParameter);

            if(parameter == null)
            {
                Log.Warn($"Parameter {abilityParameter} doesnt exists in genericParameters: {genericParameters}");
                return default(T);
            }

            if(typeof(T).IsEnum && parameter.Value != null && parameter.Value is string value)
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }

            return (T)Convert.ChangeType(parameter.Value, typeof(T));
        }

        public bool HasParameter(
            IReadOnlyList<GenericParameter> genericParameters,
            Enumerators.AbilityParameter abilityParameter)
        {
            return genericParameters?.FindAll(param => param.AbilityParameter == abilityParameter).Count > 0;
        }

        public bool TryGetParameterValue<T>(
            IReadOnlyList<GenericParameter> genericParameters,
            Enumerators.AbilityParameter abilityParameter,
            out T result)
        {
            if (!HasParameter(genericParameters, abilityParameter))
            {
                result = default(T);
                return false;
            }

            result = GetParameterValue<T>(genericParameters, abilityParameter);

            return true;
        }

        public T GetParameterValue<T>(
            IReadOnlyList<VfxParameter> vfxParameters,
            Enumerators.AbilityEffectType effectType,
            Enumerators.AbilityEffectParameter effectParameter)
        {
            VfxParameter.VfxParameterInfo parameter = vfxParameters.FirstOrDefault(item => item.EffectType == effectType).
                Parameters.FirstOrDefault(param => param.EffectParameter == effectParameter);

            if (typeof(T).IsEnum && parameter.Value != null && parameter.Value is string value)
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }

            return (T)Convert.ChangeType(parameter.Value, typeof(T));
        }

        public bool HasParameter(
            IReadOnlyList<VfxParameter> vfxParameters,
            Enumerators.AbilityEffectType effectType,
            Enumerators.AbilityEffectParameter effectParameter)
        {
            return vfxParameters.FirstOrDefault(item => item.EffectType == effectType).
                Parameters.FindAll(param => param.EffectParameter == effectParameter).Count > 0;
        }

        public bool TryGetParameterValue<T>(
            IReadOnlyList<VfxParameter> vfxParameters,
            Enumerators.AbilityEffectType effectType,
            Enumerators.AbilityEffectParameter effectParameter,
            out T result)
        {
            if (!HasParameter(vfxParameters, effectType, effectParameter))
            {
                result = default(T);
                return false;
            }

            result = GetParameterValue<T>(vfxParameters, effectType, effectParameter);

            return true;
        }

        #endregion

        #endregion

    }
}
