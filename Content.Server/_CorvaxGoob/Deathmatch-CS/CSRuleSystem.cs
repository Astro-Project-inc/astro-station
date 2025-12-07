using Content.Goobstation.Shared.Wraith.Components.Mobs;
using Content.Server.Administration.Commands;
using Content.Server.Cargo.Systems;
using Content.Server.Clothing.Systems;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.GridPreloader;
using Content.Server.KillTracking;
using Content.Server.Maps;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Server.RoundEnd;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._CorvaxGoob.Deathmatch_CS;
using Content.Shared.CCVar;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Points;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.Extensions.Configuration;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Content.Server._CorvaxGoob.Deathmatch_CS;

public sealed class CSRuleSystem : GameRuleSystem<CSRuleComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly OutfitSystem _outfitSystem = default!;
    [Dependency] private readonly RespawnRuleSystem _respawn = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly GridPreloaderSystem _gridPreloader = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;

    public List<Session> SessionsListS = new();
    public sealed class Session
    {
        public MapId MapId;
        public List<EntityUid> Players_ = new();
    }
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleStartedEvent>(RStart);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }
    private void RStart(ref GameRuleStartedEvent i)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dm, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;
            NewSession(dm);
        }
    }
    private void NewSession(CSRuleComponent cscomp)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dm, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                return;
            while ((this.SessionsListS?.Count ?? 0) < cscomp.NumberofSessions)
            {
                Session newsession = new();
                Addmap(out var grids, out var mapId);
                newsession.MapId = mapId;
                _map.InitializeMap(mapId);
                SessionsListS?.Add(newsession);
            }
        }
    }
    private void Addmap(out IReadOnlyList<EntityUid> grids, out MapId mapId)
    {
        var mainStationMap = _gameMapManager.GetSelectedMap();
        var maps = new List<GameMapPrototype>();
        if (mainStationMap == null)
        {
            _gameMapManager.SelectMapByConfigRules();
            mainStationMap = _gameMapManager.GetSelectedMap();
        }
        if (mainStationMap != null)
        {
            maps.Add(mainStationMap);
        }
        grids = GameTicker.LoadGameMap(mainStationMap!, out MapId mapId1, null); mapId = mapId1;
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dm, out _, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;
            foreach (var session in SessionsListS)
            {
                foreach (var player in session.Players_)
                {
                    if (player == ev.Entity)
                    {
                        session.Players_.Remove(ev.Entity);
                        if (session.Players_.Count == 0)
                        { _map.DeleteMap(session.MapId); SessionsListS.Remove(session); NewSession(dm); }
                        break;
                    }
                }
                break;
            }
        }
    }
}
