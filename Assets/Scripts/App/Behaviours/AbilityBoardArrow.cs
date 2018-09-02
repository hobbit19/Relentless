using System;
using System.Collections.Generic;
using LoomNetwork.CZB.Common;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class AbilityBoardArrow : BoardArrow
    {
        public event Action<BoardUnit> OnCardSelectedEvent;

        public event Action<BoardUnit> OnCardUnselectedevent;

        public event Action<Player> OnPlayerSelectedEvent;

        public event Action<Player> OnPlayerUnselectedEvent;

        public event Action OnInputEndEvent;

        public event Action OnInputCancelEvent;

        public List<Enumerators.AbilityTargetType> possibleTargets = new List<Enumerators.AbilityTargetType>();

        public BoardUnit selfBoardCreature;

        public Enumerators.CardType targetUnitType;

        public Enumerators.UnitStatusType targetUnitStatusType;

        private IInputManager _inputManager;

        private int _onMouseDownInputIndex;

        private int _onRightMouseDownInputIndex;

        private int _onEscapeInputIndex;

        private bool _isAbilitySecondTouchActivated = false;

        public override void OnCardSelected(BoardUnit unit)
        {
            if (unit.CurrentHP <= 0)

                return;

            if ((possibleTargets.Contains(Enumerators.AbilityTargetType.PLAYER_CARD) && unit.gameObject.CompareTag(Constants.TAG_PLAYER_OWNED)) || (possibleTargets.Contains(Enumerators.AbilityTargetType.OPPONENT_CARD) && unit.gameObject.CompareTag(Constants.TAG_OPPONENT_OWNED)) || possibleTargets.Contains(Enumerators.AbilityTargetType.ALL))
            {
                if ((targetUnitType == Enumerators.CardType.NONE) || (unit.InitialUnitType == targetUnitType))
                {
                    if ((targetUnitStatusType == Enumerators.UnitStatusType.NONE) || (unit.UnitStatus == targetUnitStatusType))
                    {
                        if (selfBoardCreature != unit)
                        {
                            if (selectedCard != null)
                            {
                                selectedCard.SetSelectedUnit(false);
                            }

                            selectedCard = unit;
                            if (selectedPlayer != null)
                            {
                                selectedPlayer.SetGlowStatus(false);
                            }

                            selectedPlayer = null;
                            CreateTarget(unit.transform.position);
                            selectedCard.SetSelectedUnit(true);

                            OnCardSelectedEvent?.Invoke(unit);
                        }
                    }
                }
            }
        }

        public override void OnCardUnselected(BoardUnit creature)
        {
            if (selectedCard == creature)
            {
                selectedCard.SetSelectedUnit(false);

                // _targetObjectsGroup.SetActive(false);
                OnCardUnselectedevent?.Invoke(creature);
            }

            selectedCard = null;
        }

        public override void OnPlayerSelected(Player player)
        {
            if (player.HP <= 0)

                return;

            if ((possibleTargets.Contains(Enumerators.AbilityTargetType.PLAYER) && player.AvatarObject.CompareTag(Constants.TAG_PLAYER_OWNED)) || (possibleTargets.Contains(Enumerators.AbilityTargetType.OPPONENT) && player.AvatarObject.CompareTag(Constants.TAG_OPPONENT_OWNED)) || possibleTargets.Contains(Enumerators.AbilityTargetType.ALL))
            {
                selectedPlayer = player;
                if (selectedCard != null)
                {
                    selectedCard.SetSelectedUnit(false);
                }

                selectedCard = null;
                CreateTarget(player.AvatarObject.transform.position);
                selectedPlayer.SetGlowStatus(true);
                OnPlayerSelectedEvent?.Invoke(player);
            }
        }

        public override void OnPlayerUnselected(Player player)
        {
            if (selectedPlayer == player)
            {
                if (selectedCard != null)
                {
                    selectedCard.SetSelectedUnit(false);
                }

                selectedCard = null;

                selectedPlayer.SetGlowStatus(false);

                // _targetObjectsGroup.SetActive(false);
                OnPlayerUnselectedEvent?.Invoke(player);
            }

            selectedPlayer = null;
        }

        protected void Awake()
        {
            _inputManager = GameClient.Get<IInputManager>();

            Init();

            GameClient.Get<ITimerManager>().AddTimer(
                x =>
                {
                    _onMouseDownInputIndex = _inputManager.RegisterInputHandler(Enumerators.InputType.MOUSE, 0, OnMouseButtonUpHandler, null, null);
                    _onRightMouseDownInputIndex = _inputManager.RegisterInputHandler(Enumerators.InputType.MOUSE, 1, OnRightMouseButtonUpHandler, null, null);
                    _onEscapeInputIndex = _inputManager.RegisterInputHandler(Enumerators.InputType.KEYBOARD, (int)KeyCode.Escape, null, OnRightMouseButtonUpHandler, null);
                },
                null,
                Time.fixedDeltaTime);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _inputManager.UnregisterInputHandler(_onMouseDownInputIndex);
            _inputManager.UnregisterInputHandler(_onRightMouseDownInputIndex);
            _inputManager.UnregisterInputHandler(_onEscapeInputIndex);
        }

        protected override void Update()
        {
            base.Update();
        }

        protected void OnMouseButtonUpHandler()
        {
            if (startedDrag)
            {
                startedDrag = false;
                OnInputEndEvent?.Invoke();
            }
        }

        protected void OnRightMouseButtonUpHandler()
        {
            if (startedDrag)
            {
                startedDrag = false;
                OnInputCancelEvent?.Invoke();
            }
        }
    }
}
