using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Loom.ZombieBattleground.Common;
using Newtonsoft.Json;

namespace Loom.ZombieBattleground.Data
{
    public class OverlordData
    {
        public List<OverlordModel> Overlords { get; }

        public OverlordData(List<OverlordModel> overlords)
        {
            Overlords = overlords ?? throw new ArgumentNullException(nameof(overlords));
        }

        public OverlordModel GetOverlordById(OverlordId id)
        {
            return Overlords.Single(model => model.Id == id);
        }
    }

    public class OverlordModel
    {
        public OverlordId Id { get; }

        public string Icon { get; }

        public string Name { get; }

        public string ShortDescription { get; }

        public string LongDescription { get; }

        public long Experience { get; set; }

        public int Level { get; set; }

        public Enumerators.Faction Faction { get; }

        public List<OverlordSkill> Skills { get; }

        public string FullName => $"{Name}, {ShortDescription}";

        public OverlordModel(
            OverlordId id,
            string icon,
            string name,
            string shortDescription,
            string longDescription,
            long experience,
            int level,
            Enumerators.Faction faction,
            List<OverlordSkill> skills)
        {
            Id = id;
            Icon = icon;
            Name = name;
            ShortDescription = shortDescription;
            LongDescription = longDescription;
            Experience = experience;
            Level = level;
            Faction = faction;
            Skills = skills ?? new List<OverlordSkill>();
        }

        public OverlordSkill GetSkill(Enumerators.Skill skill)
        {
            return Skills.Find(x => x.Skill == skill);
        }

        public OverlordSkill GetSkill(int index)
        {
            return Skills[index];
        }
    }

    public class OverlordSkill
    {
        public int Id { get; }

        public string Title { get; }

        public string IconPath { get; }

        public string Description { get; }

        public int Cooldown { get; }

        public int InitialCooldown { get; }

        public int Value { get; }

        public int Damage { get; }

        public int Count { get; }

        public Enumerators.Skill Skill { get; private set; }

        public List<Enumerators.SkillTarget> SkillTargets { get; private set; }

        public Enumerators.UnitSpecialStatus TargetUnitSpecialStatus { get; private set; }

        public List<Enumerators.Faction> TargetFactions { get; private set; }

        public bool Unlocked { get; set; }

        public bool CanSelectTarget { get; }

        public bool SingleUse { get; }

        public OverlordSkill(
            int id,
            string title,
            string iconPath,
            string description,
            int cooldown,
            int initialCooldown,
            int value,
            int damage,
            int count,
            Enumerators.Skill skill,
            List<Enumerators.SkillTarget> skillTargets,
            Enumerators.UnitSpecialStatus targetUnitSpecialStatus,
            List<Enumerators.Faction> targetFactions,
            bool unlocked,
            bool canSelectTarget,
            bool singleUse)
        {
            Id = id;
            Title = title;
            IconPath = iconPath;
            Description = description;
            Cooldown = cooldown;
            InitialCooldown = initialCooldown;
            Value = value;
            Damage = damage;
            Count = count;
            Skill = skill;
            SkillTargets = skillTargets ?? new List<Enumerators.SkillTarget>();
            TargetUnitSpecialStatus = targetUnitSpecialStatus;
            TargetFactions = targetFactions ?? new List<Enumerators.Faction>();
            CanSelectTarget = canSelectTarget;
            Unlocked = unlocked;
            SingleUse = singleUse;
        }
    }
}
