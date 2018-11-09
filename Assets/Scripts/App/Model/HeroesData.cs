using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Loom.ZombieBattleground.Common;
using Newtonsoft.Json;

namespace Loom.ZombieBattleground.Data
{
    public class HeroesData
    {
        public List<Hero> Heroes { get; private set; }

        public HeroesData(List<Hero> heroes)
        {
            Heroes = heroes ?? throw new ArgumentNullException(nameof(heroes));
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            foreach (Hero item in Heroes)
            {
                item.ValidateSkillLocking();
            }
        }
    }

    public class Hero
    {
        public int HeroId { get; private set; }

        public string Icon { get; private set; }

        public string Name { get; private set; }

        public string ShortDescription { get; private set; }

        public string LongDescription { get; private set; }

        public long Experience { get; set; }

        public int Level { get; set; }

        [JsonProperty("Element")]
        public Enumerators.SetType HeroElement { get; private set; }

        public List<HeroSkill> Skills { get; private set; }

        public int PrimarySkill { get; set; }

        public int SecondarySkill { get; private set; }

        public string FullName => $"{Name}, {ShortDescription}";

        public Hero(
            int heroId,
            string icon,
            string name,
            string shortDescription,
            string longDescription,
            long experience,
            int level,
            Enumerators.SetType heroElement,
            List<HeroSkill> skills,
            int primarySkill,
            int secondarySkill)
        {
            HeroId = heroId;
            Icon = icon;
            Name = name;
            ShortDescription = shortDescription;
            LongDescription = longDescription;
            Experience = experience;
            Level = level;
            HeroElement = heroElement;
            Skills = skills ?? new List<HeroSkill>();
            PrimarySkill = primarySkill;
            SecondarySkill = secondarySkill;
        }

        public void ValidateSkillLocking()
        {
            //TODO: commented now in perspective of lock funcitonality for release stage
            //int skillId = Level % 4;
            int skillId = 2;
            for (int i = 0; i < skillId; i++)
            {
                Skills[i].Unlocked = true;
            }
        }
    }

    public class HeroSkill
    {
        public string Title { get; private set; }

        public string IconPath { get; private set; }

        public string Description { get; private set; }

        public int Cooldown { get; private set; }

        public int InitialCooldown { get; private set; }

        public int Value { get; private set; }

        public int Attack { get; private set; }

        public int Count { get; private set; }

        [JsonProperty("Skill")]
        public Enumerators.OverlordSkill OverlordSkill { get; private set; }

        [JsonProperty("SkillTargets")]
        public List<Enumerators.SkillTargetType> SkillTargetTypes { get; private set; }

        [JsonProperty("UnitStatus")]
        public Enumerators.UnitStatusType TargetUnitStatusType { get; private set; }

        [JsonProperty("ElementTargets")]
        public List<Enumerators.SetType> ElementTargetTypes { get; private set; }

        [JsonIgnore]
        public bool Unlocked { get; set; }

        public HeroSkill(
            string title,
            string iconPath,
            string description,
            int cooldown,
            int initialCooldown,
            int value,
            int attack,
            int count,
            Enumerators.OverlordSkill overlordSkill,
            List<Enumerators.SkillTargetType> skillTargetTypes,
            Enumerators.UnitStatusType targetUnitStatusType,
            List<Enumerators.SetType> elementTargetTypes)
        {
            Title = title;
            IconPath = iconPath;
            Description = description;
            Cooldown = cooldown;
            InitialCooldown = initialCooldown;
            Value = value;
            Attack = attack;
            Count = count;
            OverlordSkill = overlordSkill;
            SkillTargetTypes = skillTargetTypes ?? new List<Enumerators.SkillTargetType>();
            TargetUnitStatusType = targetUnitStatusType;
            ElementTargetTypes = elementTargetTypes ?? new List<Enumerators.SetType>();
        }
    }
}
