using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes; 

namespace Content.Shared._NC.Netrunning.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class NetrunnerAvatarComponent : Component
    {
        [DataField("linkedBody"), AutoNetworkedField]
        public NetEntity? LinkedBody;

        [DataField("linkedDeck"), AutoNetworkedField]
        public NetEntity? LinkedDeck;

        // Это твоя главная кнопка. Она добавится через ref.
        [DataField("actionId"), AutoNetworkedField]
        public EntProtoId ActionId = "ActionNetrunnerReturn";

        // Сюда запишется сама сущность кнопки.
        [DataField("actionEntity"), AutoNetworkedField]
        public EntityUid? ActionEntity;
    }
}