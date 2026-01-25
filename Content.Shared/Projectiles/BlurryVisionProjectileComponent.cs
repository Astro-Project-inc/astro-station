using Robust.Shared.GameStates;  
  
namespace Content.Shared.Projectiles;  
  
/// <summary>  
/// Applies blurry vision status effect when this projectile hits a target  
/// </summary>  
[RegisterComponent, NetworkedComponent]  
public sealed partial class BlurryVisionProjectileComponent : Component  
{  
    /// <summary>  
    /// Duration of the blurry vision effect in seconds  
    /// </summary>  
    [DataField("duration")]  
    public float Duration = 5f;  
}
