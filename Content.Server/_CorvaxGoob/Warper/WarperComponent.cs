namespace Content.Server._CorvaxGoob.Warper;

[RegisterComponent]
public sealed partial class WarperComponent : Component
{
    /// Warp destination unique identifier.
    [ViewVariables(VVAccess.ReadWrite)] [DataField("id")]
    public string? Id { get; set; }
}
