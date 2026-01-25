using Content.Shared.Eye.Blinding.Components;  
using Content.Shared.StatusEffect;  
using Content.Shared.Weapons.Ranged.Systems;  
  
namespace Content.Shared.Projectiles;  
  
public sealed class BlurryVisionProjectileSystem : EntitySystem  
{  
    [Dependency] private readonly StatusEffectsSystem _status = default!;  
  
    public override void Initialize()  
    {  
        base.Initialize();  
        SubscribeLocalEvent<BlurryVisionProjectileComponent, ProjectileHitEvent>(OnProjectileHit);  
        SubscribeLocalEvent<HitscanPrototype, HitscanHitEvent>(OnHitscanHit);  
    }  
  
    private void OnProjectileHit(Entity<BlurryVisionProjectileComponent> projectile, ref ProjectileHitEvent args)  
    {  
        if (args.Target == null || args.Target == args.Shooter)  
            return;  
  
        var duration = TimeSpan.FromSeconds(projectile.Comp.Duration);  
        _status.TryAddStatusEffect<BlurryVisionComponent>(  
            args.Target.Value,  
            "BlurryVision",  
            duration,  
            true);  
    }  
  
    private void OnHitscanHit(HitscanPrototype proto, ref HitscanHitEvent args)  
    {  
        // Check if this hitscan has blurry vision duration  
        if (proto.BlurryVisionDuration == null || args.HitEntity == null)  
            return;  
  
        var duration = TimeSpan.FromSeconds(proto.BlurryVisionDuration.Value);  
        _status.TryAddStatusEffect<BlurryVisionComponent>(  
            args.HitEntity.Value,  
            "BlurryVision",  
            duration,  
            true);  
    }  
}
