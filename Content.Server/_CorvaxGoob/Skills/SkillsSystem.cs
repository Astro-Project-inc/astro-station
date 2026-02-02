using System.Linq;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Tag;
using SkillTypes = Content.Shared._CorvaxGoob.Skills.Skills;

namespace Content.Server._CorvaxGoob.Skills;

public sealed class SkillsSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string SkillsTag = "Skills";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantImplantedEvent>(OnImplantImplanted);
    }

    private void OnImplantImplanted(ref ImplantImplantedEvent ev)
    {
        if (ev.Implanted is null)
            return;

        if (!_tag.HasTag(ev.Implant, SkillsTag))
            return;

        GrantAllSkills(ev.Implanted.Value);
    }

    /// <summary>
    /// Grant all skills on target mind.
    /// </summary>
    /// <param name="entity">Entity with target mind</param>
    public void GrantAllSkills(EntityUid entity)
    {
        GrantSkill(entity, SkillTypes.All);
    }

    /// <summary>
    /// Grant new skills on target mind. Can full clear skills on mind if clearSkills set to true
    /// </summary>
    /// <param name="entity">Entity with target mind</param>
    /// <param name="skills">What skills we grant</param>
    /// <param name="clearSkills">Does we need to clear all skills before grant new</param>
    public void GrantSkill(EntityUid entity, HashSet<SkillTypes> skills, bool clearSkills = false)
    {
        if (!_mind.TryGetMind(entity, out var mind, out var mindComp))
        {
            Log.Error($"Can't get mind from entity {entity.Id}");
            return;
        }

        if (skills.Count() < 1)
        {
            Log.Error($"HashSet<Skills> skills is empty, entity {entity.Id}");
            return;
        }

        HashSet<SkillTypes> oldSkills = new HashSet<SkillTypes>(mindComp.Skills);

        if (clearSkills)
            mindComp.Skills.Clear();

        if (skills.Contains(SkillTypes.All))
        {
            mindComp.Skills.Clear();
            mindComp.Skills.UnionWith(new HashSet<SkillTypes>() { SkillTypes.AdvancedBuilding, SkillTypes.Butchering, SkillTypes.MedicalEquipment, SkillTypes.SelfSurgery, SkillTypes.Shooting, SkillTypes.ShuttleControl, SkillTypes.Surgery });
        }
        else
            mindComp.Skills.UnionWith(skills);

        HashSet<SkillTypes> newSkills = new HashSet<SkillTypes>(mindComp.Skills);
        newSkills.ExceptWith(oldSkills);

        if (newSkills.Count() < 1)
        {
            Log.Info($"No new skills added to entity {entity.Id} with mind {mind.Id}. Clear skills: {clearSkills}");
            Dirty(mind, mindComp);
            return;
        }

        string skillsMassive = string.Join(", ", newSkills.Select(s => s.ToString()));

        Log.Info($"Grant {(skills.Contains(SkillTypes.All) ? $"{SkillTypes.All.ToString()}" : $"{skillsMassive}")} skills to entity {entity.Id} with mind {mind.Id}. Clear skills: {clearSkills}");

        Dirty(mind, mindComp);
    }

    /// <summary>
    /// Grant new skills on target mind. Can full clear skills on mind if clearSkills set to true
    /// </summary>
    /// <param name="entity">Entity with target mind</param>
    /// <param name="skills">What skills we grant</param>
    /// <param name="clearSkills">Does we need to clear all skills before grant new</param>
    public void GrantSkill(EntityUid entity, bool clearSkills = false, params SkillTypes[] skills)
    {
        GrantSkill(entity, new HashSet<SkillTypes>(skills), clearSkills);
    }

    /// <summary>
    /// Grant new skill on target mind. Can full clear skills on mind if clearSkills set to true
    /// </summary>
    /// <param name="entity">Entity with target mind</param>
    /// <param name="skill">What skill we grant</param>
    /// <param name="clearSkills">Does we need to clear all skills before grant new</param>
    public void GrantSkill(EntityUid entity, SkillTypes skill, bool clearSkills = false)
    {
        GrantSkill(entity, new HashSet<SkillTypes>() { skill }, clearSkills);
    }

    /// <summary>
    /// Revoke skills on target mind. If skill is Skills.All - clear all mind skills
    /// </summary>
    /// <param name="entity">Entity with target mind</param>
    /// <param name="skills">What skills we revoke</param>
    public void RevokeSkill(EntityUid entity, HashSet<SkillTypes> skills)
    {
        if (!_mind.TryGetMind(entity, out var mind, out var mindComp))
        {
            Log.Error($"Can't get mind from entity {entity.Id}");
            return;
        }

        if (skills.Count() < 1)
        {
            Log.Error($"HashSet<Skills> skills is empty, entity {entity}");
            return;
        }

        HashSet<SkillTypes> oldSkills = new HashSet<SkillTypes>(mindComp.Skills);

        if (skills.Contains(SkillTypes.All))
            mindComp.Skills.Clear();
        else
        {
            foreach (var skill in skills)
            {
                mindComp.Skills.Remove(skill);
            }
        }

        HashSet<SkillTypes> revokedSkills = new HashSet<SkillTypes>(oldSkills);
        revokedSkills.ExceptWith(mindComp.Skills);

        if (revokedSkills.Count() < 1)
        {
            Log.Info($"No skills revoked from entity {entity.Id} with mind {mind.Id}");
            Dirty(mind, mindComp);
            return;
        }

        string skillsMassive = string.Join(", ", revokedSkills.Select(s => s.ToString()));

        Log.Info($"Revoke {(skills.Contains(SkillTypes.All) ? $"{SkillTypes.All.ToString()}" : $"{skillsMassive}")} skills from entity {entity.Id} with mind {mind.Id}");

        Dirty(mind, mindComp);
    }

    /// <summary>
    /// Revoke skills on target mind. If skill is Skills.All - clear all mind skills
    /// </summary>
    /// <param name="entity">Entity with target mind</param>
    /// <param name="skills">What skills we revoke</param>
    public void RevokeSkill(EntityUid entity, params SkillTypes[] skills)
    {
        RevokeSkill(entity, new HashSet<SkillTypes>(skills));
    }

    /// <summary>
    /// Revoke skills on target mind. If skill is Skills.All - clear all mind skills
    /// </summary>
    /// <param name="entity">Entity with target mind</param>
    /// <param name="skill">What skill we revoke</param>
    public void RevokeSkill(EntityUid entity, SkillTypes skill)
    {
        RevokeSkill(entity, new HashSet<SkillTypes>() { skill });
    }
}
