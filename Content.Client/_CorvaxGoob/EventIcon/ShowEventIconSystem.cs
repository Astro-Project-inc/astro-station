using Content.Shared._CorvaxGoob.EventIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._CorvaxGoob.EventIcon;

public sealed class ShowEventIconSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventIconComponent, GetStatusIconsEvent>(GetEventIcons);
    }

    private void GetEventIcons(Entity<EventIconComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}
