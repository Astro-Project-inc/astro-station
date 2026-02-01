using System.Linq;
using Content.Server.Administration;
using Content.Server.Mind;
using SkillTypes = Content.Shared._CorvaxGoob.Skills.Skills;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Shared.Mobs;
using Content.Shared.Mind.Components;

namespace Content.Server._CorvaxGoob.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class GrantSkillCommand : IConsoleCommand
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public string Command => "grantskill";

    public string Description => Loc.GetString("cmd-grantskill-desc");

    public string Help => Loc.GetString("cmd-grantskill-help", ("command", Command));

    public void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(_localization.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var id))
        {
            shell.WriteError(_localization.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_entity.TryGetEntity(id, out var entity))
        {
            shell.WriteError(_localization.GetString("shell-invalid-entity-id"));
            return;
        }

        HashSet<SkillTypes> skills = new HashSet<SkillTypes>();

        for (int i = 1; i < args.Length; i++)
        {
            if (!Enum.TryParse<SkillTypes>(args[i], out var skill))
            {
                shell.WriteError(Loc.GetString("cmd-grantskill-not-a-skill-type", ("args", args[i])));
                return;
            }

            skills.Add(skill);
        }

        _entity.System<SkillsSystem>().GrantSkill(entity.Value, skills);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<MindContainerComponent>(args[0]),
                "Entity UID");
        }

        var component = int.TryParse(args[0], out var id)
            ? _entity.TryGetEntity(new(id), out var entity)
                ? _entity.System<MindSystem>().TryGetMind(entity.Value, out _, out var comp)
                    ? comp
                    : null
                : null
            : null;

        var existingSkills = component?.Skills ?? new HashSet<SkillTypes>();

        var alreadyEnteredSkills = new HashSet<SkillTypes>();
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (Enum.TryParse<SkillTypes>(args[i], out var skill))
                alreadyEnteredSkills.Add(skill);
        }

        var allExcludedSkills = new HashSet<SkillTypes>(existingSkills);
        allExcludedSkills.UnionWith(alreadyEnteredSkills);

        var currentInput = args[^1];
        return GetSkillCompletion(currentInput, allExcludedSkills);
    }

    private CompletionResult GetSkillCompletion(string currentInput, HashSet<SkillTypes> existingSkills)
    {
        return CompletionResult.FromOptions(Enum.GetValues<SkillTypes>()
            .Where(skill => !existingSkills.Contains(skill))
            .Select(skill => skill.ToString())
            .Where(name => name.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
            .Select(name => new CompletionOption(name)));
    }
}
