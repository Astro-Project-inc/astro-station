using Content.Shared.Mind;
using Robust.Shared.Configuration;

namespace Content.Shared._CorvaxGoob.Skills;

public sealed class SharedSkillsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private bool _skillsEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _skillsEnabled = _cfg.GetCVar(CCCVars.CCCVars.SkillsEnabled);
        Subs.CVar(_cfg, CCCVars.CCCVars.SkillsEnabled, value => _skillsEnabled = value);
    }
    public bool IsSkillsEnabled()
    {
        return _skillsEnabled;
    }

    public bool HasSkill(EntityUid entity, Skills skill)
    {
        if (!_skillsEnabled)
            return true;

        if (HasComp<IgnoreSkillsComponent>(entity))
            return true;

        if (!_mind.TryGetMind(entity, out _, out var mind))
            return false;

        if (mind.Skills.Contains(Skills.All))
            return true;

        return mind.Skills.Contains(skill);
    }
}
