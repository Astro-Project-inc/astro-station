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
using Content.Shared._CorvaxGoob.Deathmatch_CS;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.Deathmatch_CS;

public sealed class CSRuleSystem : GameRuleSystem<CSRuleComponent>
{

    [Dependency] private readonly MapSystem _map = default!;
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
    private void RStart(ref GameRuleStartedEvent _)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dm, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;
            NewSession(dm);
            /*if (GameTicker.GetAddedGameRules().Count() < 3)
            {
                foreach (var i in _map.GetAllMapIds())
                {
                    if (!_map.MapExists(i)) continue;
                    var bl = true;
                    foreach (var i2 in SessionsListS)
                        if (i2.MapId == i) bl = false;
                    if (bl) _map.DeleteMap(i);
                }
            }*/
        }
    }
    public void NewSession(CSRuleComponent cscomp)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dm, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                return;
            if ((this.SessionsListS?.Count ?? 0) < cscomp.NumberofSessions)
            {
                Session newsession = new();
                Addmap(out var mapId);
                newsession.MapId = mapId;
                _map.InitializeMap(mapId);
                SessionsListS?.Add(newsession);
            }
        }
    }
    private void Addmap(out MapId mapId)
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
        GameTicker.LoadGameMap(mainStationMap!, out MapId mapId1);
        mapId = mapId1;
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<CSRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var guid, out var dm, out _, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(guid, rule))
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
    // ТуДу
    // Отслеживание выхода игрока из игры
    // проверка связий айди в механиках регистрации сессии
    // проверка на отслеживание суицида
    // проверка, на случай трансформации, не записывать игрока в сессию дважды, переделать на сикей?...
}
