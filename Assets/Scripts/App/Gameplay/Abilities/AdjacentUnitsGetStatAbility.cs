using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;

namespace Loom.ZombieBattleground
{
    public class AdjacentUnitsGetStatAbility : AbilityBase
    {
        public int Defense { get; }

        public int Damage { get; }

        public AdjacentUnitsGetStatAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Damage = ability.Damage;
            Defense = ability.Defense;
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityActivity == Enumerators.AbilityActivity.PASSIVE)
            {
                InvokeUseAbilityEvent();
            }

            if (AbilityTrigger != Enumerators.AbilityTrigger.ENTRY)
                return;

            ChangeStats(BattlegroundController.GetAdjacentUnitsToUnit(AbilityUnitOwner), Defense, Damage);
        }

        protected override void InputEndedHandler()
        {
            base.InputEndedHandler();

            if (IsAbilityResolved)
            {
                ChangeStats(BattlegroundController.GetAdjacentUnitsToUnit(TargetUnit), Defense, Damage);

                InvokeUseAbilityEvent(new List<ParametrizedAbilityBoardObject>()
                {
                    new ParametrizedAbilityBoardObject(TargetUnit)
                });
            }
        }

        protected override void UnitDiedHandler()
        {
            base.UnitDiedHandler();

            if (AbilityTrigger != Enumerators.AbilityTrigger.DEATH)
                return;

            ChangeStats(BattlegroundController.GetAdjacentUnitsToUnit(AbilityUnitOwner), Defense, Damage);
        }

        protected override void ChangeAuraStatusAction(bool status)
        {
            base.ChangeAuraStatusAction(status);

            if (AbilityTrigger != Enumerators.AbilityTrigger.AURA)
                return;

            if (status)
            {
                ChangeStats(BattlegroundController.GetAdjacentUnitsToUnit(AbilityUnitOwner), Defense, Damage);
            }
            else
            {
                ChangeStats(BattlegroundController.GetAdjacentUnitsToUnit(AbilityUnitOwner), -Defense, -Damage);
            }
        }

        private void ChangeStats(List<CardModel> units, int defense, int damage)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            foreach (CardModel unit in units)
            {
                if (defense != 0)
                {
                    unit.BuffedDefense += defense;
                    unit.CurrentDefense += defense;

                    targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = defense >= 0 ? Enumerators.ActionEffectType.ShieldBuff : Enumerators.ActionEffectType.ShieldDebuff,
                        Target = unit,
                    });
                }

                if (damage != 0)
                {
                    unit.BuffedDamage += damage;
                    unit.CurrentDamage += damage;

                    targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = damage >= 0 ? Enumerators.ActionEffectType.AttackBuff : Enumerators.ActionEffectType.AttackDebuff,
                        Target = unit
                    });
                }
            }

            ActionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.CardAffectingMultipleCards,
                Caller = AbilityUnitOwner,
                TargetEffects = targetEffects
            });
        }
    }
}
