using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;

namespace Loom.ZombieBattleground
{
    public class WorkingCard
    {
       public int Id;

        public int CardId;

        public Card LibraryCard;

        public Player Owner;

        public int InitialHealth, InitialDamage, Health, Damage;

        public int InitialCost, RealCost;

        public Enumerators.CardType Type;

        public WorkingCard(Card card, Player player)
        {
            LibraryCard = card.Clone();
            CardId = LibraryCard.Id;
            Owner = player;

            InitialHealth = LibraryCard.Health;
            InitialDamage = LibraryCard.Damage;
            InitialCost = LibraryCard.Cost;
            Health = InitialHealth;
            Damage = InitialDamage;
            RealCost = InitialCost;

            Type = LibraryCard.CardType;

            Id = GameClient.Get<IGameplayManager>().GetController<CardsController>().GetNewCardInstanceId();
        }

        public bool IsPlayable { get; set; }
    }
}
