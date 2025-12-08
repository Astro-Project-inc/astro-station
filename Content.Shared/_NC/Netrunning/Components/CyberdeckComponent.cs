using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Netrunning.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class CyberdeckComponent : Component
    {
        [DataField("range"), AutoNetworkedField]
        public float Range = 15.0f;

        // Урон от способности "Short Circuit"
        [DataField("shockDamage"), AutoNetworkedField]
        public int ShockDamage = 20;

        // Время оглушения в секундах
        [DataField("shockStunTime"), AutoNetworkedField]
        public float ShockStunTime = 3.0f;

        [DataField("installedPrograms")]
        public List<EntProtoId> InstalledPrograms = new();
    }
}