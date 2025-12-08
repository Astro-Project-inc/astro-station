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

        // === ПАРАМЕТРЫ ПЕРЕГРЕВА (ОГНЯ) ===
        // Количество стаков огня. 
        // 1-2 стака = легкое возгорание.
        // 5+ стаков = сильный пожар.
        [DataField("igniteFireStacks"), AutoNetworkedField]
        public float IgniteFireStacks = 3.0f;

        [DataField("installedPrograms")]
        public List<EntProtoId> InstalledPrograms = new();
    }
}