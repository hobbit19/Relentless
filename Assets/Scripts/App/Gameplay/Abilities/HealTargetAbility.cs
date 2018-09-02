using System;
using DG.Tweening;
using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;
using LoomNetwork.Internal;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class HealTargetAbility : AbilityBase
    {
        public int Value = 1;

        public HealTargetAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Value = ability.Value;
        }

        public override void Activate()
        {
            base.Activate();

            VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/GreenHealVFX");
        }

        public override void Update()
        {
        }

        public override void Dispose()
        {
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            object caller = AbilityUnitOwner != null?AbilityUnitOwner:(object)BoardSpell;

            switch (AffectObjectType)
            {
                case Enumerators.AffectObjectType.Player:
                    BattleController.HealPlayerByAbility(caller, AbilityData, TargetPlayer);
                    break;
                case Enumerators.AffectObjectType.Character:
                    BattleController.HealUnitByAbility(caller, AbilityData, TargetUnit);
                    break;
            }
        }

        protected override void OnInputEndEventHandler()
        {
            base.OnInputEndEventHandler();

            if (IsAbilityResolved)
            {
                Action();
            }
        }

        private void CreateAndMoveParticle(Action callback, Vector3 target)
        {
            target = Utilites.CastVfxPosition(target);
            if (AbilityEffectType == Enumerators.AbilityEffectType.Heal)
            {
                Vector3 startPosition = CardKind == Enumerators.CardKind.Creature?AbilityUnitOwner.Transform.position:SelectedPlayer.Transform.position;
                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/SpellTargetLifeAttack");

                CreateVfx(startPosition);
                VfxObject.transform.DOMove(target, 0.5f).OnComplete(
                    () =>
                    {
                        ClearParticles();
                        VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/GreenHealVFX");

                        CreateVfx(target, true);
                        callback();
                    });
            }
            else if (AbilityEffectType == Enumerators.AbilityEffectType.HealDirectly)
            {
                CreateVfx(target, true);
                callback();
            }
        }
    }
}
