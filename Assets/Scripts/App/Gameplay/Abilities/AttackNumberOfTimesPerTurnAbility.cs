﻿// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/


using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;

namespace LoomNetwork.CZB
{
    public class AttackNumberOfTimesPerTurnAbility : AbilityBase
    {
        private int _numberOfAttacksWas = 0;

        public Enumerators.AttackInfoType attackInfo;
        public int value = 1;


        public AttackNumberOfTimesPerTurnAbility(Enumerators.CardKind cardKind, AbilityData ability) : base(cardKind, ability)
        {
            this.value = ability.value;
            this.attackInfo = ability.attackInfoType;
        }

        public override void Activate()
        {
            base.Activate();

            abilityUnitOwner.attackInfoType = this.attackInfo;
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        private void Action()
        {
        }

        protected override void UnitOnAttackEventHandler(object info, int damage, bool isAttacker)
        {
            base.UnitOnAttackEventHandler(info, damage, isAttacker);

            if (!isAttacker)
                return;

            _numberOfAttacksWas++;

            if(_numberOfAttacksWas < value)
                abilityUnitOwner.ForceSetCreaturePlayable();
        }

        protected override void OnStartTurnEventHandler()
        {
            base.OnStartTurnEventHandler();
            _numberOfAttacksWas = 0;
        }
    }
}