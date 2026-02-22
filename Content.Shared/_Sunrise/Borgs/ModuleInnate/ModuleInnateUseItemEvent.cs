using Content.Shared.Actions;

namespace Content.Shared._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Ивент на активацию встроенного предмета
/// </summary>
public sealed partial class ModuleInnateUseItemEvent : InstantActionEvent
{
    public readonly EntityUid Item;

    public ModuleInnateUseItemEvent(EntityUid item)
        : this()
    {
        Item = item;
    }
}
