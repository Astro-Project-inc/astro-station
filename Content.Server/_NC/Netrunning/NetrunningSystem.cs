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

            var avatarComp = EnsureComp<NetrunnerAvatarComponent>(avatar);
            avatarComp.LinkedBody = GetNetEntity(user);
            avatarComp.LinkedDeck = GetNetEntity(uid);

            // 1. ПЕРЕНОС РАЗУМА (Сначала разум, потом кнопки — это важно для UI!)
            _mind.TransferTo(mindId, avatar, mind: mind);

            // 2. ВЫДАЕМ ГЛАВНУЮ КНОПКУ (Совет из Discord)
            // Использование 'ref avatarComp.ActionEntity' гарантирует, что 
            // сервер запомнит эту кнопку и привяжет её к компоненту.
            _actions.AddAction(avatar, ref avatarComp.ActionEntity, avatarComp.ActionId);

            // 3. ВЫДАЕМ ОСТАЛЬНЫЕ ПРОГРАММЫ
            // Если в деке есть дополнительные скрипты (кроме выхода), добавляем их.
            if (component.InstalledPrograms != null)
            {
                foreach (var actionId in component.InstalledPrograms)
                {
                    // Для списка мы используем простую выдачу.
                    // (Чтобы использовать ref для списка, нужно сильно усложнять код компонента, 
                    // для начала хватит и так).
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

        // ... Остальной код (OnDamageTaken, Update, JackOut) без изменений ...
        
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