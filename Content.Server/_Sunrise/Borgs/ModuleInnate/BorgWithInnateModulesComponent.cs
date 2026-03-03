namespace Content.Server._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Компонент, который добавляется боргу при установке модуля с InnateComponents.
/// Нужен для оптимизации EntityQuery, чтобы не опрашивать всех боргов.
/// </summary>
[RegisterComponent]
[Access(typeof(BorgModuleInnateSystem))]
public sealed partial class BorgWithInnateModulesComponent : Component
{
    [ViewVariables]
    public List<EntityUid> Modules = [];
}
