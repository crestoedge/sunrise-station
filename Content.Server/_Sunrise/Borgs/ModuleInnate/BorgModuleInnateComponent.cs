using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Компонент, позволяющий давать боргам действия (экшены) и компоненты через модуль
/// </summary>
[RegisterComponent]
public sealed partial class BorgModuleInnateComponent : Component
{
    /// <summary>
    /// Множитель потребления энергии предметами модуля
    /// </summary>
    [DataField]
    public float PowerUseCoefficient = 0.5f;

    /// <summary>
    /// Предметы, которые активируются прямо в руке
    /// </summary>
    [DataField]
    public List<EntProtoId?> UseItems = new();

    /// <summary>
    /// Предметы, с помощью которых можно взаимодействовать с сущностями
    /// </summary>
    [DataField]
    public List<EntProtoId?> InteractionItems = new();

    /// <summary>
    /// Предметы, которые переключаются при активации в руке
    /// </summary>
    [DataField]
    public List<EntProtoId?> ToggleItems = new();

    /// <summary>
    /// Компоненты, которые будут добавлены боргу при установке модуля
    /// Будут удалены после его изъятия!
    /// </summary>
    [DataField]
    public ComponentRegistry InnateComponents = new();

    /// <summary>
    /// Айди добавленных предметов этим модулем
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables]
    public List<EntityUid> AddedInnateItems = new();

    /// <summary>
    /// Экшены для борга, созданные данным модулем
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Actions = new();

    /// <summary>
    /// Список включенных предметов
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables]
    public List<EntityUid> ToggledOn = new();
}
