using System;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class AbilityBase
    {
        public ulong ActivityId;

        public Enumerators.AbilityActivityType AbilityActivityType;

        public Enumerators.AbilityCallType AbilityCallType;

        public Enumerators.AffectObjectType AffectObjectType;

        public Enumerators.AbilityEffectType AbilityEffectType;

        public Enumerators.CardType TargetCardType = Enumerators.CardType.NONE;

        public Enumerators.UnitStatusType TargetUnitStatusType = Enumerators.UnitStatusType.NONE;

        public List<Enumerators.AbilityTargetType> AbilityTargetTypes;

        public Enumerators.CardKind CardKind;

        public Card CardOwnerOfAbility;

        public WorkingCard MainWorkingCard;

        public BoardUnitModel AbilityUnitOwner;

        public Player PlayerCallerOfAbility;

        public BoardSpell BoardSpell;

        public BoardCard BoardCard;

        public BoardUnitModel TargetUnit;

        public Player TargetPlayer;

        public Player SelectedPlayer;

        protected AbilitiesController AbilitiesController;

        protected ParticlesController ParticlesController;

        protected BattleController BattleController;

        protected ActionsQueueController ActionsQueueController;

        protected BattlegroundController BattlegroundController;

        protected CardsController CardsController;

        protected ILoadObjectsManager LoadObjectsManager;

        protected IGameplayManager GameplayManager;

        protected IDataManager DataManager;

        protected ITimerManager TimerManager;

        protected ISoundManager SoundManager;

        protected GameObject VfxObject;

        protected bool IsAbilityResolved;

        protected Action OnObjectSelectedByTargettingArrowCallback;

        protected Action OnObjectSelectFailedByTargettingArrowCallback;

        protected List<ulong> ParticleIds;

        private readonly Player _playerAvatar;

        private readonly Player _opponenentAvatar;

        public AbilityBase(Enumerators.CardKind cardKind, AbilityData ability)
        {
            LoadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            GameplayManager = GameClient.Get<IGameplayManager>();
            DataManager = GameClient.Get<IDataManager>();
            TimerManager = GameClient.Get<ITimerManager>();
            SoundManager = GameClient.Get<ISoundManager>();

            AbilitiesController = GameplayManager.GetController<AbilitiesController>();
            ParticlesController = GameplayManager.GetController<ParticlesController>();
            BattleController = GameplayManager.GetController<BattleController>();
            ActionsQueueController = GameplayManager.GetController<ActionsQueueController>();
            BattlegroundController = GameplayManager.GetController<BattlegroundController>();
            CardsController = GameplayManager.GetController<CardsController>();

            AbilityData = ability;
            CardKind = cardKind;
            AbilityActivityType = ability.AbilityActivityType;
            AbilityCallType = ability.AbilityCallType;
            AbilityTargetTypes = ability.AbilityTargetTypes;
            AbilityEffectType = ability.AbilityEffectType;
            _playerAvatar = GameplayManager.CurrentPlayer;
            _opponenentAvatar = GameplayManager.OpponentPlayer;

            PermanentInputEndEvent += InputEndedHandler;

            ParticleIds = new List<ulong>();
        }

        protected event Action PermanentInputEndEvent;

        public event Action<object> ActionTriggered;

        public event Action Disposed;

        public AbilityBoardArrow TargettingArrow { get; protected set; }

        public AbilityData AbilityData { get; protected set; }

        public void ActivateSelectTarget(
            List<Enumerators.SkillTargetType> targetsType = null, Action callback = null, Action failedCallback = null)
        {
            OnObjectSelectedByTargettingArrowCallback = callback;
            OnObjectSelectFailedByTargettingArrowCallback = failedCallback;

            BoardUnitView abilityUnitOwnerView = GetAbilityUnitOwnerView();

            TargettingArrow =
                Object
                .Instantiate(LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Arrow/AttackArrowVFX_Object"))
                .AddComponent<AbilityBoardArrow>();
            TargettingArrow.PossibleTargets = AbilityTargetTypes;
            TargettingArrow.SelfBoardCreature = abilityUnitOwnerView;
            TargettingArrow.TargetUnitType = TargetCardType;
            TargettingArrow.TargetUnitStatusType = TargetUnitStatusType;

            switch (CardKind)
            {
                case Enumerators.CardKind.CREATURE:
                    TargettingArrow.Begin(abilityUnitOwnerView.Transform.position);
                    break;
                case Enumerators.CardKind.SPELL:
                    TargettingArrow.Begin(SelectedPlayer.AvatarObject.transform.position);
                    break;
                default:
                    TargettingArrow.Begin(PlayerCallerOfAbility.AvatarObject.transform.position);
                    break;
            }

            TargettingArrow.CardSelected += CardSelectedHandler;
            TargettingArrow.CardUnselected += CardUnselectedHandler;
            TargettingArrow.PlayerSelected += PlayerSelectedHandler;
            TargettingArrow.PlayerUnselected += PlayerUnselectedHandler;
            TargettingArrow.InputEnded += InputEndedHandler;
            TargettingArrow.InputCanceled += InputCanceledHandler;
        }

        public void DeactivateSelectTarget()
        {
            if (TargettingArrow != null)
            {
                TargettingArrow.CardSelected -= CardSelectedHandler;
                TargettingArrow.CardUnselected -= CardUnselectedHandler;
                TargettingArrow.PlayerSelected -= PlayerSelectedHandler;
                TargettingArrow.PlayerUnselected -= PlayerUnselectedHandler;
                TargettingArrow.InputEnded -= InputEndedHandler;
                TargettingArrow.InputCanceled -= InputCanceledHandler;

                TargettingArrow.Dispose();
                TargettingArrow = null;
            }
        }

        public virtual void Activate()
        {
            PlayerCallerOfAbility.TurnEnded += TurnEndedHandler;
            PlayerCallerOfAbility.TurnStarted += TurnStartedHandler;

            switch (CardKind)
            {
                case Enumerators.CardKind.CREATURE:
                    if (AbilityUnitOwner != null)
                    {
                        AbilityUnitOwner.UnitDied += UnitDiedHandler;
                        AbilityUnitOwner.UnitAttacked += UnitAttackedHandler;
                        AbilityUnitOwner.UnitHpChanged += UnitHpChangedHandler;
                        AbilityUnitOwner.UnitDamaged += UnitDamagedHandler;
                    }

                    break;
                case Enumerators.CardKind.SPELL:
                    if (BoardSpell != null)
                    {
                        BoardSpell.Used += UsedHandler;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(CardKind), CardKind, null);
            }

            SelectedPlayer = PlayerCallerOfAbility.IsLocalPlayer ? _playerAvatar : _opponenentAvatar;
        }

        public virtual void Update()
        {
        }

        public virtual void Dispose()
        {
            PlayerCallerOfAbility.TurnEnded -= TurnEndedHandler;
            PlayerCallerOfAbility.TurnStarted -= TurnStartedHandler;

            DeactivateSelectTarget();
            ClearParticles();

            Disposed?.Invoke();
        }

        public virtual void SelectedTargetAction(bool callInputEndBefore = false)
        {
            if (callInputEndBefore)
            {
                PermanentInputEndEvent?.Invoke();
                return;
            }

            if (TargetUnit != null)
            {
                AffectObjectType = Enumerators.AffectObjectType.CHARACTER;
            }
            else if (TargetPlayer != null)
            {
                AffectObjectType = Enumerators.AffectObjectType.PLAYER;
            }
            else
            {
                AffectObjectType = Enumerators.AffectObjectType.NONE;
            }

            if (AffectObjectType != Enumerators.AffectObjectType.NONE)
            {
                IsAbilityResolved = true;

                OnObjectSelectedByTargettingArrowCallback?.Invoke();
                OnObjectSelectedByTargettingArrowCallback = null;
            }
            else
            {
                OnObjectSelectFailedByTargettingArrowCallback?.Invoke();
                OnObjectSelectFailedByTargettingArrowCallback = null;
            }
        }

        public virtual void Action(object info = null)
        {
        }

        protected virtual void CardSelectedHandler(BoardUnitView obj)
        {
            TargetUnit = obj.Model;

            TargetPlayer = null;
        }

        protected virtual void CardUnselectedHandler(BoardUnitView obj)
        {
            TargetUnit = null;
        }

        protected virtual void PlayerSelectedHandler(Player obj)
        {
            TargetPlayer = obj;

            TargetUnit = null;
        }

        protected virtual void PlayerUnselectedHandler(Player obj)
        {
            TargetPlayer = null;
        }

        protected virtual void CreateVfx(
            Vector3 pos, bool autoDestroy = false, float duration = 3f, bool justPosition = false)
        {
            // todo make it async
            if (VfxObject != null)
            {
                VfxObject = Object.Instantiate(VfxObject);

                if (!justPosition)
                {
                    VfxObject.transform.position = pos - Constants.VfxOffset + Vector3.forward;
                }
                else
                {
                    VfxObject.transform.position = pos;
                }

                ulong id = ParticlesController.RegisterParticleSystem(VfxObject, autoDestroy, duration);

                if (!autoDestroy)
                {
                    ParticleIds.Add(id);
                }
            }
        }

        protected virtual void InputEndedHandler()
        {
            SelectedTargetAction();
            DeactivateSelectTarget();
        }

        protected virtual void InputCanceledHandler()
        {
            OnObjectSelectFailedByTargettingArrowCallback?.Invoke();
            OnObjectSelectFailedByTargettingArrowCallback = null;

            DeactivateSelectTarget();
        }

        protected virtual void TurnEndedHandler()
        {
            if (TargettingArrow != null)
            {
                InputEndedHandler();
            }
        }

        protected virtual void TurnStartedHandler()
        {
        }

        protected virtual void UnitDiedHandler()
        {
            AbilityUnitOwner.UnitDied -= UnitDiedHandler;
            AbilityUnitOwner.UnitAttacked -= UnitAttackedHandler;
            AbilityUnitOwner.UnitHpChanged -= UnitHpChangedHandler;
            AbilityUnitOwner.UnitDamaged -= UnitDamagedHandler;

            AbilitiesController.DeactivateAbility(ActivityId);
            Dispose();
        }

        protected virtual void UnitAttackedHandler(BoardObject info, int damage, bool isAttacker)
        {
        }

        protected virtual void UnitHpChangedHandler()
        {
        }

        protected virtual void UnitDamagedHandler(BoardObject from)
        {
        }

        protected void UsedHandler()
        {
            BoardSpell.Used -= UsedHandler;
        }

        protected void ClearParticles()
        {
            foreach (ulong id in ParticleIds)
            {
                ParticlesController.DestroyParticle(id);
            }
        }

        protected object GetCaller()
        {
            return AbilityUnitOwner ?? (object) BoardSpell;
        }

        protected Player GetOpponentOverlord()
        {
            return PlayerCallerOfAbility.Equals(GameplayManager.CurrentPlayer) ?
                GameplayManager.OpponentPlayer :
                GameplayManager.CurrentPlayer;
        }

        protected BoardUnitView GetAbilityUnitOwnerView()
        {
            return BattlegroundController.GetBoardUnitViewByModel(AbilityUnitOwner);
        }

        protected void InvokeActionTriggered(object info = null)
        {
            ActionTriggered?.Invoke(info);
        }
    }
}
