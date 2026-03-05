using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.DynamicAppearance;

[Serializable, NetSerializable]
public enum DynamicAppearanceUiKey
{
    Key,
}

/// <summary>
/// Complete snapshot of the editable appearance fields.
/// Used in BUI messages, BUI state, and as the local draft in the editor window.
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
/// Client → Server: commit the complete appearance draft.
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
/// Server → Client: full appearance snapshot + the entity being edited (for preview).
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearanceBUIState : BoundUserInterfaceState
{
    public DynamicAppearanceState State { get; }

    /// <summary>
    /// The entity whose appearance is being edited. Used by the client to display a live preview.
    /// </summary>
    public NetEntity Entity { get; }

    public DynamicAppearanceBUIState(DynamicAppearanceState state, NetEntity entity)
    {
        State = state;
        Entity = entity;
    }
}
