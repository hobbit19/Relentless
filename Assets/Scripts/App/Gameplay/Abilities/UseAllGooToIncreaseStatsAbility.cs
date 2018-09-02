using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;

namespace LoomNetwork.CZB
{
    public class UseAllGooToIncreaseStatsAbility : AbilityBase
    {
        public int value;

        public UseAllGooToIncreaseStatsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            value = ability.value;
        }

        public override void Activate()
        {
            base.Activate();

            if (abilityCallType != Enumerators.AbilityCallType.ENTRY)

                return;

            Action();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            if (playerCallerOfAbility.Goo == 0)

                return;

            int increaseOn = 0;

            increaseOn = playerCallerOfAbility.Goo * value;
            abilityUnitOwner.BuffedHP += increaseOn;
            abilityUnitOwner.CurrentHP += increaseOn;

            increaseOn = playerCallerOfAbility.Goo * value;
            abilityUnitOwner.BuffedDamage += increaseOn;
            abilityUnitOwner.CurrentDamage += increaseOn;

            playerCallerOfAbility.Goo = 0;
        }

        protected override void OnInputEndEventHandler()
        {
            base.OnInputEndEventHandler();
        }

        protected override void UnitOnAttackEventHandler(object info, int damage, bool isAttacker)
        {
            base.UnitOnAttackEventHandler(info, damage, isAttacker);
        }
    }
}
