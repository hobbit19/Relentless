using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;

namespace Loom.ZombieBattleground.Test
{
    /// <summary>
    /// Convenience wrapper for <see cref="IPlayerActionTestProxy"/> that wraps calls into lambdas and adds the
    /// to a queue to be called in a delayed fashion.
    /// </summary>
    public class QueueProxyPlayerActionTestProxy
    {
        private readonly Func<Queue<Func<Task>>> _getQueueFunc;
        private readonly MatchScenarioPlayer _matchScenarioPlayer;

        protected Queue<Func<Task>> Queue => _getQueueFunc();

        /// <summary>
        /// <see cref="IPlayerActionTestProxy"/> to be used for actions .
        /// </summary>
        public IPlayerActionTestProxy Proxy { get; }

        public QueueProxyPlayerActionTestProxy(MatchScenarioPlayer matchScenarioPlayer, Func<Queue<Func<Task>>> queueFunc, IPlayerActionTestProxy proxy)
        {
            _matchScenarioPlayer = matchScenarioPlayer;
            _getQueueFunc = queueFunc;
            Proxy = proxy;
        }

        public void AbortNextTurns()
        {
            _matchScenarioPlayer.AbortNextMoves();
        }

        public void EndTurn()
        {
            Queue.Enqueue(() => Proxy.EndTurn());
        }

        public void LeaveMatch()
        {
            Queue.Enqueue(() => Proxy.LeaveMatch());
        }

        public void Mulligan(IEnumerable<InstanceId> cards)
        {
            Queue.Enqueue(() => Proxy.Mulligan(cards));
        }

        public void CardPlay(InstanceId card, ItemPosition position, InstanceId? entryAbilityTarget = null)
        {
            Queue.Enqueue(() => Proxy.CardPlay(card, position, entryAbilityTarget));
        }

        public void RankBuff(InstanceId card, IEnumerable<InstanceId> units)
        {
            Queue.Enqueue(() => Proxy.RankBuff(card, units));
        }

        public void CardAbilityUsed(
            InstanceId card,
            int abilityIndex,
            int choosableAbilityIndex = 0,
            IReadOnlyList<ParametrizedAbilityInstanceId> targets = null)
        {
            Queue.Enqueue(() => Proxy.CardAbilityUsed(card, abilityIndex, choosableAbilityIndex, targets));
        }

        public void OverlordSkillUsed(SkillId skillId, Enumerators.AffectObjectType affectObjectType, InstanceId targetInstanceId)
        {
            Queue.Enqueue(() => Proxy.OverlordSkillUsed(skillId, affectObjectType, targetInstanceId));
        }

        public void CardAttack(InstanceId attacker, Enumerators.AffectObjectType type, InstanceId target)
        {
            Queue.Enqueue(() => Proxy.CardAttack(attacker, type, target));
        }

        public void CheatDestroyCardsOnBoard(IEnumerable<Data.InstanceId> targets)
        {
            Queue.Enqueue(() => Proxy.CheatDestroyCardsOnBoard(targets));
        }
    }
}
