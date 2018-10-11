using System;
using DG.Tweening;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Loom.ZombieBattleground
{
    public class AnimationsController : IController
    {
        private IGameplayManager _gameplayManager;

        private ITimerManager _timerManager;

        private BattlegroundController _battlegroundController;

        public void Dispose()
        {
        }

        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _timerManager = GameClient.Get<ITimerManager>();

            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();
        }

        public void Update()
        {
        }

        public void ResetAll()
        {
        }

        public void DoFightAnimation(
            GameObject source,
            GameObject target,
            float shakeStrength,
            Action onHitCallback,
            Action onCompleteCallback = null,
            bool isCreatureAttacker = true,
            float duration = 0.5f)
        {
            Vector3 originalPos = source.transform.position;

            SortingGroup sortingGroup = source.GetComponent<SortingGroup>();
            int oldSortingOrder = sortingGroup.sortingOrder;
            string oldsortingLayerName = sortingGroup.sortingLayerName;

            sortingGroup.sortingLayerID = SRSortingLayers.BoardCards;
            sortingGroup.sortingOrder = 1000;

            Vector3 partWay;

            if (isCreatureAttacker)
            {
                partWay = Vector3.Lerp(originalPos + Vector3.back * 5f, target.transform.position + Vector3.back * 5f, 0.6f);
            }
            else
            {
                partWay = Vector3.Lerp(originalPos + Vector3.back * 5f, target.transform.position + Vector3.back * 5f, 0.7f);
            }

            source.transform.DOMove(partWay, 0.10f).SetEase(Ease.InSine).OnComplete(
                () =>
                {
                    DOTween.Sequence().Append(target.GetComponent<Image>().DOColor(Color.red, 0.25f))
                        .Append(target.GetComponent<Image>().DOColor(Color.white, 0.25f)).Play();

                    target.transform.DOShakePosition(1, new Vector3(shakeStrength, shakeStrength, 0));

                    source.transform.DOMove(originalPos, duration).SetEase(Ease.OutSine).OnComplete(
                        () =>
                        {
                            InternalTools.DoActionDelayed(() =>
                            {
                                onCompleteCallback?.Invoke();
                            }, 0.5f);

                            sortingGroup.sortingOrder = oldSortingOrder;
                            sortingGroup.sortingLayerName = oldsortingLayerName;
                        });

                    onHitCallback?.Invoke();
                });
        }

        public void MoveCardFromPlayerDeckToPlayerHandAnimation(Player fromDeck, Player toHand, BoardCard boardCard)
        {
            boardCard.DrawCardFromOpponentDeckToPlayer();
        }

        public void MoveCardFromPlayerDeckToOpponentHandAnimation(Player fromDeck, Player toHand, GameObject boardCard)
        {
            Animator animator = boardCard.GetComponent<Animator>();

            boardCard.transform.localScale = Vector3.zero;
            boardCard.transform.DOScale(new Vector3(0.9f, 0.9f, 0.9f), 0.15f);

            animator.enabled = true;
            animator.StopPlayback();
            animator.Play("MoveCardFromPlayerDeckToOpponentHand");

            _timerManager.AddTimer(
                x =>
                {
                    animator.enabled = false;

                    _battlegroundController.OpponentHandCards.Add(boardCard);

                    _battlegroundController.UpdatePositionOfCardsInOpponentHand(true);
                },
                null,
                1.1f);
        }
    }
}
