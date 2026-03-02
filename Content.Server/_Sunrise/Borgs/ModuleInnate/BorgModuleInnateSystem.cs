using Content.Shared._Sunrise.Borgs.ModuleInnate;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Система выдачи встраиваемых предметов и компонентов через модуль.
/// </summary>
public sealed class BorgModuleInnateSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedInteractionSystem _interactions = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    // Название контейнера-хранилища встроенных предметов
    private const string InnateItemsContainerId = "module_innate_items";

    // Прототипы действий над предметами
    private static readonly EntProtoId InnateUseItemAction = "ModuleInnateUseItemAction";
    private static readonly EntProtoId InnateToggleItemAction = "ModuleInnateToggleItemAction";
    private static readonly EntProtoId InnateInteractionItemAction = "ModuleInnateInteractionItemAction";

    /// <summary>
    /// Период обновления баланса зарядов между боргом и встроенными предметами.
    /// </summary>
    private const float ChargeBalanceInterval = 1f;

    private float _chargeBalanceAccumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgModuleInnateComponent, BorgModuleInstalledEvent>(OnInstalled);
        SubscribeLocalEvent<BorgModuleInnateComponent, BorgModuleUninstalledEvent>(OnUninstalled);

        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateUseItemEvent>(OnInnateUseItem);
        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateToggleItemEvent>(OnInnateToggleItem);
        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateInteractionItemEvent>(OnInnateInteractionItem);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _chargeBalanceAccumulator += frameTime;
        if (_chargeBalanceAccumulator < ChargeBalanceInterval)
            return;

        _chargeBalanceAccumulator -= ChargeBalanceInterval;

        BalanceInnateItemCharges();
    }

    /// <summary>
    /// Раз в секунду балансирует заряд между батарейкой борга и предметами, которые тоже имеют батарею.
    /// </summary>
    private void BalanceInnateItemCharges()
    {
        var borgQuery = EntityQueryEnumerator<BorgChassisComponent, PowerCellSlotComponent>();

        while (borgQuery.MoveNext(out var borgUid, out var chassis, out var borgCellSlot))
        {
            // Пытаемся получить основную батарею борга.
            if (!_powerCell.TryGetBatteryFromSlot(borgUid, out var borgBattery))
                continue;

            // Проверяем заряд борга.
            var borgCharge = _battery.GetCharge((borgBattery.Value.Owner, borgBattery.Value.Comp));
            if (borgCharge <= 5f || borgBattery.Value.Comp.MaxCharge <= 5f)
                continue;

            // Батарейки, которые будут отбалансированы, включая батарею борга
            var batteriesToBalance = new List<Entity<BatteryComponent>> { borgBattery.Value };
            // Нужные значения для балансировки
            var totalChargeToBalance = borgCharge;
            var totalMaxChargeToBalance = borgBattery.Value.Comp.MaxCharge;

            // Подготавливаем переменные
            var borgChargeLevel = borgCharge / borgBattery.Value.Comp.MaxCharge;

            // Для всех модулей (берём айтемы из модулей, а не контейнера, чтобы потом можно было задавать рейты заряда в будущем)
            foreach (var moduleUid in chassis.ModuleContainer.ContainedEntities)
            {
                // С компонентом иннейтов
                if (!TryComp<BorgModuleInnateComponent>(moduleUid, out var moduleComp))
                    continue;
                // Для каждого предмета модуля
                foreach (var item in moduleComp.AddedInnateItems)
                {
                    // Если у него есть батарея
                    if (!TryGetItemBattery(item, out var itemBattery))
                        continue;

                    // Добавляем в список балансировки
                    batteriesToBalance.Add(itemBattery);
                    // Ведём учет общего заряда / максимального заряда для балансировки
                    // Умножаем на PowerUseCoefficient для уменьшения потребления
                    totalChargeToBalance += _battery.GetCharge(itemBattery.AsNullable()) * moduleComp.PowerUseCoefficient;
                    totalMaxChargeToBalance += itemBattery.Comp.MaxCharge * moduleComp.PowerUseCoefficient;
                }
            }

            // Считаем уровень балансировки
            var setLevel = totalChargeToBalance / totalMaxChargeToBalance;
            // Раздаем заряд каждой батарее обратно в балансированном виде
            foreach (var battery in batteriesToBalance)
                _battery.SetCharge((battery.Owner, battery.Comp), setLevel * battery.Comp.MaxCharge);
        }
    }

    /// <summary>
    /// Пытается получить батарею встроенного предмета (из PowerCellSlot или прямо из BatteryComponent).
    /// </summary>
    private bool TryGetItemBattery(EntityUid item, out Entity<BatteryComponent> battery)
    {
        // Сперва пробуем через слот аккумулятора, если он есть.
        if (TryComp<PowerCellSlotComponent>(item, out var slot))
        {
            if (_powerCell.TryGetBatteryFromSlotOrEntity((item, slot), out var slotBattery) && slotBattery != null)
            {
                battery = slotBattery.Value;
                return true;
            }
        }

        // Если слота нет, проверяем является ли сам предмет батарейкой.
        if (TryComp<BatteryComponent>(item, out var directBattery))
        {
            battery = (item, directBattery);
            return true;
        }

        battery = default;
        return false;
    }

    /// <summary>
    /// Добавляет нужные компоненты и предметы при "установке" модуля
    /// </summary>
    private void OnInstalled(Entity<BorgModuleInnateComponent> module, ref BorgModuleInstalledEvent args)
    {
        var containerManager = EnsureComp<ContainerManagerComponent>(args.ChassisEnt);
        var container = _containers.EnsureContainer<Container>(
            args.ChassisEnt,
            InnateItemsContainerId,
            containerManager
        );
        // Позволяет встраивать предметы-светильники в модуль
        container.OccludesLight = false;

        EntityManager.AddComponents(args.ChassisEnt, module.Comp.InnateComponents);

        AddItems(args.ChassisEnt, module, container);
    }

    /// <summary>
    /// Удаляет нужные компоненты и предметы при "удалении" модуля
    /// </summary>
    private void OnUninstalled(Entity<BorgModuleInnateComponent> module, ref BorgModuleUninstalledEvent args)
    {
        // Выключаем включенные предметы
        foreach (var enabledItem in module.Comp.ToggledOn)
            _interactions.UseInHandInteraction(args.ChassisEnt, enabledItem, false, true);

        // Чистим сущностей и компоненты
        foreach (var action in module.Comp.Actions)
        {
            _actions.RemoveAction(args.ChassisEnt, action);
            QueueDel(action);
        }
        foreach (var item in module.Comp.AddedInnateItems)
            QueueDel(item);

        EntityManager.RemoveComponents(args.ChassisEnt, module.Comp.InnateComponents);

        // Чистим списки очищения
        module.Comp.Actions.Clear();
        module.Comp.AddedInnateItems.Clear();
    }

    /// <summary>
    /// Добавляет предметы в контейнер, а также создаёт экшены их активации в модуле для тела киборга
    /// </summary>
    private void AddItems(EntityUid chassis, Entity<BorgModuleInnateComponent> module, BaseContainer container)
    {
        foreach (var itemProto in module.Comp.UseItems)
        {
            if (itemProto is null)
                continue;

            AddUseItem(itemProto.Value, chassis, module, container);
        }

        foreach (var itemProto in module.Comp.InteractionItems)
        {
            if (itemProto is null)
                continue;

            AddInteractionItem(itemProto.Value, chassis, module, container);
        }

        foreach (var itemProto in module.Comp.ToggleItems)
        {
            if (itemProto is null)
                continue;

            AddToggleItem(itemProto.Value, chassis, module, container);
        }
    }

    /// <summary>
    /// Добавляет предмет, который активируется в руке, вместе с экшеном для его активации
    /// </summary>
    private void AddUseItem(
        EntProtoId itemProto,
        EntityUid chassis,
        Entity<BorgModuleInnateComponent> module,
        BaseContainer container
    )
    {
        var item = CreateInnateItem(itemProto, module, container);
        var ev = new ModuleInnateUseItemEvent(item);
        var action = CreateAction(item, module.Owner, ev, InnateUseItemAction);
        AssignAction(chassis, module, action);
    }

    /// <summary>
    /// Добавляет предмет, который активируется выбором цели, вместе с экшеном для его активации
    /// </summary>
    private void AddInteractionItem(
        EntProtoId itemProto,
        EntityUid chassis,
        Entity<BorgModuleInnateComponent> module,
        BaseContainer container
    )
    {
        var item = CreateInnateItem(itemProto, module, container);
        var ev = new ModuleInnateInteractionItemEvent(item);
        var action = CreateAction(item, module.Owner, ev, InnateInteractionItemAction);
        AssignAction(chassis, module, action);
    }

    /// <summary>
    /// Создает предмет для использования через экшен, при чем считает его состояние переключаемым
    /// </summary>
    private void AddToggleItem(
        EntProtoId itemProto,
        EntityUid chassis,
        Entity<BorgModuleInnateComponent> module,
        BaseContainer container
    )
    {
        var item = CreateInnateItem(itemProto, module, container);
        var ev = new ModuleInnateToggleItemEvent(item);
        var action = CreateAction(item, module.Owner, ev, InnateToggleItemAction);
        AssignAction(chassis, module, action);
    }

    /// <summary>
    /// Создает предмет для использования через экшены согласно прототипу в заданном контейнере
    /// </summary>
    /// <returns>Сущность предмета</returns>
    private EntityUid CreateInnateItem(
        EntProtoId itemProto,
        Entity<BorgModuleInnateComponent> module,
        BaseContainer container
    )
    {
        var item = Spawn(itemProto);
        module.Comp.AddedInnateItems.Add(item);

        // Модифицируем компач юай, чтобы борг наверняка мог его использовать
        if (TryComp<ActivatableUIComponent>(item, out var activatableUIComponent))
        {
            activatableUIComponent.RequiresComplex = false;
            activatableUIComponent.InHandsOnly = false;
            activatableUIComponent.RequireActiveHand = false;
            Dirty(item, activatableUIComponent);
        }

        // Сохраняем его в контейнере предметов модуля
        _containers.Insert(item, container);

        return item;
    }

    /// <summary>
    /// Согласно прототипу и событию создает экшен для активации данной сущности-предмета
    /// </summary>
    /// <returns>Сущность экшена</returns>
    private EntityUid CreateAction(EntityUid item, EntityUid module, BaseActionEvent assignedEvent, EntProtoId actionProto)
    {
        var actionEnt = Spawn(actionProto);
        // Подгружаем спрайт для экшена в виде модуля
        _actions.SetIcon(actionEnt, new SpriteSpecifier.EntityPrototype(MetaData(module).EntityPrototype!.ID));
        // Заготовка события для экшена
        _actions.SetEvent(actionEnt, assignedEvent);
        // Устанавливаем сущность предмета в качестве иконки
        _actions.SetEntityIcon(actionEnt, item);

        // Даем экшену название и описание предмета
        _metadata.SetEntityName(actionEnt, MetaData(item).EntityName);
        _metadata.SetEntityDescription(actionEnt, MetaData(item).EntityDescription);
        return actionEnt;
    }

    /// <summary>
    /// Добавляет экшен в шасси и сохраняет его в контейнере модуля
    /// </summary>
    private void AssignAction(EntityUid chassis, Entity<BorgModuleInnateComponent> module, EntityUid action)
    {
        // Добавляем экшн в список экшенов и в список компача
        _actionContainer.AddAction(module.Owner, action);
        _actions.AddAction(chassis, action, module.Owner);
        module.Comp.Actions.Add(action);
    }

    private void UseInHand(EntityUid performer, EntityUid item)
    {
        var ev = new UseInHandEvent(performer);
        RaiseLocalEvent(item, ev);
    }

    /// <summary>
    /// Обработчик события использования предмета как будто он в руке
    /// </summary>
    private void OnInnateUseItem(Entity<BorgModuleInnateComponent> ent, ref ModuleInnateUseItemEvent args)
    {
        _interactions.UseInHandInteraction(args.Performer, args.Item, false, true);
        // UseInHand(args.Performer, args.Item);
        args.Handled = true;
    }

    /// <summary>
    /// Обработчик события использования предмета как будто он в руке
    /// </summary>
    private void OnInnateToggleItem(Entity<BorgModuleInnateComponent> ent, ref ModuleInnateToggleItemEvent args)
    {
        // Пытаемся взаимодействовать
        if (!_interactions.UseInHandInteraction(args.Performer, args.Item, false, true))
        {
            args.Handled = true;
            return;
        }

        // Обновляем состояние в соответствии с текущим
        var wasToggled = ent.Comp.ToggledOn.Contains(args.Item);
        if (wasToggled)
            ent.Comp.ToggledOn.Add(args.Item);
        else
            ent.Comp.ToggledOn.Remove(args.Item);

        args.Handled = true;
    }

    /// <summary>
    /// Обработчик события использования предмета на заданной цели
    /// </summary>
    private void OnInnateInteractionItem(Entity<BorgModuleInnateComponent> ent, ref ModuleInnateInteractionItemEvent args)
    {
        _interactions.InteractUsing(
            args.Performer,
            args.Item,
            args.Target,
            Transform(args.Target).Coordinates,
            false,
            false,
            false
        );
        args.Handled = true;
    }
}
