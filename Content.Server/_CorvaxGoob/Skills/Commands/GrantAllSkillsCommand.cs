using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;

namespace Content.Server._CorvaxGoob.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class GrantAllSkillsCommand : IConsoleCommand
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public string Command => "grantallskills";

    public string Description => Loc.GetString("cmd-grantallskills-desc");

    public string Help => Loc.GetString("cmd=grantallskills-help", ("command", Command));

    public void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length != 1)
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

        _entity.System<SkillsSystem>().GrantAllSkills(entity.Value);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<MindContainerComponent>(args[0]),
                "Entity UID");
        }
        return CompletionResult.Empty;
    }
}
