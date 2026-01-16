using Content.Shared._CorvaxGoob.CCCVars;
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

    private void OnImplantImplanted(ref ImplantImplantedEvent e)
    {
        if (e.Implanted is null)
            return;

        if (!_tag.HasTag(e.Implant, SkillsTag))
            return;

        GrantAllSkills(e.Implanted.Value);
    }

    public void GrantAllSkills(EntityUid entity)
    {
        GrantSkill(entity, new HashSet<SkillTypes>() { SkillTypes.All });
    }

    public void GrantSkill(EntityUid entity, HashSet<SkillTypes> skills, bool clearSkills = false)
    {
        if (!_mind.TryGetMind(entity, out _, out var mind))
            return;

        if (clearSkills)
            mind.Skills.Clear();

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
        GrantSkill(entity, skills, true);
    }
}
