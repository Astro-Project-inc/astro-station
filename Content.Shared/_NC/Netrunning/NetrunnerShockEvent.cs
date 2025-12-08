using Content.Shared.Actions;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._NC.Netrunning
{
    /// <summary>
    /// Событие, которое отправляется на сервер, когда нетраннер выбирает цель для "Short Circuit".
    /// </summary>
    [DataDefinition]
    public sealed partial class NetrunnerShockEvent : EntityTargetActionEvent 
    { 
    }
}