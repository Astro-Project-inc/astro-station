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
using Content.Shared.Actions; // <--- Этот using должен остаться

namespace Content.Server._NC.Netrunning
{
    public sealed class NetrunningSystem : EntitySystem
    {
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!; 

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CyberdeckComponent, UseInHandEvent>(OnUseDeck);
            SubscribeLocalEvent<NetrunnerAvatarComponent, ReturnToBodyEvent>(OnReturnAction);
            SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageTaken);
        }

        // =============================================================
        // 1. ВХОД В СЕТЬ
        // =============================================================
        private void OnUseDeck(EntityUid uid, CyberdeckComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;
            var user = args.User;

            if (!_mind.TryGetMind(user, out var mindId, out var mind)) 
                return;

            var coords = _transform.GetMapCoordinates(user);
            var avatar = Spawn("NCNetrunnerAvatar", coords);

            // Настраиваем компонент аватара
            var avatarComp = EnsureComp<NetrunnerAvatarComponent>(avatar);
            avatarComp.LinkedBody = GetNetEntity(user);
            avatarComp.LinkedDeck = GetNetEntity(uid);

            // 1. Сначала переносим разум (чтобы клиент "сел" в аватара)
            _mind.TransferTo(mindId, avatar, mind: mind);

            // 2. ВЫДАЕМ "РОДНОЙ" ЭКШЕН ВЫХОДА (СТИЛЬ JAUNT)
            // Мы передаем ссылку "ref avatarComp.ActionEntity". 
            // Система сама создаст ActionsComponent (если его нет) и запишет туда кнопку.
            _actions.AddAction(avatar, ref avatarComp.ActionEntity, avatarComp.ActionId);

            // 3. ВЫДАЕМ ПРОГРАММЫ ИЗ ДЕКИ (ВАШИ СКРИПТЫ)
            if (component.InstalledPrograms != null)
            {
                foreach (var actionId in component.InstalledPrograms)
                {
                    // Для дополнительных скриптов используем простую перегрузку.
                    // Она просто добавит кнопку в список.
                    _actions.AddAction(avatar, actionId);
                }
            }

            args.Handled = true;
        }

        // =============================================================
        // 2. ВЫХОД ПО КНОПКЕ
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
        // 3. АВТО-ВЫХОД ПРИ УРОНЕ
        // =============================================================
        private void OnDamageTaken(EntityUid uid, DamageableComponent component, DamageChangedEvent args)
        {
            if (args.DamageDelta == null || args.DamageDelta.Empty) return;
            var query = EntityQueryEnumerator<NetrunnerAvatarComponent>();
            while (query.MoveNext(out var avatarUid, out var avatarComp)) {
                if (avatarComp.LinkedBody == null) continue;
                var bodyUid = GetEntity(avatarComp.LinkedBody.Value);
                if (bodyUid == uid) {
                    JackOut(avatarUid, bodyUid, avatarComp);
                    break; 
                }
            }
        }

        // =============================================================
        // 4. ПРОВЕРКА ДИСТАНЦИИ
        // =============================================================
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var query = EntityQueryEnumerator<NetrunnerAvatarComponent>();
            while (query.MoveNext(out var avatarUid, out var avatarComp)) {
                if (avatarComp.LinkedBody == null || avatarComp.LinkedDeck == null) {
                    QueueDel(avatarUid); continue;
                }
                var bodyUid = GetEntity(avatarComp.LinkedBody.Value);
                var deckUid = GetEntity(avatarComp.LinkedDeck.Value);
                if (!Exists(bodyUid) || !Exists(deckUid)) {
                      JackOut(avatarUid, bodyUid, avatarComp); continue;
                }
                if (TryComp<CyberdeckComponent>(deckUid, out var deck)) {
                    var deckCoords = _transform.GetMapCoordinates(deckUid);
                    var avatarCoords = _transform.GetMapCoordinates(avatarUid);
                    if (deckCoords.MapId != avatarCoords.MapId || 
                        (deckCoords.Position - avatarCoords.Position).Length() > deck.Range) {
                        JackOut(avatarUid, bodyUid, avatarComp);
                    }
                }
            }
        }

        // =============================================================
        // 5. ФУНКЦИЯ ВОЗВРАТА
        // =============================================================
        private void JackOut(EntityUid avatarUid, EntityUid bodyUid, NetrunnerAvatarComponent? component = null)
        {
            if (_mind.TryGetMind(avatarUid, out var mindId, out var mind)) {
                if (Exists(bodyUid)) {
                    _mind.TransferTo(mindId, bodyUid, mind: mind);
                }
            }
            QueueDel(avatarUid);
        }
    }
}