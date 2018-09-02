using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class DealDamageToThisAndAdjacentUnitsAbility : AbilityBase
    {
        public int value = 1;

        public DealDamageToThisAndAdjacentUnitsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            value = ability.value;
        }

        public override void Activate()
        {
            base.Activate();

            _vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/toxicDamageVFX");
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void Action(object param = null)
        {
            base.Action(param);

            int targetIndex = -1;
            for (int i = 0; i < playerCallerOfAbility.BoardCards.Count; i++)
            {
                if (playerCallerOfAbility.BoardCards[i] == abilityUnitOwner)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex > -1)
            {
                if (targetIndex - 1 > -1)
                {
                    TakeDamageToUnit(playerCallerOfAbility.BoardCards[targetIndex - 1]);
                }

                if (targetIndex + 1 < playerCallerOfAbility.BoardCards.Count)
                {
                    TakeDamageToUnit(playerCallerOfAbility.BoardCards[targetIndex + 1]);
                }
            }

            TakeDamageToUnit(abilityUnitOwner);
        }

        protected override void OnEndTurnEventHandler()
        {
            base.OnEndTurnEventHandler();

            if (abilityCallType != Enumerators.AbilityCallType.END)

                return;

            Action();
        }

        private void TakeDamageToUnit(BoardUnit unit)
        {
            _battleController.AttackUnitByAbility(abilityUnitOwner, abilityData, unit);
            CreateVFX(unit.transform.position, true, 5f);
        }
    }
}
