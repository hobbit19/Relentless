using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class AttackOverlordAbility : AbilityBase
    {
        public int Value { get; }

        public List<Enumerators.AbilityTargetType> TargetTypes { get; }

        public AttackOverlordAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            TargetTypes = ability.AbilityTargetTypes;
            Value = ability.Value;
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;

            VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/toxicDamageVFX");

            Action();
        }

        public override void Action(object param = null)
        {
            base.Action(param);

            foreach (Enumerators.AbilityTargetType target in TargetTypes)
            {
                switch (target)
                {
                    case Enumerators.AbilityTargetType.OPPONENT:
                        GetOpponentOverlord().Health -= Value;
                        CreateVfx(GetOpponentOverlord().AvatarObject.transform.position, true, 5f, true);
                        break;
                    case Enumerators.AbilityTargetType.PLAYER:
                        PlayerCallerOfAbility.Health -= Value;
                        CreateVfx(PlayerCallerOfAbility.AvatarObject.transform.position, true, 5f, true);
                        break;
                    default: continue;
                }
            }
        }
    }
}
