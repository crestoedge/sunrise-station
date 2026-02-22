using Content.Shared.Actions;

namespace Content.Shared._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Ивент на активацию встроенного предмета с взаимодействием с целью
/// </summary>
public sealed partial class ModuleInnateInteractionItemEvent : EntityTargetActionEvent
{
    public readonly EntityUid Item;

    public ModuleInnateInteractionItemEvent(EntityUid item)
        : this()
    {
        Item = item;
    }
}
