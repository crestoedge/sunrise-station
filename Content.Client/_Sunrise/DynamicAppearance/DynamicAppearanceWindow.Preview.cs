using System.Linq;
using Content.Client.Humanoid;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Client.Utility;
using Robust.Shared.Map;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client._Sunrise.DynamicAppearance;

/// <summary>
/// Character preview: real entity (clothes ON) or client-side dummy (clothes OFF) + rotation.
/// </summary>
public sealed partial class DynamicAppearanceWindow
{
    // ═══════════ Handler wiring ═══════════

    private void InitPreviewHandlers()
    {
        RotateLeftButton.OnPressed += _ =>
        {
            _previewRotation = _previewRotation.TurnCw();
            ApplyPreviewRotation();
        };

        RotateRightButton.OnPressed += _ =>
        {
            _previewRotation = _previewRotation.TurnCcw();
            ApplyPreviewRotation();
        };

        ShowClothesButton.OnToggled += args =>
        {
            _showClothes = args.Pressed;
            RefreshPreview();
        };
    }

    // ═══════════ Preview refresh ═══════════

    /// <summary>
    /// Switch SpriteView between the real entity (with equipment) and a dummy entity (appearance only).
    /// </summary>
    private void RefreshPreview()
    {
        if (_showClothes)
        {
            // Show the real entity — it has all current equipment visible.
            if (_entManager.EntityExists(_previewEntity))
                SpriteView.SetEntity(_previewEntity);

            DestroyDummy();
        }
        else
        {
            // Show a clothes-off dummy that mirrors the current draft appearance.
            RebuildDummy();

            if (_entManager.EntityExists(_dummyEntity))
                SpriteView.SetEntity(_dummyEntity);
        }

        ApplyPreviewRotation();
    }

    /// <summary>
    /// Lightweight refresh: update the dummy if it exists and is visible (clothes OFF).
    /// Called by handlers after every draft mutation (color changed, marking added, etc.).
    /// </summary>
    private void RefreshDummyPreview()
    {
        if (_showClothes || !_entManager.EntityExists(_dummyEntity))
            return;

        ApplyDraftToDummy();
    }

    // ═══════════ Rotation ═══════════

    private void ApplyPreviewRotation()
    {
        SpriteView.OverrideDirection = (Direction) ((int) _previewRotation % 4 * 2);
    }

    // ═══════════ Dummy lifecycle ═══════════

    /// <summary>
    /// Creates (or re-creates) the client-side dummy entity and applies the current draft appearance.
    /// </summary>
    private void RebuildDummy()
    {
        DestroyDummy();

        if (!_protoMan.TryIndex<SpeciesPrototype>(_draftState.Species, out var speciesProto))
            return;

        _dummyEntity = _entManager.SpawnEntity(speciesProto.DollPrototype, MapCoordinates.Nullspace);
        ApplyDraftToDummy();
    }

    /// <summary>
    /// Applies the current <see cref="_draftState"/> to the dummy entity via a temporary
    /// <see cref="HumanoidCharacterProfile"/> and <see cref="HumanoidAppearanceSystem.LoadProfile"/>.
    /// </summary>
    private void ApplyDraftToDummy()
    {
        if (!_entManager.EntityExists(_dummyEntity))
            return;

        var profile = BuildProfileFromDraft();
        _humanoidSystem.LoadProfile(_dummyEntity, profile);
    }

    private void DestroyDummy()
    {
        if (_entManager.EntityExists(_dummyEntity))
            _entManager.DeleteEntity(_dummyEntity);

        _dummyEntity = EntityUid.Invalid;
    }

    // ═══════════ Profile construction ═══════════

    /// <summary>
    /// Builds a minimal <see cref="HumanoidCharacterProfile"/> suitable for
    /// <see cref="HumanoidAppearanceSystem.LoadProfile"/> from the current draft state.
    /// </summary>
    private HumanoidCharacterProfile BuildProfileFromDraft()
    {
        // Extract hair / facial hair from MarkingSet so we can populate the profile fields.
        var hairMarking = _draftState.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairList)
            ? hairList.FirstOrDefault()
            : null;

        var facialHairMarking = _draftState.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialList)
            ? facialList.FirstOrDefault()
            : null;

        var hairId = hairMarking?.MarkingId ?? HairStyles.DefaultHairStyle;
        var hairColor = hairMarking != null && hairMarking.MarkingColors.Count > 0
            ? hairMarking.MarkingColors[0]
            : Color.Black;

        var facialId = facialHairMarking?.MarkingId ?? HairStyles.DefaultFacialHairStyle;
        var facialColor = facialHairMarking != null && facialHairMarking.MarkingColors.Count > 0
            ? facialHairMarking.MarkingColors[0]
            : Color.Black;

        // Extract gradient / marking effects from hair markings.
        var hairEffectType = MarkingEffectType.Color;
        MarkingEffect? hairEffect = null;
        if (hairMarking != null && hairMarking.MarkingEffects.Count > 0)
        {
            hairEffect = hairMarking.MarkingEffects[0];
            hairEffectType = hairEffect.Type;
        }

        var facialEffectType = MarkingEffectType.Color;
        MarkingEffect? facialEffect = null;
        if (facialHairMarking != null && facialHairMarking.MarkingEffects.Count > 0)
        {
            facialEffect = facialHairMarking.MarkingEffects[0];
            facialEffectType = facialEffect.Type;
        }

        // Flatten all markings.
        var allMarkings = _draftState.MarkingSet.GetForwardEnumerator().ToList();

        var appearance = new HumanoidCharacterAppearance(
            hairId,
            hairColor,
            facialId,
            facialColor,
            _draftState.EyeColor,
            _draftState.SkinColor,
            allMarkings,
            hairEffectType,
            hairEffect,
            facialEffectType,
            facialEffect,
            _draftState.Width,
            _draftState.Height);

        return HumanoidCharacterProfile
            .DefaultWithSpecies(_draftState.Species)
            .WithCharacterAppearance(appearance)
            .WithSex(_draftState.Sex)
            .WithGender(_draftState.Gender)
            .WithAge(_draftState.Age)
            .WithVoice(_draftState.Voice);
    }
}
