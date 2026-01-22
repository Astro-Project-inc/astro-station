using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._CorvaxGoob.Clothing;

/// <summary>
///     A system for the operation of a component that prohibits the others player from taking off his own clothes that have this component.
/// </summary>
public sealed class OtherUnremovableClothingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _state = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OtherUnremovableClothingComponent, BeingUnequippedAttemptEvent>(OnUnequip);
        SubscribeLocalEvent<OtherUnremovableClothingComponent, ExaminedEvent>(OnUnequipMarkup);
    }

    private void OnUnequip(Entity<OtherUnremovableClothingComponent> otherUnremovableClothing, ref BeingUnequippedAttemptEvent args)
    {
        if (TryComp<ClothingComponent>(otherUnremovableClothing, out var clothing) && (clothing.Slots & args.SlotFlags) == SlotFlags.NONE)
            return;

        if (args.UnEquipTarget != args.Unequipee || _state.IsDead(args.UnEquipTarget))
        {
            args.Cancel();
        }
    }

    private void OnUnequipMarkup(Entity<OtherUnremovableClothingComponent> otherUnremovableClothing, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("comp-other-unremovable-clothing"));
    }
}
