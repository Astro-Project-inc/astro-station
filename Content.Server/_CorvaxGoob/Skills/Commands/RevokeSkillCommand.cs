using System.Linq;
using Content.Server.Administration;
using Content.Server.Mind;
using SkillTypes = Content.Shared._CorvaxGoob.Skills.Skills;
using Content.Shared.Administration;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;

namespace Content.Server._CorvaxGoob.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class RevokeSkillCommand : LocalizedCommands
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public override string Command => "revokeskill";

    public override void Execute(IConsoleShell shell, string arg, string[] args)
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

        if (!_entity.System<MindSystem>().TryGetMind(entity.Value, out _, out var _))
        {
            shell.WriteError(_localization.GetString("shell-invalid-entity-id"));
            return;
        }

        var skills = new HashSet<SkillTypes>();

        for (int i = 1; i < args.Length; i++)
        {
            if (!Enum.TryParse<SkillTypes>(args[i], out var skill))
            {
                shell.WriteError(Loc.GetString("cmd-revokeskill-not-a-skill-type", ("args", args[i])));
                return;
            }
            skills.Add(skill);
        }



        _entity.System<SkillsSystem>().RevokeSkill(entity.Value, skills);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<MindContainerComponent>(args[0]),
                _localization.GetString("shell-argument-uid"));
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

        var availableSkills = existingSkills.Where(skill => !alreadyEnteredSkills.Contains(skill));
        var currentInput = args[^1];

        return CompletionResult.FromOptions(availableSkills
            .Select(skill => skill.ToString())
            .Where(name => name.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
            .Select(name => new CompletionOption(name)));
    }
}
