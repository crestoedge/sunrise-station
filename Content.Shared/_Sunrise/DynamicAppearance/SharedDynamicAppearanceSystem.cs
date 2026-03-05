using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.DynamicAppearance;

[Serializable, NetSerializable]
public enum DynamicAppearanceUiKey
{
    Key,
}

/// <summary>
/// Реальное хранилище всего состояния редактируемой внешности.
/// Используется и в сообщениях, и в классе состояния UI, так и для временного хранения временного состояния в редакторе.
/// </summary>
[Serializable, NetSerializable]
public record struct DynamicAppearanceState(
    MarkingSet MarkingSet,
    string Species,
    Sex Sex,
    int Age,
    Gender Gender,
    string Voice,
    Color SkinColor,
    Color EyeColor,
    Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers,
    float Width,
    float Height
);

/// <summary>
/// Client -> Server: commit the complete appearance draft.
/// Replaces all previous per-field messages.
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearanceSaveMessage : BoundUserInterfaceMessage
{
    public DynamicAppearanceState State { get; }

    public DynamicAppearanceSaveMessage(DynamicAppearanceState state)
    {
        State = state;
    }
}

/// <summary>
/// Server -> Client: full appearance snapshot to populate the editor UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearanceBUIState : BoundUserInterfaceState
{
    public DynamicAppearanceState State { get; }

    public DynamicAppearanceBUIState(DynamicAppearanceState state)
    {
        State = state;
    }
}
