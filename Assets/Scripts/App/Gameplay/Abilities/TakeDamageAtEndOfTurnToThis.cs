using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class TakeDamageAtEndOfTurnToThis : AbilityBase
    {
        public int Value { get; }

        public TakeDamageAtEndOfTurnToThis(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Value = ability.Value;
        }

        public override void Activate()
        {
            base.Activate();

            VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/toxicDamageVFX");

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>(), AbilityData.AbilityType, Protobuf.AffectObjectType.Types.Enum.Character);
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            BattleController.AttackUnitByAbility(AbilityUnitOwner, AbilityData, AbilityUnitOwner);
            CreateVfx(GetAbilityUnitOwnerView().Transform.position, true, 5f);
        }

        protected override void TurnEndedHandler()
        {
            base.TurnEndedHandler();

            if (!GameplayManager.CurrentTurnPlayer.Equals(PlayerCallerOfAbility))
                return;

            if (AbilityCallType != Enumerators.AbilityCallType.END)
                return;

            Action();
        }
    }
}
