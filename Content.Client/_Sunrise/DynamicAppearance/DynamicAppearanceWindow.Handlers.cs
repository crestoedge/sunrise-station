using System.Linq;
using Content.Client._Sunrise.TTS;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;

namespace Content.Client._Sunrise.DynamicAppearance;

/// <summary>
/// Event handler wiring: connects UI controls to draft-state mutations.
/// </summary>
public sealed partial class DynamicAppearanceWindow
{
    // ═══════════ Save / Reset ═══════════

    private void InitSaveResetHandlers()
    {
        SaveButton.OnPressed += _ => OnSave?.Invoke();
        ResetButton.OnPressed += _ => OnReset?.Invoke();
    }

    // ═══════════ Age ═══════════

    private void InitAgeHandlers()
    {
        AgeEdit.OnTextChanged += args =>
        {
            _draftState.Age = int.TryParse(args.Text, out var age) ? age : _draftState.Age;
        };
    }

    // ═══════════ Sex ═══════════

    private void InitSexHandlers()
    {
        SexButton.OnItemSelected += args =>
        {
            _draftState.Sex = _sexValues[args.Id];
            SexButton.SelectId(args.Id);

            if (_ttsEnabled)
                RebuildVoiceList();

            RefreshBodyMarkings();
            RefreshDummyPreview();
        };
    }

    // ═══════════ Pronouns ═══════════

    private void InitPronounsHandlers()
    {
        foreach (Gender gender in Enum.GetValues<Gender>())
        {
            PronounsButton.AddItem(
                Loc.GetString($"humanoid-profile-editor-pronouns-{gender.ToString().ToLowerInvariant()}-text"),
                _genderValues.Count);
            _genderValues.Add(gender);
        }

        PronounsButton.OnItemSelected += args =>
        {
            _draftState.Gender = _genderValues[args.Id];
            PronounsButton.SelectId(args.Id);
        };
    }

    // ═══════════ TTS Voice ═══════════

    private void InitVoiceHandlers()
    {
        VoiceButton.OnItemSelected += args =>
        {
            if (args.Id < _filteredVoices.Count)
            {
                _draftState.Voice = _filteredVoices[args.Id].ID;
                VoiceButton.SelectId(args.Id);
            }
        };

        VoicePlayButton.OnPressed += _ =>
        {
            if (!string.IsNullOrEmpty(_draftState.Voice))
                _entManager.System<TTSSystem>().RequestPreviewTts(_draftState.Voice);
        };
    }

    // ═══════════ Size sliders ═══════════

    private void InitSizeHandlers()
    {
        // Width
        WidthSlider.OnValueChanged += _ => UpdateSizeLabels();

        WidthSlider.OnReleased += _ =>
        {
            _draftState.Width = WidthSlider.Value;
            RefreshDummyPreview();
        };

        WidthResetButton.OnPressed += _ =>
        {
            if (_speciesProto == null)
                return;

            WidthSlider.Value = _speciesProto.DefaultWidth;
            _draftState.Width = _speciesProto.DefaultWidth;
            UpdateSizeLabels();
            RefreshDummyPreview();
        };

        // Height
        HeightSlider.OnValueChanged += _ => UpdateSizeLabels();

        HeightSlider.OnReleased += _ =>
        {
            _draftState.Height = HeightSlider.Value;
            RefreshDummyPreview();
        };

        HeightResetButton.OnPressed += _ =>
        {
            if (_speciesProto == null)
                return;

            HeightSlider.Value = _speciesProto.DefaultHeight;
            _draftState.Height = _speciesProto.DefaultHeight;
            UpdateSizeLabels();
            RefreshDummyPreview();
        };
    }

    // ═══════════ Colors ═══════════

    private void InitColorHandlers()
    {
        // Skin color
        _skinColorSelector = new ColorSelectorSliders
        {
            SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv,
        };
        SkinColorContainer.AddChild(_skinColorSelector);

        _skinColorSelector.OnColorChanged += color =>
        {
            _draftState.SkinColor = color;
            RefreshDummyPreview();
        };

        // Eye color
        EyeColorPicker.OnEyeColorPicked += color =>
        {
            _draftState.EyeColor = color;
            RefreshDummyPreview();
        };
    }

    // ═══════════ Markings (hair + body) ═══════════

    private void InitMarkingHandlers()
    {
        // Hair
        HairPicker.OnMarkingSelect += args =>
        {
            ApplyMarkingSelect(MarkingCategories.Hair, args.slot, args.id);
            RefreshDummyPreview();
        };

        HairPicker.OnColorChanged += args =>
        {
            ApplyMarkingColor(MarkingCategories.Hair, args.slot, args.marking);
            RefreshDummyPreview();
        };

        HairPicker.OnSlotRemove += slot =>
        {
            _draftState.MarkingSet.Remove(MarkingCategories.Hair, slot);
            RefreshDummyPreview();
        };

        HairPicker.OnSlotAdd += () => { };

        // Facial hair
        FacialHairPicker.OnMarkingSelect += args =>
        {
            ApplyMarkingSelect(MarkingCategories.FacialHair, args.slot, args.id);
            RefreshDummyPreview();
        };

        FacialHairPicker.OnColorChanged += args =>
        {
            ApplyMarkingColor(MarkingCategories.FacialHair, args.slot, args.marking);
            RefreshDummyPreview();
        };

        FacialHairPicker.OnSlotRemove += slot =>
        {
            _draftState.MarkingSet.Remove(MarkingCategories.FacialHair, slot);
            RefreshDummyPreview();
        };

        FacialHairPicker.OnSlotAdd += () => { };

        // Body markings — MarkingPicker passes back its full set on every change
        Markings.OnMarkingAdded += set =>
        {
            _draftState.MarkingSet = set;
            RefreshDummyPreview();
        };

        Markings.OnMarkingRemoved += set =>
        {
            _draftState.MarkingSet = set;
            RefreshDummyPreview();
        };

        Markings.OnMarkingColorChange += set =>
        {
            _draftState.MarkingSet = set;
            RefreshDummyPreview();
        };

        Markings.OnMarkingRankChange += set =>
        {
            _draftState.MarkingSet = set;
            RefreshDummyPreview();
        };
    }

    // ═══════════ Marking helpers ═══════════

    private void ApplyMarkingSelect(MarkingCategories category, int slot, string newId)
    {
        if (!_protoMan.TryIndex<MarkingPrototype>(newId, out var proto))
            return;

        if (_draftState.MarkingSet.TryGetCategory(category, out var list) && slot < list.Count)
            _draftState.MarkingSet.Replace(category, slot, new Marking(newId, proto.Sprites.Count));
        else
            _draftState.MarkingSet.AddBack(category, new Marking(newId, proto.Sprites.Count));
    }

    private void ApplyMarkingColor(MarkingCategories category, int slot, Marking updated)
    {
        if (_draftState.MarkingSet.TryGetCategory(category, out var list) && slot < list.Count)
            _draftState.MarkingSet.Replace(category, slot, updated);
    }
}
