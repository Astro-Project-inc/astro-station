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
using Content.Server.Electrocution; // Нужно для системы электричества
using Content.Server.Atmos.EntitySystems; // НУЖНО ДЛЯ ОГНЯ (FlammableSystem)
using Content.Server.Atmos.Components;
using Content.Shared.Actions.Components;

namespace Content.Server._NC.Netrunning
{
    public sealed class NetrunningSystem : EntitySystem
    {
        // Инъекция зависимостей (систем, которые мы будем использовать)
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly FlammableSystem _flammable = default!;

        public override void Initialize()
        {
            base.Initialize();

            // 1. Активация деки в руке (Z) -> Создание аватара
            SubscribeLocalEvent<CyberdeckComponent, UseInHandEvent>(OnUseDeck);

            // 2. Нажатие кнопки "Jack Out" -> Возврат в тело
            SubscribeLocalEvent<NetrunnerAvatarComponent, ReturnToBodyEvent>(OnReturnAction);

            // 3. Получение урона телом -> Экстренный возврат
            SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageTaken);

            // 4. Использование боевой способности "Short Circuit"
            SubscribeLocalEvent<NetrunnerAvatarComponent, NetrunnerShockEvent>(OnShockTarget);

            // 5. Использование боевой способности "Ignite"
            SubscribeLocalEvent<NetrunnerAvatarComponent, NetrunnerIgniteEvent>(OnIgniteTarget);
            
        }

        // =============================================================
        // 1. ВХОД В СЕТЬ (Логика спавна)
        // =============================================================
        private void OnUseDeck(EntityUid uid, CyberdeckComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;
            var user = args.User;

            // Проверяем, есть ли у игрока Разум (Mind)
            if (!_mind.TryGetMind(user, out var mindId, out var mind))
                return;

            // Спавним аватара в той же точке, где стоит пользователь
            var coords = _transform.GetMapCoordinates(user);
            var avatar = Spawn("NCNetrunnerAvatar", coords);

            // Навешиваем и настраиваем компонент связи
            var avatarComp = EnsureComp<NetrunnerAvatarComponent>(avatar);
            avatarComp.LinkedBody = GetNetEntity(user); // Запоминаем ID тела
            avatarComp.LinkedDeck = GetNetEntity(uid);  // Запоминаем ID деки (для статов)

            // Переносим сознание игрока в аватара
            _mind.TransferTo(mindId, avatar, mind: mind);

            // Выдаем кнопку принудительного выхода ("Jack Out")
            _actions.AddAction(avatar, ref avatarComp.ActionEntity, avatarComp.ActionId);

            // Выдаем остальные программы, установленные в деке (включая "Short Circuit")
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
        // 2. ВЫХОД ПО КНОПКЕ
        // =============================================================
        private void OnReturnAction(EntityUid uid, NetrunnerAvatarComponent component, ReturnToBodyEvent args)
        {
            if (args.Handled) return;

            // Если тело существует — возвращаемся
            if (component.LinkedBody != null)
            {
                // Конвертируем NetEntity обратно в EntityUid
                var bodyUid = GetEntity(component.LinkedBody.Value);
                JackOut(uid, bodyUid, component);
                args.Handled = true;
            }
        }

        // =============================================================
        // 3. БОЕВАЯ СИСТЕМА (Short Circuit)
        // =============================================================
        private void OnShockTarget(EntityUid uid, NetrunnerAvatarComponent component, NetrunnerShockEvent args)
        {
            // Эта функция срабатывает, когда игрок нажал кнопку и выбрал цель мышкой

            // А. Проверяем связь с декой (откуда брать урон)
            if (component.LinkedDeck == null)
            {
                _popup.PopupEntity("Error: Deck signal lost!", uid, uid, PopupType.MediumCaution);
                return;
            }

            // Б. Получаем сущность деки
            var deckUid = GetEntity(component.LinkedDeck.Value);

            // В. Читаем настройки урона из деки
            if (!TryComp<CyberdeckComponent>(deckUid, out var deckComp))
            {
                return;
            }

            var target = args.Target;


            // Нельзя хакнуть самого себя
            if (target == uid) return;

            // Г. Наносим удар
            // TryDoElectrocution делает всё сама: наносит дамага, станит, рисует молнию
            bool success = _electrocution.TryDoElectrocution(
                target,                 // Кого бьем
                uid,                    // Кто бьет (источник)
                deckComp.ShockDamage,   // Урон (берем из деки)
                TimeSpan.FromSeconds(deckComp.ShockStunTime), // Длительность стана
                refresh: true,          // Обновить таймер, если уже в стане
                ignoreInsulation: true  // Игнорировать диэлектрические перчатки (это же взлом!)
            );

            if (success)
            {
                // 1. Получаем координаты жертвы
                var targetCoords = _transform.GetMapCoordinates(target);

                // 2. Спавним эффект искр в этой точке
                // Можно использовать "EffectSparks" или "EffectSparksHighVoltage"
                Spawn("EffectSparks", targetCoords);

                // (Опционально) Можно проиграть дополнительный звук, если звук ElectrocutionSystem слишком тихий
                // _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/sparks4.ogg"), target);

                _popup.PopupEntity("Target System Overloaded!", target, uid, PopupType.Medium);
                args.Handled = true;
            }
        }

        // =============================================================
        // СПОСОБНОСТЬ: OVERHEAT (ПОДЖОГ)
        // =============================================================
        private void OnIgniteTarget(EntityUid uid, NetrunnerAvatarComponent component, NetrunnerIgniteEvent args)
        {
            // 1. Проверка деки
            if (component.LinkedDeck == null) return;
            var deckUid = GetEntity(component.LinkedDeck.Value);
            if (!TryComp<CyberdeckComponent>(deckUid, out var deckComp)) return;

            var target = args.Target;
            if (target == uid) return;

            // 2. Проверяем, может ли цель гореть
            // Если у цели нет компонента Flammable, мы не сможем её поджечь.
            if (!HasComp<FlammableComponent>(target))
            {
                _popup.PopupEntity("Target is fireproof!", target, uid, PopupType.SmallCaution);
                return;
            }

            // 3. ПОДЖИГАЕМ
            // Добавляем стаки огня (интенсивность)
            _flammable.AdjustFireStacks(target, deckComp.IgniteFireStacks);
            // Заставляем загореться прямо сейчас
            _flammable.Ignite(target, uid);

            // Визуал и звук
            _popup.PopupEntity("Thermal Optics Overloaded!", target, uid, PopupType.MediumCaution);
            // Звук огня проиграется сам системой Flammable, но можно добавить свой звук хака:
            // _audio.PlayPvs(..., target);

            args.Handled = true;
        }

        // =============================================================
        // 4. ЗАЩИТА ТЕЛА (Выход при получении урона)
        // =============================================================
        private void OnDamageTaken(EntityUid uid, DamageableComponent component, DamageChangedEvent args)
        {
            // Если урона нет, игнорируем
            if (args.DamageDelta == null || args.DamageDelta.Empty) return;

            // 'uid' здесь — это сущность, которая получила урон (Тело).
            // Нам нужно найти всех аватаров и проверить, чей это "хост".

            var query = EntityQueryEnumerator<NetrunnerAvatarComponent>();
            while (query.MoveNext(out var avatarUid, out var avatarComp))
            {
                if (avatarComp.LinkedBody == null) continue;

                var bodyUid = GetEntity(avatarComp.LinkedBody.Value);

                // Если пострадавшее тело принадлежит этому аватару
                if (bodyUid == uid)
                {
                    _popup.PopupEntity("EMERGENCY DISCONNECT!", uid, uid, PopupType.LargeCaution);
                    JackOut(avatarUid, bodyUid, avatarComp);
                    break;
                }
            }
        }

        // =============================================================
        // 5. ПРОВЕРКА РАДИУСА (Update Loop)
        // =============================================================
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Проходимся по всем активным аватарам в игре
            var query = EntityQueryEnumerator<NetrunnerAvatarComponent>();
            while (query.MoveNext(out var avatarUid, out var avatarComp))
            {
                // Если ссылки на тело или деку потеряны — удаляем аватара
                if (avatarComp.LinkedBody == null || avatarComp.LinkedDeck == null)
                {
                    QueueDel(avatarUid);
                    continue;
                }

                var bodyUid = GetEntity(avatarComp.LinkedBody.Value);
                var deckUid = GetEntity(avatarComp.LinkedDeck.Value);

                // Если тело или дека уничтожены
                if (!Exists(bodyUid) || !Exists(deckUid))
                {
                    JackOut(avatarUid, bodyUid, avatarComp);
                    continue;
                }

                // Проверка дистанции
                if (TryComp<CyberdeckComponent>(deckUid, out var deck))
                {
                    var deckCoords = _transform.GetMapCoordinates(deckUid);
                    var avatarCoords = _transform.GetMapCoordinates(avatarUid);

                    // Если разные карты (например, шаттл улетел) или дистанция больше Range
                    if (deckCoords.MapId != avatarCoords.MapId ||
                        (deckCoords.Position - avatarCoords.Position).Length() > deck.Range)
                    {
                        _popup.PopupEntity("SIGNAL LOST", avatarUid, avatarUid, PopupType.LargeCaution);
                        JackOut(avatarUid, bodyUid, avatarComp);
                    }
                }
            }
        }

        // =============================================================
        // ВСПОМОГАТЕЛЬНЫЙ МЕТОД: JACK OUT
        // =============================================================
        private void JackOut(EntityUid avatarUid, EntityUid bodyUid, NetrunnerAvatarComponent? component = null)
        {
            // Пытаемся найти разум в аватаре
            if (_mind.TryGetMind(avatarUid, out var mindId, out var mind))
            {
                // Если тело еще существует — возвращаем разум туда
                if (Exists(bodyUid))
                {
                    _mind.TransferTo(mindId, bodyUid, mind: mind);
                }
            }
            // Уничтожаем аватара
            QueueDel(avatarUid);
        }
    }
}