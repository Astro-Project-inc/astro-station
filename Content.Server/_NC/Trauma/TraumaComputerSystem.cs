using Content.Shared._NC.Trauma;
using Content.Shared._NC.Trauma.Components;
using Content.Shared.Mobs; // Нужно для Enum MobState (если понадобится)
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems; // Здесь живут IsDead, IsCritical
using Robust.Shared.Player;
using Robust.Server.GameObjects;

namespace Content.Server._NC.Trauma
{
    public sealed class TraumaComputerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        // Подключаем систему состояния мобов
        [Dependency] private readonly MobStateSystem _mobState = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TraumaComputerComponent, TraumaChangeSubscriptionMsg>(OnSubscriptionChange);
            SubscribeLocalEvent<TraumaComputerComponent, BoundUIOpenedEvent>(OnUiOpen);
        }

        private void OnUiOpen(EntityUid uid, TraumaComputerComponent component, BoundUIOpenedEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnSubscriptionChange(EntityUid uid, TraumaComputerComponent component, TraumaChangeSubscriptionMsg args)
        {
            var targetEntity = GetEntity(args.TargetEntity);

            if (TryComp<TraumaSubscriberComponent>(targetEntity, out var subscriber))
            {
                subscriber.Tier = args.NewTier;
                Dirty(targetEntity, subscriber);
                UpdateUserInterface(uid, component);
            }
        }

        private void UpdateUserInterface(EntityUid uid, TraumaComputerComponent? component = null)
        {
            if (!Resolve(uid, ref component)) return;

            var patients = new List<TraumaPatientData>();

            // Используем ActorComponent, чтобы найти только живых игроков (онлайн)
            var query = EntityQueryEnumerator<TraumaSubscriberComponent, MetaDataComponent, MobStateComponent, ActorComponent>();
            
            while (query.MoveNext(out var entity, out var sub, out var meta, out var mobState, out _))
            {
            
                string status = "Unknown";

                if (_mobState.IsDead(entity, mobState))
                {
                    status = "Dead";
                }
                else if (_mobState.IsCritical(entity, mobState))
                {
                    status = "Critical";
                }
                else
                {
                    status = "Alive"; // Или "Healthy"
                }

                patients.Add(new TraumaPatientData
                {
                    EntityUid = GetNetEntity(entity),
                    Name = meta.EntityName,
                    HealthStatus = status,
                    Subscription = sub.Tier
                });
            }

            _ui.SetUiState(uid, TraumaComputerUiKey.Key, new TraumaComputerState(patients));
        }
    }
}