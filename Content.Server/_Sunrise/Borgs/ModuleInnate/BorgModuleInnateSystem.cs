using Content.Shared._Sunrise.Borgs.ModuleInnate;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
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

    // Название контейнера-хранилища встроенных предметов
    private const string InnateItemsContainerId = "module_innate_items";

    // Прототипы действий над предметами
    private static readonly EntProtoId InnateUseItemAction = "ModuleInnateUseItemAction";
    private static readonly EntProtoId InnateToggleItemAction = "ModuleInnateToggleItemAction";
    private static readonly EntProtoId InnateInteractionItemAction = "ModuleInnateInteractionItemAction";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgModuleInnateComponent, BorgModuleInstalledEvent>(OnInstalled);
        SubscribeLocalEvent<BorgModuleInnateComponent, BorgModuleUninstalledEvent>(OnUninstalled);

        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateUseItemEvent>(OnInnateUseItem);
        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateToggleItemEvent>(OnInnateToggleItem);
        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateInteractionItemEvent>(OnInnateInteractionItem);
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
        foreach (var enabled in module.Comp.ToggledOn)
            UseInHand(args.ChassisEnt, enabled);

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
        var action = CreateAction(item, ev, InnateUseItemAction);
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
        var action = CreateAction(item, ev, InnateInteractionItemAction);
        AssignAction(chassis, module, action);
    }

    /// <summary>
    /// TODO
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
        var action = CreateAction(item, ev, InnateToggleItemAction);
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
    private EntityUid CreateAction(EntityUid item, BaseActionEvent assignedEvent, EntProtoId actionProto)
    {
        var actionEnt = Spawn(actionProto);
        // Подгружаем спрайт для экшена из прото предмета
        _actions.SetIcon(actionEnt, new SpriteSpecifier.EntityPrototype(MetaData(item).EntityPrototype!.ID));
        // Заготовка события для экшена
        _actions.SetEvent(actionEnt, assignedEvent);

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
        UseInHand(args.Performer, args.Item);
        args.Handled = true;
    }

    /// <summary>
    /// Обработчик события использования предмета как будто он в руке
    /// </summary>
    private void OnInnateToggleItem(Entity<BorgModuleInnateComponent> ent, ref ModuleInnateToggleItemEvent args)
    {
        UseInHand(args.Performer, args.Item);

        // Пытаемся удалить из списка включенных предметов
        // если успешно удалили - значит ставим дефолтный цвет иконки
        // если нет - ставим светло-зелёный и добавляем в список
        if (ent.Comp.ToggledOn.Remove(args.Item))
        {
            _actions.SetIconColor(args.Action.AsNullable(), Color.White);
        }
        else
        {
            _actions.SetIconColor(args.Action.AsNullable(), Color.PaleGreen);
            ent.Comp.ToggledOn.Add(args.Item);
        }

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
