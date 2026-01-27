using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;

namespace Content.Shared._CorvaxGoob.Projectiles;

public sealed class BlurryVisionProjectileSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlurryVisionProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<BlurryVisionProjectileComponent> projectile, ref ProjectileHitEvent args)
    {
        if (args.Target == args.Shooter)
            return;

        _status.TryAddStatusEffect<BlurryVisionComponent>(
            args.Target,
            "BlurryVision",
            TimeSpan.FromSeconds(projectile.Comp.Duration),
            true
        );
    }
}
