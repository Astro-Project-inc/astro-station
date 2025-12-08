using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Maps;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server._CorvaxGoob.Deathmatch_CS;

public sealed class CSRuleSystem : GameRuleSystem<CSRuleComponent>
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public List<Session> Sessions = new();
    public sealed class Session
    {
        public MapId MapId;
        public List<EntityUid> Players = new();
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(MapClearing);
        SubscribeLocalEvent<GameRuleStartedEvent>(RoundStart);

        SubscribeLocalEvent<IsFighterComponent, MobStateChangedEvent>(OnKillReported);
        SubscribeLocalEvent<IsFighterComponent, PlayerDetachedEvent>(PlayerHasDisconnectednected);
    }

    private void RoundStart(ref GameRuleStartedEvent _)
    {
        NewSession();
    }

    public void NewSession()
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uId, out var csRuleC, out var gRuleC))
        {
            if (!GameTicker.IsGameRuleActive(uId, gRuleC))
                return;

            if ((this.Sessions?.Count ?? 0) < csRuleC.NumberOfSessions)
            {
                Session newsession = new();
                GameMapPrototype? protoMap;

                if (!csRuleC.RandomArena)
                    protoMap = _gameMapManager.GetSelectedMap();
                else
                {
                    var maps = _gameMapManager.CurrentlyEligibleMaps().ToList();
                    protoMap = _random.Pick(maps);
                }

                Addmap(out newsession.MapId, protoMap);
                Sessions?.Add(newsession);
            }
        }
    }

    private void MapClearing(GameRunLevelChangedEvent ev)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uId, out _, out var gRuleC))
        {
            if (!GameTicker.IsGameRuleActive(uId, gRuleC))
                return;

            var activeMapIds = new HashSet<MapId>(Sessions.Select(s => s.MapId));
            foreach (var mapId in _map.GetAllMapIds())
            {
                if (_map.MapExists(mapId) && !activeMapIds.Contains(mapId))
                {
                    _map.DeleteMap(mapId);
                }
            }
        }
    }
    private void Addmap(out MapId mapId, GameMapPrototype? mapProto)
    {
        if (mapProto == null)
        {
            _gameMapManager.SelectMapByConfigRules();
            mapProto = _gameMapManager.GetSelectedMap();
        }
        GameTicker.LoadGameMap(mapProto!, out MapId mapIdproxy);
        mapId = mapIdproxy;
        _map.InitializeMap(mapId);
    }

    private void OnKillReported(EntityUid uid, IsFighterComponent _, MobStateChangedEvent args)
    {
        if (MobState.Dead != args.NewMobState || Sessions == null) return;
        RemovingRromSession(uid);
    }
    private void PlayerHasDisconnectednected(EntityUid uid, IsFighterComponent _, PlayerDetachedEvent args)
    {
        RemovingRromSession(uid);
    }
    private void RemovingRromSession(EntityUid uid)
    {
        foreach (var session in Sessions)
        {
            if (!session.Players.Contains(uid)) continue;
            session.Players.Remove(uid);

            if (session.Players.Count == 0)
            {
                var query2 = EntityQueryEnumerator<IsFighterComponent, GhostRoleComponent, TransformComponent>();
                int count = 0;
                while (query2.MoveNext(out var guid, out _, out _, out var xform))
                {
                    if (xform.MapID == session.MapId)
                        if (EntityManager.TryGetComponent(guid, out MindContainerComponent? mindContC))
                            if (mindContC.Mind == null && _mobStateSystem.IsAlive(guid)) count++;
                }
                if (count == 0)
                {
                    _map.DeleteMap(session.MapId);
                    Sessions.Remove(session);
                    NewSession();
                }
            }
            break;
        }
    }
    // ТуДу
    // проверка связии айди
    // проверка на случай трансформации
}
