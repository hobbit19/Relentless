using System.Linq;
using Loom.ZombieBattleground.Helpers;
using Loom.ZombieBattleground.Protobuf;

namespace Loom.ZombieBattleground.Data
{
    public static class ToProtobufExtensions
    {
        public static DeckCard ToProtobuf(this DeckCardData deckCardData)
        {
            return new DeckCard
            {
                Amount = deckCardData.Amount,
                CardName = deckCardData.CardName
            };
        }

        public static Protobuf.Deck ToProtobuf(this Deck deck)
        {
            return new Protobuf.Deck
            {
                Id = deck.Id,
                OverlordId = deck.OverlordId,
                Name = deck.Name,
                Cards =
                {
                    deck.Cards.Select(card => card.ToProtobuf())
                },
                PrimarySkill = (Protobuf.OverlordSkill.Types.Enum)deck.PrimarySkill,
                SecondarySkill = (Protobuf.OverlordSkill.Types.Enum)deck.SecondarySkill
            };
        }

        public static Protobuf.AbilityData ToProtobuf(this AbilityData ability) {
            Protobuf.AbilityData abilityData = new Protobuf.AbilityData {
                Ability = (Protobuf.AbilityType.Types.Enum) ability.Ability,
                Activity = (Protobuf.AbilityActivity.Types.Enum) ability.Activity,
                Trigger = (Protobuf.AbilityTrigger.Types.Enum) ability.Trigger,
                Targets =
                {
                    ability.Targets.Select(t => (Target.Types.Enum) t)
                },
                Stat = (Stat.Types.Enum) ability.Stat,
                Faction = (Protobuf.Faction.Types.Enum) ability.Faction,
                Effect = (AbilityEffect.Types.Enum) ability.Effect,
                AttackRestriction = (AttackRestriction.Types.Enum) ability.AttackRestriction,
                TargetCardType = (Protobuf.CardType.Types.Enum) ability.TargetCardType,
                TargetUnitSpecialStatus = (UnitSpecialStatus.Types.Enum) ability.TargetUnitSpecialStatus,
                TargetUnitType = (Protobuf.CardType.Types.Enum) ability.TargetUnitType,
                Value = ability.Value,
                Damage = ability.Damage,
                Defense = ability.Defense,
                Name = ability.Name,
                Turns = ability.Turns,
                Count = ability.Count,
                Delay = ability.Delay,
                VisualEffectsToPlay =
                {
                    ability.VisualEffectsToPlay.Select(v => v.ToProtobuf())
                },
                GameMechanicDescription = (GameMechanicDescription.Types.Enum) ability.GameMechanicDescription,
                TargetFaction = (Protobuf.Faction.Types.Enum) ability.TargetFaction,
                SubTrigger = (AbilitySubTrigger.Types.Enum) ability.SubTrigger,
                ChoosableAbilities =
                {
                    ability.ChoosableAbilities.Select(a => a.ToProtobuf())
                },
                Defense2 = ability.Defense2,
                Cost = ability.Cost
            };

            return abilityData;
        }

        public static CardChoosableAbility ToProtobuf(this AbilityData.ChoosableAbility choosableAbility)
        {
            return new CardChoosableAbility {
                Description = choosableAbility.Description,
                AbilityData = choosableAbility.AbilityData.ToProtobuf()
            };
        }

        public static Protobuf.AbilityData.Types.VisualEffectInfo ToProtobuf(this AbilityData.VisualEffectInfo visualEffectInfo)
        {
            return new Protobuf.AbilityData.Types.VisualEffectInfo {
                Type = (Protobuf.AbilityData.Types.VisualEffectInfo.Types.VisualEffectType) visualEffectInfo.Type,
                Path = visualEffectInfo.Path
            };
        }

        public static CardInstance ToProtobuf(this WorkingCard workingCard)
        {
            CardInstance cardInstance = new CardInstance
            {
                InstanceId = workingCard.InstanceId.ToProtobuf(),
                Prototype = workingCard.Prototype.ToProtobuf(),
                Instance = workingCard.InstanceCard.ToProtobuf()
            };

            return cardInstance;
        }

        public static Protobuf.CardInstanceSpecificData ToProtobuf(this CardInstanceSpecificData data)
        {
            Protobuf.CardInstanceSpecificData protoData = new Protobuf.CardInstanceSpecificData
            {
                Cost = data.Cost,
                Damage = data.Damage,
                Defense = data.Defense,
                Faction = (Protobuf.Faction.Types.Enum) data.Faction,
                Type = (CardType.Types.Enum) data.CardType,
                Abilities =
                {
                    data.Abilities.Select(abilityData => abilityData.ToProtobuf())
                }
            };

            return protoData;
        }

        public static Protobuf.Card ToProtobuf(this IReadOnlyCard card)
        {
            Protobuf.Card protoCard = new Protobuf.Card
            {
                MouldId = card.MouldId,
                Name = card.Name,
                Cost = card.Cost,
                Description = card.Description,
                FlavorText = card.FlavorText,
                Picture = card.Picture,
                Damage = card.Damage,
                Defense = card.Defense,
                Faction = (Protobuf.Faction.Types.Enum) card.Faction,
                Frame = card.Frame,
                Kind = (CardKind.Types.Enum) card.Kind,
                Rank = (CreatureRank.Types.Enum) card.Rank,
                Type = (CardType.Types.Enum) card.Type,
                PictureTransform = card.PictureTransform.ToProtobuf(),
                Abilities =
                {
                    card.Abilities.Select(a => a.ToProtobuf())
                },
                UniqueAnimation = (Protobuf.UniqueAnimation.Types.Enum) card.UniqueAnimation,
                Hidden = card.Hidden
            };

            return protoCard;
        }

        public static Vector3Float ToProtobuf(this FloatVector3 vector)
        {
            return new Vector3Float
            {
                X = vector.X,
                Y = vector.Y,
                Z = vector.Z
            };
        }

        public static Protobuf.PictureTransform ToProtobuf(this PictureTransform cardViewInfo)
        {
            if (cardViewInfo == null)
                return null;

            return new Protobuf.PictureTransform
            {
                Position = cardViewInfo.Position.ToProtobuf(),
                Scale = cardViewInfo.Scale.ToProtobuf()
            };
        }

        public static Protobuf.InstanceId ToProtobuf(this InstanceId instanceId)
        {
            return new Protobuf.InstanceId
            {
                Id = instanceId.Id
            };
        }

        public static Protobuf.Parameter ToProtobuf(this ParametrizedAbilityParameters parameters)
        {
            return new Protobuf.Parameter
            {
                Damage = parameters.Attack,
                Defense = parameters.Defense,
                CardName = parameters.CardName
            };
        }

        public static Protobuf.DebugCheatsConfiguration ToProtobuf(this BackendCommunication.DebugCheatsConfiguration debugCheatsConfiguration)
        {
            if (debugCheatsConfiguration == null)
                return new Protobuf.DebugCheatsConfiguration();

            return new Protobuf.DebugCheatsConfiguration
            {
                Enabled = debugCheatsConfiguration.Enabled,

                UseCustomRandomSeed = debugCheatsConfiguration.CustomRandomSeed != null,
                CustomRandomSeed = debugCheatsConfiguration.CustomRandomSeed ?? 0,

                UseCustomDeck = debugCheatsConfiguration.UseCustomDeck,
                CustomDeck = debugCheatsConfiguration.UseCustomDeck ? debugCheatsConfiguration.CustomDeck?.ToProtobuf() : null,

                DisableDeckShuffle = debugCheatsConfiguration.DisableDeckShuffle,

                ForceFirstTurnUserId = debugCheatsConfiguration.ForceFirstTurnUserId ?? "",

                IgnoreGooRequirements = debugCheatsConfiguration.IgnoreGooRequirements,

                SkipMulligan = debugCheatsConfiguration.SkipMulligan
            };
        }
    }
}

