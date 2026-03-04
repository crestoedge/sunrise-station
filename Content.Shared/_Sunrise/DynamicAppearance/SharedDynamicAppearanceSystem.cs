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
/// Client -> Server: commit the complete appearance draft.
/// Replaces all previous per-field messages.
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearanceSaveMessage : BoundUserInterfaceMessage
{
    public MarkingSet MarkingSet { get; }
    public Sex Sex { get; }
    public int Age { get; }
    public Gender Gender { get; }
    public string Voice { get; }
    public Color SkinColor { get; }
    public Color EyeColor { get; }
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers { get; }
    public float Width { get; }
    public float Height { get; }

    public DynamicAppearanceSaveMessage(
        MarkingSet markingSet,
        Sex sex,
        int age,
        Gender gender,
        string voice,
        Color skinColor,
        Color eyeColor,
        Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayers,
        float width,
        float height)
    {
        MarkingSet = markingSet;
        Sex = sex;
        Age = age;
        Gender = gender;
        Voice = voice;
        SkinColor = skinColor;
        EyeColor = eyeColor;
        CustomBaseLayers = customBaseLayers;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// Server -> Client: full appearance snapshot to populate the editor UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearanceState : BoundUserInterfaceState
{
    public MarkingSet MarkingSet { get; }
    public string Species { get; }
    public Sex Sex { get; }
    public int Age { get; }
    public Gender Gender { get; }
    public string Voice { get; }
    public Color SkinColor { get; }
    public Color EyeColor { get; }
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers { get; }
    public float Width { get; }
    public float Height { get; }

    public DynamicAppearanceState(
        MarkingSet markingSet,
        string species,
        Sex sex,
        int age,
        Gender gender,
        string voice,
        Color skinColor,
        Color eyeColor,
        Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayers,
        float width,
        float height)
    {
        MarkingSet = markingSet;
        Species = species;
        Sex = sex;
        Age = age;
        Gender = gender;
        Voice = voice;
        SkinColor = skinColor;
        EyeColor = eyeColor;
        CustomBaseLayers = customBaseLayers;
        Width = width;
        Height = height;
    }
}
