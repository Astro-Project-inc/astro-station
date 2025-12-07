using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Maps;
using Content.Shared._CorvaxGoob.Deathmatch_CS;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;

namespace Content.Server._CorvaxGoob.Deathmatch_CS;

public sealed class Observer : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly CSRuleSystem _CSRuleSystem = default!;
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
            foreach (var session in _CSRuleSystem.SessionsListS)
            {
                if (session.MapId == mapid)
                {
                    session.Players_.Add(uid);
                    break;
                }
            }
        }
    }
}
