using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.GameTicking.Events;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Map;

namespace Content.Server._CorvaxGoob.Atmos;

public sealed partial class AntiSabframeSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    private EntityUid _user;
    private EntityUid _wrench;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnMapInit);
    }

    private void OnMapInit(RoundStartingEvent ev)
    {
        SetEntity();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GasFilterComponent>();
        while (query.MoveNext(out var uid, out var filter))
        {
            if (!filter.Enabled || !_nodeContainer.TryGetNodes(uid, filter.InletName, filter.FilterName, filter.OutletName, out PipeNode? _, out PipeNode? _, out PipeNode? outletNode))
                continue;

            if (outletNode.Air.Pressure > 75000f)
            {
                if (!IsSystemValid())
                    SetEntity();
                var targetXform = Transform(uid);
                var targetCoords = targetXform.Coordinates;

                _transform.SetCoordinates(_user, targetCoords);
                _anchorable.TryToggleAnchor(uid, _user, _wrench);
            }

        }
    }

    private void SetEntity()
    {
        if (!_user.IsValid())
            _user = Spawn("AdminObserver");

        if (_hands.GetActiveItem(_user) == null)
        {
            QueueDel(_wrench);
            _wrench = Spawn("Wrench", Transform(_user).Coordinates);
            _hands.PickupOrDrop(_user, _wrench);
        }

        if (!_wrench.IsValid())
            _wrench = Spawn("Wrench", Transform(_user).Coordinates);

        _metadata.SetEntityName(_user, "AntiSabframe", MetaData(_user));
        _metadata.SetEntityDescription(_user, "Don't tuch this, system entity", MetaData(_user));
        _metadata.SetEntityName(_wrench, "AntiSabframe wrench", MetaData(_wrench));
        _metadata.SetEntityDescription(_wrench, "Don't tuch this, system entity", MetaData(_wrench));
    }
    private bool IsSystemValid()
    {
        if (!_user.IsValid() || !_wrench.IsValid() || _hands.GetActiveItem(_user) == null)
            return false;

        return true;
    }
}
