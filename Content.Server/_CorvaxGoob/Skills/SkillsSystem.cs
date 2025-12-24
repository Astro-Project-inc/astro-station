using Content.Shared._CorvaxGoob.CCCVars;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using SkillTypes = Content.Shared._CorvaxGoob.Skills.Skills;

namespace Content.Server._CorvaxGoob.Skills;

public sealed class SkillsSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string SkillsTag = "Skills";

    private bool _skillsEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _skillsEnabled = _cfg.GetCVar(CCCVars.SkillsEnabled);
        Subs.CVar(_cfg, CCCVars.SkillsEnabled, value => _skillsEnabled = value);

        SubscribeLocalEvent<ImplantImplantedEvent>(OnImplantImplanted);
    }

    private void OnImplantImplanted(ref ImplantImplantedEvent e)
    {
        if (e.Implanted is null)
            return;

        if (!_tag.HasTag(e.Implant, SkillsTag))
            return;

        GrantAllSkills(e.Implanted.Value);
    }

    public bool IsSkillsEnabled()
    {
        return _skillsEnabled;
    }

    public bool HasSkill(EntityUid entity, SkillTypes skill)
    {
        if (!_skillsEnabled)
            return true;

        if (HasComp<IgnoreSkillsComponent>(entity))
            return true;

        if (!_mind.TryGetMind(entity, out _, out var mind))
            return false;

        if (mind.Skills.Contains(SkillTypes.All))
            return true;

        return mind.Skills.Contains(skill);
    }

    public void GrantAllSkills(EntityUid entity)
    {
        if (!_mind.TryGetMind(entity, out _, out var mind))
            return;

        mind.Skills.Clear();
        mind.Skills.Add(SkillTypes.All);
    }

    public void GrantSkill(EntityUid entity, HashSet<SkillTypes> skills)
    {
        if (!_mind.TryGetMind(entity, out _, out var mind))
            return;

        if (skills.Contains(SkillTypes.All))
        {
            mind.Skills.Clear();
            mind.Skills.Add(SkillTypes.All);
        }
        else
            mind.Skills.UnionWith(skills);
    }

    /// <summary>
    /// Revokes all skills and grant new on target mind.
    /// </summary>
    public void UpdateSkills(EntityUid entity, HashSet<SkillTypes> skills)
    {
        if (_mind.TryGetMind(entity, out _, out var mind))
            UpdateSkills((entity, mind), skills);
    }

    public void UpdateSkills(Entity<MindComponent> entity, HashSet<SkillTypes>? skills)
    {
        entity.Comp.Skills.Clear();

        if (skills is null)
            return;

        if (entity.Comp.Skills.Contains(SkillTypes.All))
            entity.Comp.Skills.Add(SkillTypes.All);
        else
            entity.Comp.Skills.UnionWith(skills);
    }
}
