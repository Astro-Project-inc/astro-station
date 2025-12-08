using Content.Shared._NC.Netrunning.Components;
using Content.Shared._NC.Netrunning;
using Content.Shared.Interaction.Events;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Movement.Components;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Server.Electrocution;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Server.Doors.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._NC.Netrunning
{
    public sealed class NetrunningSystem : EntitySystem
    {
        // =============================================================
        // ЗАВИСИМОСТИ
        // =============================================================
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly FlammableSystem _flammable = default!;
        [Dependency] private readonly SharedDoorSystem _door = default!;
        [Dependency] private readonly AirlockSystem _airlock = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            // 1. Вход и Выход
            SubscribeLocalEvent<CyberdeckComponent, UseInHandEvent>(OnUseDeck);
            SubscribeLocalEvent<NetrunnerAvatarComponent, ReturnToBodyEvent>(OnReturnAction);

            // 2. Безопасность
            SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageTaken);

            // 3. Боевые программы
            SubscribeLocalEvent<NetrunnerAvatarComponent, NetrunnerShockEvent>(OnShockTarget);
            SubscribeLocalEvent<NetrunnerAvatarComponent, NetrunnerIgniteEvent>(OnIgniteTarget);

            // 4. Утилиты (Двери)
            SubscribeLocalEvent<NetrunnerAvatarComponent, NetrunnerDoorToggleEvent>(OnDoorToggle);
        }

        // =============================================================
        // 1. ЛОГИКА ВХОДА В СЕТЬ (JACK IN)
        // =============================================================
        private void OnUseDeck(EntityUid uid, CyberdeckComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;
            var user = args.User;

            if (!_mind.TryGetMind(user, out var mindId, out var mind))
                return;

            var coords = _transform.GetMapCoordinates(user);
            var avatar = Spawn("NCNetrunnerAvatar", coords);

            var avatarComp = EnsureComp<NetrunnerAvatarComponent>(avatar);
            avatarComp.LinkedBody = GetNetEntity(user);
            avatarComp.LinkedDeck = GetNetEntity(uid);

            _mind.TransferTo(mindId, avatar, mind: mind);

            _actions.AddAction(avatar, ref avatarComp.ActionEntity, avatarComp.ActionId);

            if (component.InstalledPrograms != null)
            {
                foreach (var actionId in component.InstalledPrograms)
                {
                    _actions.AddAction(avatar, actionId);
                }
            }

            args.Handled = true;
        }

        // =============================================================
        // 2. ЛОГИКА ВЫХОДА (JACK OUT)
        // =============================================================
        private void OnReturnAction(EntityUid uid, NetrunnerAvatarComponent component, ReturnToBodyEvent args)
        {
            if (args.Handled) return;

            if (component.LinkedBody != null)
            {
                var bodyUid = GetEntity(component.LinkedBody.Value);
                JackOut(uid, bodyUid, component);
                args.Handled = true;
            }
        }

        // =============================================================
        // 3. SHORT CIRCUIT (ШОК)
        // =============================================================
        private void OnShockTarget(EntityUid uid, NetrunnerAvatarComponent component, NetrunnerShockEvent args)
        {
            if (component.LinkedDeck == null) return;
            var deckUid = GetEntity(component.LinkedDeck.Value);
            if (!TryComp<CyberdeckComponent>(deckUid, out var deckComp)) return;

            var target = args.Target;
            if (target == uid) return;

            bool success = _electrocution.TryDoElectrocution(
                target, uid, deckComp.ShockDamage,
                TimeSpan.FromSeconds(deckComp.ShockStunTime),
                refresh: true, ignoreInsulation: true
            );

            if (success)
            {
                Spawn("EffectSparks", _transform.GetMapCoordinates(target));
                _popup.PopupEntity("Вызов короткого замыкания прошел успешно!", target, uid, PopupType.Medium);
                args.Handled = true;
            }
        }

        // =============================================================
        // 4. OVERHEAT (ПОДЖОГ)
        // =============================================================
        private void OnIgniteTarget(EntityUid uid, NetrunnerAvatarComponent component, NetrunnerIgniteEvent args)
        {
            if (component.LinkedDeck == null) return;
            var deckUid = GetEntity(component.LinkedDeck.Value);
            if (!TryComp<CyberdeckComponent>(deckUid, out var deckComp)) return;

            var target = args.Target;
            if (target == uid) return;

            if (!HasComp<FlammableComponent>(target))
            {
                _popup.PopupEntity("Цель не может загореться!", target, uid, PopupType.SmallCaution);
                return;
            }

            _flammable.AdjustFireStacks(target, deckComp.IgniteFireStacks);
            _flammable.Ignite(target, uid);
            _popup.PopupEntity("Вызов перегрева систем, прошел успешно", target, uid, PopupType.MediumCaution);

            args.Handled = true;
        }

        // =============================================================
        // 5. DOOR OVERRIDE (ОТКРЫТЬ/ЗАКРЫТЬ)
        // =============================================================
        private void OnDoorToggle(EntityUid uid, NetrunnerAvatarComponent component, NetrunnerDoorToggleEvent args)
        {
            if (component.LinkedDeck == null) return;
            var target = args.Target;

            if (!TryComp<DoorComponent>(target, out var doorComp))
            {
                _popup.PopupEntity("Invalid Target: Это не дверь!", target, uid, PopupType.SmallCaution);
                return;
            }

            if (doorComp.State == DoorState.Open)
            {
                _door.StartClosing(target, doorComp);
                _popup.PopupEntity("Протокол закрытия активирован.", target, uid, PopupType.Medium);
            }
            else
            {
                _door.StartOpening(target, doorComp);
                _popup.PopupEntity("Протокол открытия активирован.", target, uid, PopupType.Medium);
            }

            args.Handled = true;
        }

        // =============================================================
        // 7. ЗАЩИТА ТЕЛА
        // =============================================================
        private void OnDamageTaken(EntityUid uid, DamageableComponent component, DamageChangedEvent args)
        {
            if (args.DamageDelta == null || args.DamageDelta.Empty) return;

            var query = EntityQueryEnumerator<NetrunnerAvatarComponent>();
            while (query.MoveNext(out var avatarUid, out var avatarComp))
            {
                if (avatarComp.LinkedBody == null) continue;
                var bodyUid = GetEntity(avatarComp.LinkedBody.Value);

                if (bodyUid == uid)
                {
                    _popup.PopupEntity("ЭКСТРЕННЫЙ ВЫХОД!", uid, uid, PopupType.LargeCaution);
                    JackOut(avatarUid, bodyUid, avatarComp);
                    break;
                }
            }
        }

        // =============================================================
        // 8. ПРОВЕРКА РАДИУСА (UPDATE)
        // =============================================================
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<NetrunnerAvatarComponent>();
            while (query.MoveNext(out var avatarUid, out var avatarComp))
            {
                if (avatarComp.LinkedBody == null || avatarComp.LinkedDeck == null)
                {
                    QueueDel(avatarUid);
                    continue;
                }

                var bodyUid = GetEntity(avatarComp.LinkedBody.Value);
                var deckUid = GetEntity(avatarComp.LinkedDeck.Value);

                if (!Exists(bodyUid) || !Exists(deckUid))
                {
                    JackOut(avatarUid, bodyUid, avatarComp);
                    continue;
                }

                if (TryComp<CyberdeckComponent>(deckUid, out var deck))
                {
                    var deckCoords = _transform.GetMapCoordinates(deckUid);
                    var avatarCoords = _transform.GetMapCoordinates(avatarUid);

                    if (deckCoords.MapId != avatarCoords.MapId ||
                        (deckCoords.Position - avatarCoords.Position).Length() > deck.Range)
                    {
                        _popup.PopupEntity("СИГНАЛ ПОТЕРЯН", avatarUid, avatarUid, PopupType.LargeCaution);
                        JackOut(avatarUid, bodyUid, avatarComp);
                    }
                }
            }
        }

        // =============================================================
        // ВСПОМОГАТЕЛЬНЫЙ МЕТОД
        // =============================================================
        private void JackOut(EntityUid avatarUid, EntityUid bodyUid, NetrunnerAvatarComponent? component = null)
        {
            if (_mind.TryGetMind(avatarUid, out var mindId, out var mind))
            {
                if (Exists(bodyUid))
                {
                    _mind.TransferTo(mindId, bodyUid, mind: mind);
                }
            }
            QueueDel(avatarUid);
        }
    }
}