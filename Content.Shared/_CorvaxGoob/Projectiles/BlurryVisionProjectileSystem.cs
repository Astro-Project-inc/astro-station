using Content.Shared.Clothing.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;

namespace Content.Shared._CorvaxGoob.Projectiles;

public sealed class BlurryVisionProjectileSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlurryVisionProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<BlurryVisionProjectileComponent> projectile, ref ProjectileHitEvent args)
    {
        if (args.Target == args.Shooter)
            return;

        var duration = TimeSpan.FromSeconds(projectile.Comp.Duration);
        bool hasProtect = false;
        var slotEnumerator = _inventory.GetSlotEnumerator(args.Target, SlotFlags.HEAD | SlotFlags.EYES | SlotFlags.MASK);
        while (slotEnumerator.MoveNext(out var slot) && !hasProtect)
        {
            if (slot.ContainedEntity is not { } item
                || !TryComp<EyeProtectionComponent>(item, out var eyeProtection)
                || eyeProtection.ProtectionTime < duration)
                continue;

            hasProtect = true;
        }

        if (!hasProtect)
            _status.TryAddStatusEffect<BlurryVisionComponent>(
                args.Target,
                "BlurryVision",
                duration,
                true
            );
    }
}
