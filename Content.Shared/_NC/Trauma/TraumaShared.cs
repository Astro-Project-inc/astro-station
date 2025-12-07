using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trauma
{
    // Уровни подписки Trauma Team
    [Serializable, NetSerializable]
    public enum TraumaSubscriptionTier : byte
    {
        None = 0,    // Нет подписки
        Bronze,      // Базовая (по умолчанию)
        Silver,
        Gold,
        Platinum     // Элита
    }

    // Уникальный ключ для открытия интерфейса
    [Serializable, NetSerializable]
    public enum TraumaComputerUiKey
    {
        Key
    }

    // Структура данных об одном пациенте для передачи по сети
    [Serializable, NetSerializable]
    public struct TraumaPatientData
    {
        public NetEntity EntityUid; // Сетевой ID сущности
        public string Name;         // Имя персонажа
        public string HealthStatus; // Состояние (Alive, Critical, Dead)
        public TraumaSubscriptionTier Subscription; // Текущая подписка
    }

    // Состояние интерфейса (Сервер -> Клиент)
    // Отправляется каждый раз, когда данные меняются
    [Serializable, NetSerializable]
    public sealed class TraumaComputerState : BoundUserInterfaceState
    {
        public List<TraumaPatientData> Patients;

        public TraumaComputerState(List<TraumaPatientData> patients)
        {
            Patients = patients;
        }
    }

    // Сообщение о смене подписки (Клиент -> Сервер)
    // Отправляется, когда админ меняет подписку в меню
    [Serializable, NetSerializable]
    public sealed class TraumaChangeSubscriptionMsg : BoundUserInterfaceMessage
    {
        public NetEntity TargetEntity;
        public TraumaSubscriptionTier NewTier;

        public TraumaChangeSubscriptionMsg(NetEntity target, TraumaSubscriptionTier tier)
        {
            TargetEntity = target;
            NewTier = tier;
        }
    }
}