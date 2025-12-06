using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Deathmatch_CS;

[RegisterComponent, NetworkedComponent]
public sealed partial class IsFighterComponent : Component
{
    /// <summary>
    ///     The component allows the CS system to recognize the mob as a participant in the battle.
    /// </summary>
    [DataField("IsFighter"), ViewVariables(VVAccess.ReadWrite)]
    public bool IsFighter = true;
}
