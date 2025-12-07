using Robust.Shared.GameStates;

namespace Content.Shared._NC.Trauma.Components
{
    /// <summary>
    /// Вешается на объект компьютера (консоль). Маркер для системы.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class TraumaComputerComponent : Component
    {
        // Здесь пока пусто, так как вся логика в Системе, 
        // но компонент нужен, чтобы мы могли найти компьютер в мире.
    }
}