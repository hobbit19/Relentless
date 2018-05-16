﻿using System.Collections;
using System.Collections.Generic;
using GrandDevs.CZB.Common;
using Newtonsoft.Json;

namespace GrandDevs.CZB.Data
{
    public class Card {
        public int id;
        public Enumerators.SetType cardSetType;
        public string kind;
        public string name;
        public int cost;
        public string description;
        public string flavorText; // new
        public string picture;
        public int damage;
        public int health;
        public string rarity;
        public string type;
        public List<AbilityData> abilities = new List<AbilityData>();

        [JsonIgnore]
        public Enumerators.CardRarity cardRarity;
        [JsonIgnore]
        public Enumerators.CardType cardType;
        [JsonIgnore]
        public Enumerators.CardKind cardKind;

        public Card()
        {
        }
    }
}