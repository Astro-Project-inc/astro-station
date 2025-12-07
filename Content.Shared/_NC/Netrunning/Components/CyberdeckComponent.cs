using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Netrunning.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class CyberdeckComponent : Component
    {
        [DataField("range"), AutoNetworkedField]
        public float Range = 15.0f;

        [DataField("installedPrograms"), AutoNetworkedField]
        public List<string> InstalledPrograms = new()
        {
            "ActionNetrunnerReturn" 
        };
    }
}