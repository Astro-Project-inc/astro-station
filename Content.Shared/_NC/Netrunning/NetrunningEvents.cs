using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Netrunning
{
    // Событие должно быть в Shared, чтобы клиент мог отправить его на сервер при нажатии кнопки
    public sealed partial class ReturnToBodyEvent : InstantActionEvent 
    { 
    }
}

