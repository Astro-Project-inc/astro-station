using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Tag;
using SkillTypes = Content.Shared._CorvaxGoob.Skills.Skills;

namespace Content.Server._CorvaxGoob.Skills;

public sealed class SkillsSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

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
    /// Grant all skills on tarteg mind.
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
            return;

        if (clearSkills)
            mindComp.Skills.Clear();

        if (skills.Contains(SkillTypes.All))
        {
            mindComp.Skills.Clear();
            mindComp.Skills.Add(SkillTypes.All);
        }
        else
            mindComp.Skills.UnionWith(skills);

        _adminLog.Add(LogType.AdminCommands, $"Grant {(skills.Contains(SkillTypes.All) ? $"{SkillTypes.All.GetType()}" : $"{string.Join(", ", skills.Select(s => s.GetType()))}")} skills to entity {entity.Id} with mind {mind.Id}. Clear skills: {clearSkills}");

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
            return;

        if (skills.Contains(SkillTypes.All))
            mindComp.Skills.Clear();
        else
        {
            foreach (var skill in skills)
            {
                mindComp.Skills.Remove(skill);
            }
        }

        _adminLog.Add(LogType.AdminCommands, $"Revoke {(skills.Contains(SkillTypes.All) ? $"{SkillTypes.All.GetType()}" : $"{string.Join(", ", skills.Select(s => s.GetType()))}")} from entity {entity.Id} with mind {mind.Id}");

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
