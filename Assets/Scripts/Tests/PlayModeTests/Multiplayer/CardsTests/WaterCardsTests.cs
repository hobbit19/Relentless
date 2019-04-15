using System;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using UnityEngine.TestTools;
using System.Linq;

namespace Loom.ZombieBattleground.Test.MultiplayerTests
{
    public class WaterCardsTests : BaseIntegrationTest
    {
		[UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Znowman()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zlab", 5),
                    new DeckCardData("Znowman", 1)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zlab", 5),
                    new DeckCardData("Znowman", 1)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerZnowmanId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Znowman", 1);
                InstanceId playerZlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 1);
                InstanceId playerZlab2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 2);

                InstanceId opponentZnowmanId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Znowman", 1);
                InstanceId opponentZlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 1);
                InstanceId opponentZlab2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 2);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {
                           player.CardPlay(playerZnowmanId, ItemPosition.Start);
                           player.CardPlay(playerZlabId, ItemPosition.Start);
                           player.CardPlay(playerZlab2Id, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentZnowmanId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentZnowmanId, Enumerators.AbilityType.ENEMY_THAT_ATTACKS_BECOME_FROZEN, new List<ParametrizedAbilityInstanceId>());
                           opponent.CardAbilityUsed(opponentZnowmanId, Enumerators.AbilityType.STUN, new List<ParametrizedAbilityInstanceId>());
                           opponent.CardPlay(opponentZlabId, ItemPosition.Start);
                           opponent.CardPlay(opponentZlab2Id, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardAttack(playerZlabId, opponentZnowmanId);
                       },
                       opponent =>
                       {
                           opponent.CardAttack(opponentZlab2Id, playerZnowmanId);
                           opponent.CardAttack(opponentZnowmanId, playerZlab2Id);
                       },
                       player =>
                       {
                           player.CardAttack(playerZnowmanId, opponentZlabId);
                           Assert.AreEqual(true, ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlabId)).IsStun);
                           Assert.AreEqual(true, ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlab2Id)).IsStun);
                       },
                       opponent =>
                       {
                           Assert.AreEqual(true, ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlabId)).IsStun);
                           Assert.AreEqual(true, ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlab2Id)).IsStun);
                       },
                };

                Action validateEndState = () => { };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

    
        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Jetter()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Jetter", 2));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Jetter", 2));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerJetter1 = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Jetter", 1);
                InstanceId playerJetter2 = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Jetter", 2);

                InstanceId opponentJetter1 = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Jetter", 1);
                InstanceId opponentJetter2 = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Jetter", 2);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerJetter1, ItemPosition.Start),
                       opponent =>
                       {
                           opponent.CardPlay(opponentJetter1, ItemPosition.Start, playerJetter1);
                       },
                       player => player.CardPlay(playerJetter2, ItemPosition.Start, opponentJetter1),
                       opponent =>
                       {
                           opponent.CardPlay(opponentJetter2, ItemPosition.Start, playerJetter2);
                       },
                };

                CardModel playerJetter1Model = null;
                CardModel playerJetter2Model = null;
                CardModel opponentJetter1Model = null;
                CardModel opponentJetter2Model = null;

                Action validateEndState = () =>
                {
                    playerJetter1Model = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerJetter1);
                    playerJetter2Model = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerJetter2);
                    opponentJetter1Model = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentJetter1);
                    opponentJetter2Model = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentJetter2);

                    Assert.NotNull(playerJetter1Model);
                    Assert.NotNull(playerJetter2Model);
                    Assert.NotNull(opponentJetter1Model);
                    Assert.NotNull(opponentJetter2Model);

                    Assert.AreEqual(playerJetter1Model.MaxCurrentDefense - 1, playerJetter1Model.CurrentDefense);
                    Assert.AreEqual(playerJetter2Model.MaxCurrentDefense - 1, playerJetter2Model.CurrentDefense);
                    Assert.AreEqual(opponentJetter1Model.MaxCurrentDefense - 1, opponentJetter1Model.CurrentDefense);
                    Assert.AreEqual(opponentJetter2Model.MaxCurrentDefense, opponentJetter2Model.CurrentDefense);
                };


                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Maelztrom()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Maelztrom", 1),
                    new DeckCardData("Whizper", 20));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Maelztrom", 1),
                    new DeckCardData("Whizper", 20));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerWhizperId1 = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Whizper", 1);
                InstanceId playerWhizperId2 = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Whizper", 2);
                InstanceId playerMaelstromId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Maelztrom", 1);

                InstanceId opponentWhizperId1 = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Whizper", 1);
                InstanceId opponentWhizperId2 = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Whizper", 2);
                InstanceId opponentMaelstromrId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Maelztrom", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerWhizperId1, ItemPosition.Start);
                           player.CardPlay(playerWhizperId2, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentWhizperId1, ItemPosition.Start);
                           opponent.CardPlay(opponentWhizperId2, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardPlay(playerMaelstromId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentWhizperId1, ItemPosition.Start);
                           opponent.CardPlay(opponentWhizperId2, ItemPosition.Start);
                       },
                       player => {},
                       opponent => {
                           opponent.CardPlay(opponentMaelstromrId, ItemPosition.Start);
                       },
                       player => {},
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(0, pvpTestContext.GetCurrentPlayer().CardsOnBoard.Count);
                    Assert.AreEqual(1, pvpTestContext.GetOpponentPlayer().CardsOnBoard.Count);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Ice()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Ice", 2),
                    new DeckCardData("Zlab", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Ice", 2),
                    new DeckCardData("Zlab", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerIce1 = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Ice", 1);
                InstanceId playerZlab = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 1);

                InstanceId opponentIce1 = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Ice", 1);
                InstanceId opponentZlab = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 1);


                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player =>
                       {
                           player.CardPlay(playerZlab, ItemPosition.Start);
                           player.CardPlay(playerIce1, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentZlab, ItemPosition.Start);
                           opponent.CardPlay(opponentIce1, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentIce1, Enumerators.AbilityType.STUN, new List<ParametrizedAbilityInstanceId>());
                       },
                       player =>
                       {
                           player.CardAttack(playerIce1, opponentZlab);
                       },
                       opponent =>
                       {
                           opponent.CardAttack(opponentIce1, playerZlab);
                           
                       },
                };

                Action validateEndState = () =>
                {
                    Assert.IsTrue(((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlab)).IsStun);
                    Assert.IsTrue(((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlab)).IsStun);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Ztalagmite()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Ztalagmite", 15));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Ztalagmite", 15));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerZtalagmiteId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Ztalagmite", 1);

                InstanceId opponentZtalagmiteId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Ztalagmite", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerZtalagmiteId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentZtalagmiteId, ItemPosition.Start);
                       },
                       player => {},
                       opponent => {},
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(pvpTestContext.GetCurrentPlayer().MaxCurrentDefense - 3, pvpTestContext.GetCurrentPlayer().Defense);
                    Assert.AreEqual(pvpTestContext.GetOpponentPlayer().MaxCurrentDefense - 3, pvpTestContext.GetOpponentPlayer().Defense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Ozmoziz()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Ozmoziz", 10));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Ozmoziz", 10));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Ozmoziz", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Ozmoziz", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player => {},
                    opponent => {},
                    player => player.CardPlay(playerCardId, ItemPosition.Start),
                    opponent =>
                    {
                        opponent.CardPlay(opponentCardId, ItemPosition.Start);
                        opponent.CardAbilityUsed(opponentCardId, Enumerators.AbilityType.ADD_CARD_BY_NAME_TO_HAND, new List<ParametrizedAbilityInstanceId>());
                    },
                    player => { }
                };

                Action validateEndState = () =>
                {
                    Assert.IsTrue(pvpTestContext.GetCurrentPlayer().CardsInHand.FindAll(card => card.Card.Prototype.MouldId == 155).Count > 0);
                    Assert.IsTrue(pvpTestContext.GetOpponentPlayer().CardsInHand.FindAll(card => card.Card.Prototype.MouldId == 155).Count > 0);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator HoZer()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Znowy", 1),
                    new DeckCardData("HoZer", 2));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Znowy", 1),
                    new DeckCardData("HoZer", 2));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerHoZer1 = pvpTestContext.GetCardInstanceIdByName(playerDeck, "HoZer", 1);
                InstanceId playerHoZer2 = pvpTestContext.GetCardInstanceIdByName(playerDeck, "HoZer", 2);
                InstanceId playerZnowy = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Znowy", 1);

                InstanceId opponentHoZer1 = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "HoZer", 1);
                InstanceId opponentHoZer2 = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "HoZer", 2);
                InstanceId opponentZnowy = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Znowy", 1);


                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerHoZer1, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentHoZer1, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardPlay(playerZnowy, ItemPosition.Start);
                           player.CardPlay(playerHoZer2, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentZnowy, ItemPosition.Start);
                           opponent.CardPlay(opponentHoZer2, ItemPosition.Start);
                       },
                };

                CardModel playerHoZer1Model = null;
                CardModel playerHoZer2Model = null;
                CardModel opponentHoZer1Model = null;
                CardModel opponentHoZer2Model = null;

                Action validateEndState = () =>
                {
                    playerHoZer1Model = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerHoZer1);
                    playerHoZer2Model = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerHoZer2);
                    opponentHoZer1Model = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentHoZer1);
                    opponentHoZer2Model = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentHoZer2);

                    Assert.AreEqual(playerHoZer1Model.Card.Prototype.Damage, playerHoZer1Model.CurrentDamage);
                    Assert.AreEqual(playerHoZer2Model.Card.Prototype.Damage + 1, playerHoZer2Model.CurrentDamage);
                    Assert.AreEqual(opponentHoZer1Model.Card.Prototype.Damage, opponentHoZer1Model.CurrentDamage);
                    Assert.AreEqual(opponentHoZer2Model.Card.Prototype.Damage + 1, opponentHoZer2Model.CurrentDamage);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zlider()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Zlider", 1),
                    new DeckCardData("Zlab", 15));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Zlider", 1),
                    new DeckCardData("Zlab", 15));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerZliderId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlider", 1);
                InstanceId playerZlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 1);

                InstanceId opponentZliderId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlider", 1);
                InstanceId opponentZlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player => {},
                    opponent => {},
                    player =>
                    {
                        player.CardPlay(playerZlabId, ItemPosition.Start);
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentZlabId, ItemPosition.Start);
                    },
                    player =>
                    {
                        player.CardPlay(playerZliderId, ItemPosition.Start, opponentZlabId);
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentZliderId, ItemPosition.Start, playerZlabId);
                    },
                };

                Action validateEndState = () =>
                {
                    Assert.IsTrue(((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlabId))
                        .GameMechanicDescriptionsOnUnit.Contains(Enumerators.GameMechanicDescription.Distract));
                    Assert.IsTrue(((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlabId))
                       .GameMechanicDescriptionsOnUnit.Contains(Enumerators.GameMechanicDescription.Distract));
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Geyzer()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Geyzer", 10));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Geyzer", 10));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Geyzer", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Geyzer", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player => {},
                    opponent => {},
                    player =>
                    {
                         player.CardPlay(playerCardId, ItemPosition.Start);
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentCardId, ItemPosition.Start);
                        opponent.CardAbilityUsed(opponentCardId, Enumerators.AbilityType.ADD_CARD_BY_NAME_TO_HAND, new List<ParametrizedAbilityInstanceId>());
                    },
                    player => { }
                };

                Action validateEndState = () =>
                {
                    Assert.IsNotNull(pvpTestContext.GetCurrentPlayer().CardsInHand.Select(card => card.Card.Prototype.MouldId == 156));
                    Assert.IsNotNull(pvpTestContext.GetOpponentPlayer().CardsInHand.Select(card => card.Card.Prototype.MouldId == 156));
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Froztbite()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Froztbite", 10));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Froztbite", 10));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Froztbite", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Froztbite", 1);

                int delayedDamage = 6;

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player => {},
                    opponent => {},
                    player =>
                    {
                         player.CardPlay(playerCardId, ItemPosition.Start);
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentCardId, ItemPosition.Start);
                        opponent.CardAbilityUsed(opponentCardId, Enumerators.AbilityType.DELAYED_GAIN_ATTACK, new List<ParametrizedAbilityInstanceId>());
                    },
                    player => {},
                    opponent => {},
                };

                Action validateEndState = () =>
                {
                    CardModel playerUnit = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId);
                    CardModel opponentUnit = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId);
                    Assert.AreEqual(playerUnit.Card.Prototype.Damage + delayedDamage, playerUnit.CurrentDamage);
                    Assert.AreEqual(opponentUnit.Card.Prototype.Damage + delayedDamage, opponentUnit.CurrentDamage);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        [Category("PlayQuickSubset2")]
        public IEnumerator Freezee()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Freezee", 2),
                    new DeckCardData("Zlab", 20));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Freezee", 2),
                    new DeckCardData("Zlab", 20));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerFreezeeId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Freezee", 1);
                InstanceId playerFreezee2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Freezee", 2);
                InstanceId playerZlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 1);
                InstanceId playerZlab2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 2);
                InstanceId playerZlab3Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 3);
                InstanceId opponentFreezeeId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Freezee", 1);
                InstanceId opponentFreezee2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Freezee", 2);
                InstanceId opponentZlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 1);
                InstanceId opponentZlab2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 2);
                InstanceId opponentZlab3Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 3);

                CardModel playerUnitZlab = null;
                CardModel playerUnitZlab2 = null;
                CardModel playerUnitZlab3 = null;

                CardModel opponentUnitZlab = null;
                CardModel opponentUnitZlab2 = null;
                CardModel opponentUnitZlab3 = null;

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player => {},
                    opponent => {},
                    player => {},
                    opponent => {},
                    player =>
                    {
                        player.CardPlay(playerZlabId, ItemPosition.End);
                        player.CardPlay(playerZlab2Id, ItemPosition.End);
                        player.CardPlay(playerZlab3Id, ItemPosition.End);                       
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentZlabId, ItemPosition.End);
                        opponent.CardPlay(opponentZlab2Id, ItemPosition.End);
                        opponent.CardPlay(opponentZlab3Id, ItemPosition.End);
                    },
                    player =>
                    {
                        playerUnitZlab = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlabId));
                        playerUnitZlab2 = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlab2Id));
                        playerUnitZlab3 = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlab3Id));
                        player.CardPlay(playerFreezeeId, ItemPosition.End, opponentZlab2Id);
                        player.CardPlay(playerFreezee2Id, ItemPosition.End, opponentZlab2Id);
                    },
                    opponent =>
                    {
                        opponentUnitZlab = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlabId));
                        opponentUnitZlab2 = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlab2Id));
                        opponentUnitZlab3 = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlab3Id));
                        opponent.CardPlay(opponentFreezeeId, ItemPosition.End, playerZlab2Id);
                        opponent.CardPlay(opponentFreezee2Id, ItemPosition.End, playerZlab2Id);
                    },
                    player =>
                    {
                        Assert.IsTrue(playerUnitZlab.IsStun);
                        Assert.IsTrue(playerUnitZlab2.IsStun);
                        Assert.IsTrue(playerUnitZlab3.IsStun);
                        Assert.IsTrue(opponentUnitZlab.IsStun);
                        Assert.IsTrue(opponentUnitZlab2.IsStun);
                        Assert.IsTrue(opponentUnitZlab3.IsStun);
                    },
                };

                Action validateEndState = () => {};

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        [Category("PlayQuickSubset2")]
        public IEnumerator Zhatterer()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Zlab", 1),
                    new DeckCardData("Freezee", 1),
                    new DeckCardData("Zhatterer", 10));
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Zlab", 1),
                    new DeckCardData("Freezee", 1),
                    new DeckCardData("Zhatterer", 10));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerZlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 1);
                InstanceId playerZhattererId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zhatterer", 1);
                InstanceId playerFreezeeId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Freezee", 1);
                InstanceId opponentZlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 1);
                InstanceId opponentZhattererId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zhatterer", 1);
                InstanceId opponentFreezeeId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Freezee", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player => {},
                    opponent => {},
                    player =>
                    {
                        player.CardPlay(playerZlabId, ItemPosition.Start);
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentZlabId, ItemPosition.Start);
                        opponent.CardPlay(opponentFreezeeId, ItemPosition.Start, playerZlabId);
                        opponent.CardPlay(opponentZhattererId, ItemPosition.Start, playerZlabId);
                    },
                    player =>
                    {
                        player.CardPlay(playerFreezeeId, ItemPosition.Start, opponentZlabId);
                        player.CardPlay(playerZhattererId, ItemPosition.Start, opponentZlabId);
                    },
                    player => {},
                    opponent => {},
                };

                Action validateEndState = () =>
                {
                    Assert.Null((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlabId));
                    Assert.Null((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlabId));
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Icicle()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Icicle", 1),
                    new DeckCardData("Cerberuz", 5));

                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Icicle", 1),
                    new DeckCardData("Cerberuz", 5));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerIcicleId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Icicle", 1);
                InstanceId playerCerberuzId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Cerberuz", 1);
                InstanceId opponentIcicleId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Icicle", 1);
                InstanceId opponentCerberuzId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Cerberuz", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player =>  player.CardPlay(playerCerberuzId, ItemPosition.Start),
                    opponent => opponent.CardPlay(opponentCerberuzId, ItemPosition.Start),
                    player => player.CardPlay(playerIcicleId, ItemPosition.Start, opponentCerberuzId),
                    opponent =>opponent.CardPlay(opponentIcicleId, ItemPosition.Start, playerCerberuzId),
                    player => {},
                    opponent => {}
                };

                Action validateEndState = () =>
                {
                    Assert.IsNull(TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCerberuzId));
                    Assert.IsNull(TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCerberuzId));
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zpring()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Zpring", 1),
                    new DeckCardData("Zwoop", 20));

                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Zpring", 1),
                    new DeckCardData("Zwoop", 20));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerZpringId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zpring", 1);
                InstanceId playerZwoopId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zwoop", 1);
                InstanceId opponentZpringId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zpring", 1);
                InstanceId opponentZwoopId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zwoop", 1);

                int value = 1;

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player => {},
                    opponent =>
                    {
                        opponent.CardPlay(opponentZwoopId, ItemPosition.Start);
                        opponent.CardPlay(opponentZpringId, ItemPosition.Start);
                    },
                    player =>
                    {
                        player.CardPlay(playerZpringId, ItemPosition.Start);
                        player.CardPlay(playerZwoopId, ItemPosition.Start);
                    },
                    opponent => {},
                    player => {},
                    opponent =>
                    {
                        Assert.AreEqual(value, pvpTestContext.GetOpponentPlayer().ExtraGoo);
                        Assert.AreEqual(value, pvpTestContext.GetCurrentPlayer().ExtraGoo);
                        opponent.CardAttack(opponentZwoopId, playerZpringId);
                    },
                    player =>
                    {
                        player.CardAttack(playerZwoopId, opponentZpringId);
                    },
                    opponent => {},
                };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(0, pvpTestContext.GetCurrentPlayer().ExtraGoo);
                    Assert.AreEqual(0, pvpTestContext.GetOpponentPlayer().ExtraGoo);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        [Category("PlayQuickSubset2")]
        public IEnumerator Blizzard()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Blizzard", 1),
                    new DeckCardData("Zlab", 20));

                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Blizzard", 1),
                    new DeckCardData("Zlab", 20));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerBlizzardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Blizzard", 1);
                InstanceId playerZlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 1);
                InstanceId playerZlab2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 2);
                InstanceId opponentBlizzardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Blizzard", 1);
                InstanceId opponentZlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 1);
                InstanceId opponentZlab2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 2);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player => {},
                    opponent => {},
                    player => {},
                    opponent => {},
                    player => {},
                    opponent =>
                    {
                        opponent.CardPlay(opponentZlabId, ItemPosition.Start);
                        opponent.CardPlay(opponentZlab2Id, ItemPosition.Start);
                    },
                    player =>
                    {
                        player.CardPlay(playerZlabId, ItemPosition.Start);
                        player.CardPlay(playerZlab2Id, ItemPosition.Start);
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentBlizzardId, ItemPosition.Start);
                    },
                    player =>
                    {
                        player.CardPlay(playerBlizzardId, ItemPosition.Start);
                    },
                    opponent =>
                    {
                        Assert.IsTrue(((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlabId)).IsStun);
                        Assert.IsTrue(((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZlab2Id)).IsStun);
                        Assert.IsTrue(((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlabId)).IsStun);
                        Assert.IsTrue(((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZlab2Id)).IsStun);
                    },
                };

                Action validateEndState = () => {};

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zplash()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zplash", 1),
                    new DeckCardData("Trunk", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zplash", 1),
                    new DeckCardData("Trunk", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);
                
                InstanceId playerZplashId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zplash", 1);
                InstanceId playerTrunk1Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Trunk", 1);
                InstanceId playerTrunk2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Trunk", 2);

                InstanceId opponentZplashId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zplash", 1);
                InstanceId opponentTrunk1Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Trunk", 1);
                InstanceId opponentTrunk2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Trunk", 2);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerTrunk1Id, ItemPosition.Start);
                           player.CardPlay(playerTrunk2Id, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentTrunk1Id, ItemPosition.Start);
                           opponent.CardPlay(opponentTrunk2Id, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardPlay(playerZplashId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentZplashId, ItemPosition.Start);
                       },
                       player => {},
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    int damageValue = 2;
                    CardModel trunk1OpponentModel  = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentTrunk1Id));
                    Assert.AreEqual(trunk1OpponentModel.MaxCurrentDefense-damageValue, trunk1OpponentModel.CurrentDefense);
                    CardModel trunk2OpponentModel  = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentTrunk2Id));
                    Assert.AreEqual(trunk2OpponentModel.MaxCurrentDefense-damageValue, trunk2OpponentModel.CurrentDefense);

                    CardModel trunk1PlayerModel  = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerTrunk1Id));
                    Assert.AreEqual(trunk1PlayerModel.MaxCurrentDefense-damageValue, trunk1PlayerModel.CurrentDefense);
                    CardModel trunk2PlayerModel  = ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerTrunk2Id));
                    Assert.AreEqual(trunk2PlayerModel.MaxCurrentDefense-damageValue, trunk2PlayerModel.CurrentDefense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator ZubZero()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Zub-Zero", 1),
                    new DeckCardData("Trunk", 15));

                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Zub-Zero", 1),
                    new DeckCardData("Trunk", 15));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerZubZeroId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zub-Zero", 1);

                InstanceId opponentTrunkId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Trunk", 1);
                InstanceId opponentZubZeroId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zub-Zero", 1);;

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerZubZeroId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentTrunkId, ItemPosition.Start);
                       },
                       player => {},
                       opponent =>
                       {
                           opponent.CardPlay(opponentZubZeroId, ItemPosition.Start);
                       },
                       player => {},
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    CardModel playerUnit1 = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZubZeroId);
                    CardModel opponentUnit1 = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZubZeroId);

                    Assert.IsTrue(playerUnit1.IsHeavyUnit);
                    Assert.IsTrue(playerUnit1.HasBuffShield);

                    Assert.IsFalse(opponentUnit1.IsHeavyUnit);
                    Assert.IsFalse(opponentUnit1.HasBuffHeavy);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        [Category("PlayQuickSubset2")]
        public IEnumerator Tzunamy()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Tzunamy", 1),
                    new DeckCardData("Zlab", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Tzunamy", 1),
                    new DeckCardData("Zlab", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerZlabId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 1);
                InstanceId playerZlab2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 2);
                InstanceId playerZlab3Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 3);
                InstanceId playerZlab4Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zlab", 4);
                InstanceId playerTzunamyId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Tzunamy", 1);

                InstanceId opponentZlabId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 1);
                InstanceId opponentZlab2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 2);
                InstanceId opponentZlab3Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 3);
                InstanceId opponentZlab4Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zlab", 4);
                InstanceId opponentTzunamyId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Tzunamy", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerZlabId, ItemPosition.Start);
                           player.CardPlay(playerZlab2Id, ItemPosition.Start);
                           player.CardPlay(playerZlab3Id, ItemPosition.Start);
                           player.CardPlay(playerZlab4Id, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentZlabId, ItemPosition.Start);
                           opponent.CardPlay(opponentZlab2Id, ItemPosition.Start);
                           opponent.CardPlay(opponentZlab3Id, ItemPosition.Start);
                           opponent.CardPlay(opponentZlab4Id, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardPlay(playerTzunamyId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentTzunamyId, ItemPosition.Start, skipEntryAbilities:true);
                           opponent.CardAbilityUsed(opponentTzunamyId, Enumerators.AbilityType.MASSIVE_DAMAGE, new List<ParametrizedAbilityInstanceId>(){});
                       },
                       player => {},
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(1, pvpTestContext.GetCurrentPlayer().CardsOnBoard.Count);
                    Assert.AreEqual(1, pvpTestContext.GetOpponentPlayer().CardsOnBoard.Count);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Vortex()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Vortex", 1),
                    new DeckCardData("Znowy", 15));

                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Vortex", 1),
                    new DeckCardData("Znowy", 15));

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerZnowyId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Znowy", 1);
                InstanceId playerZnowy2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Znowy", 2);
                InstanceId playerZnowy3Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Znowy", 3);
                InstanceId playerZnowy4Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Znowy", 4);
                InstanceId playerVortexId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Vortex", 1);

                InstanceId opponentZnowyId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Znowy", 1);
                InstanceId opponentZnowy2Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Znowy", 2);
                InstanceId opponentZnowy3Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Znowy", 3);
                InstanceId opponentZnowy4Id = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Znowy", 4);
                InstanceId opponentVortexId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Vortex", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerZnowyId, ItemPosition.Start);
                           player.CardPlay(playerZnowy2Id, ItemPosition.Start);
                           player.CardPlay(playerZnowy3Id, ItemPosition.Start);
                           player.CardPlay(playerZnowy4Id, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentZnowy4Id, ItemPosition.Start);
                           opponent.CardPlay(opponentZnowy3Id, ItemPosition.Start);
                           opponent.CardPlay(opponentZnowy2Id, ItemPosition.Start);
                           opponent.CardPlay(opponentZnowyId, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardPlay(playerVortexId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentVortexId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentVortexId, Enumerators.AbilityType.REPLACE_UNITS_WITH_TYPE_ON_STRONGER_ONES, new List<ParametrizedAbilityInstanceId>()
                           {
                               new ParametrizedAbilityInstanceId(opponentZnowyId,
                                   new ParametrizedAbilityParameters()
                                   {
                                       CardName = "FroZen"
                                   }),
                               new ParametrizedAbilityInstanceId(opponentZnowy2Id,
                                   new ParametrizedAbilityParameters()
                                   {
                                       CardName = "HoZer"
                                   }),
                               new ParametrizedAbilityInstanceId(opponentZnowy3Id,
                                   new ParametrizedAbilityParameters()
                                   {
                                       CardName = "Znowman"
                                   }),
                               new ParametrizedAbilityInstanceId(opponentZnowy4Id,
                                   new ParametrizedAbilityParameters()
                                   {
                                       CardName = "Brook"
                                   }),
                           });
                       },
                       player => {},
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.IsNotNull(TestHelper.GameplayManager.OpponentPlayer.CardsOnBoard.Select(unit => unit.Card.Prototype.Name == "FroZen"));
                    Assert.IsNotNull(TestHelper.GameplayManager.OpponentPlayer.CardsOnBoard.Select(unit => unit.Card.Prototype.Name == "HoZer"));
                    Assert.IsNotNull(TestHelper.GameplayManager.OpponentPlayer.CardsOnBoard.Select(unit => unit.Card.Prototype.Name == "Znowman"));
                    Assert.IsNotNull(TestHelper.GameplayManager.OpponentPlayer.CardsOnBoard.Select(unit => unit.Card.Prototype.Name == "Brook"));
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false, true, true);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Brook()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 4,
                    new DeckCardData("Brook", 2),
                    new DeckCardData("Trunk", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 4,
                    new DeckCardData("Brook", 2),
                    new DeckCardData("Trunk", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck);

                InstanceId playerTrunkId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Trunk", 1);
                InstanceId playerBrookId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Brook", 1);
                InstanceId opponentTrunkId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Trunk", 1);
                InstanceId opponentBrookId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Brook", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerTrunkId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentTrunkId, ItemPosition.Start);
                       },
                       player => 
                       {
                           player.CardPlay(playerBrookId, ItemPosition.Start);
                       },
                       opponent => 
                       {
                           opponent.CardPlay(opponentBrookId, ItemPosition.Start);
                       },
                       player => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(true, ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerTrunkId)).IsStun);
                    Assert.AreEqual(true, ((CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentTrunkId)).IsStun);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            });
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Igloo()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2,
                    new DeckCardData("Igloo", 1),
                    new DeckCardData("Hot", 20)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2,
                    new DeckCardData("Igloo", 1),
                    new DeckCardData("Hot", 20)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Igloo", 1);
                InstanceId playerHotId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Hot", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Igloo", 1);
                InstanceId opponentHotId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Hot", 1);

                int defence = 2;
                int damage = 3;

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                    player =>
                    {
                        player.CardPlay(playerHotId, ItemPosition.Start);
                        player.CardPlay(playerCardId, ItemPosition.Start, playerHotId);
                    },
                    opponent =>
                    {
                        opponent.CardPlay(opponentHotId, ItemPosition.Start);
                        opponent.CardPlay(opponentCardId, ItemPosition.Start, opponentHotId);
                    },
                };

                Action validateEndState = () =>
                {
                    CardModel playerUnit = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerHotId);
                    CardModel opponentUnit = (CardModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentHotId);
                    Assert.AreEqual(damage, playerUnit.BuffedDamage);
                    Assert.AreEqual(defence, playerUnit.BuffedDefense);
                    Assert.AreEqual(damage, opponentUnit.BuffedDamage);
                    Assert.AreEqual(defence, opponentUnit.BuffedDefense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            }, 400);
        }
    }
}
