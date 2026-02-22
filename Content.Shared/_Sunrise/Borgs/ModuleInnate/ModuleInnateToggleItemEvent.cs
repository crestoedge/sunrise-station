using Content.Shared.Actions;

namespace Content.Shared._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Ивент на активацию встроенного предмета, но предполагает, что предмет переключается
/// </summary>
public sealed partial class ModuleInnateToggleItemEvent : InstantActionEvent
{
    public readonly EntityUid Item;

    public ModuleInnateToggleItemEvent(EntityUid item)
        : this()
    {
        Item = item;
    }
}
