using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Maps;
using Content.Shared._CorvaxGoob.Deathmatch_CS;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server._CorvaxGoob.Deathmatch_CS;

public sealed class Observer : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly CSRuleSystem CSRuleSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IsFighterComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, IsFighterComponent component, MindAddedMessage args)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var gUid, out var dm, out _, out var rule))
        {
            if (!_gameTicker.IsGameRuleActive(gUid, rule))
                continue;
            var mapid = EntityManager.GetComponent<TransformComponent>(uid).MapID;
            foreach (var session in CSRuleSystem.SessionsListS)
            {
                if (session.MapId == mapid)
                {
                    if (!session.Players_.Contains(uid)) session.Players_.Add(uid);
                    var query1 = EntityQueryEnumerator<IsFighterComponent, GhostRoleComponent>();
                    int count = 0;
                    while (query1.MoveNext(out var guid, out _, out var i))
                    {
                        if (EntityManager.TryGetComponent(guid, out IsFighterComponent? _)) count++;
                    }
                    if (count == 0) CSRuleSystem.NewSession(dm);
                    break;
                }
            }
        }
    }
}
