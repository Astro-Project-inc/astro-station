using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Clothing;

/// <summary>
///     The component prohibits the player from taking off clothes on them that have this component.
/// </summary>
/// <remarks>
///     See also ClothingComponent.EquipDelay if you want the clothes that the player cannot take off by others to be put on by the player with a delay.
///</remarks>
[NetworkedComponent]
[RegisterComponent]
[Access(typeof(OtherUnremovableClothingSystem))]
public sealed partial class OtherUnremovableClothingComponent : Component
{
}
