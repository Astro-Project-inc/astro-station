using Content.Server.Clothing.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GridPreloader;
using Content.Server.KillTracking;
using Content.Server.Maps;
using Content.Server.Mind;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._CorvaxGoob.Deathmatch_CS;

public sealed class CSRuleSystem : GameRuleSystem<CSRuleComponent>
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;

    public List<Session> Sessions = new();
    public sealed class Session
    {
        public MapId MapId;
        public List<EntityUid> Players = new();
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRuleStartedEvent>(RoundStart);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(MapClearing);
    }

    private void RoundStart(ref GameRuleStartedEvent _)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uId, out var csRuleC, out var gRuleC))
        {
            if (!GameTicker.IsGameRuleActive(uId, gRuleC))
                continue;

            NewSession(csRuleC);
        }
    }

    public void NewSession(CSRuleComponent csRuleC)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uId, out _, out var gRuleC))
        {
            if (!GameTicker.IsGameRuleActive(uId, gRuleC))
                return;

            if ((this.Sessions?.Count ?? 0) < csRuleC.NumberOfSessions)
            {
                Session newsession = new();
                Addmap(out var mapId);
                newsession.MapId = mapId;
                _map.InitializeMap(mapId);
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
    private void Addmap(out MapId mapId)
    {
        var protoSelectedMap = _gameMapManager.GetSelectedMap();
        if (protoSelectedMap == null)
        {
            _gameMapManager.SelectMapByConfigRules();
            protoSelectedMap = _gameMapManager.GetSelectedMap();
        }
        GameTicker.LoadGameMap(protoSelectedMap!, out MapId mapIdproxy);
        mapId = mapIdproxy;
    }

    private void OnKillReported(ref KillReportedEvent ev) // эта поебота вообще пашет?
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uId, out var csRuleC, out var gRuleC))
        {
            if (!GameTicker.IsGameRuleActive(uId, gRuleC))
                return;
            foreach (var session in Sessions)
            {
                foreach (var player in session.Players)
                {
                    if (player == ev.Entity)
                    {
                        session.Players.Remove(ev.Entity);
                        if (session.Players.Count == 0)
                        { _map.DeleteMap(session.MapId); Sessions.Remove(session); NewSession(csRuleC); }
                        break;
                    }
                }
                break;
            }
        }
    }
    // ТуДу
    // Отслеживание выхода игрока из игры
    // проверка связий айди в механиках регистрации сессии
    // проверка на отслеживание суицида
    // проверка на случай трансформации
    // добавить рандомный выбор карты из маппула для сессий от компача
    //вырезать трекер компонент
}
