using System.Linq;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;

namespace Content.Client._Sunrise.DynamicAppearance;

/// <summary>
/// Pushing server state into UI controls and refreshing individual sections.
/// </summary>
public sealed partial class DynamicAppearanceWindow
{
    /// <summary>
    /// Full state push: sets the draft, resolves the preview entity, and refreshes every control.
    /// Called by the BUI when the server sends a new <see cref="DynamicAppearanceBUIState"/>.
    /// </summary>
    public void UpdateState(DynamicAppearanceBUIState buiState)
    {
        _draftState = buiState.State;
        _speciesProto = _protoMan.TryIndex<SpeciesPrototype>(_draftState.Species, out var sp) ? sp : null;

        // Resolve the entity for preview
        _previewEntity = _entManager.GetEntity(buiState.Entity);

        RefreshAge();
        RefreshSex();
        RefreshPronouns();
        RefreshVoice();
        RefreshSizeSliders();
        RefreshSkinColor();
        RefreshEyeColor();
        RefreshHairPickers();
        RefreshBodyMarkings();
        RefreshPreview();
    }

    // ═══════════ Individual refresh methods ═══════════

    private void RefreshAge()
    {
        AgeEdit.Text = _draftState.Age.ToString();
    }

    private void RefreshSex()
    {
        RebuildSexButton();
    }

    private void RebuildSexButton()
    {
        _sexValues.Clear();
        SexButton.Clear();

        if (_speciesProto == null)
            return;

        foreach (var sex in _speciesProto.Sexes)
        {
            SexButton.AddItem(
                Loc.GetString($"humanoid-profile-editor-sex-{sex.ToString().ToLowerInvariant()}-text"),
                _sexValues.Count);
            _sexValues.Add(sex);
        }

        var idx = _sexValues.IndexOf(_draftState.Sex);
        if (idx >= 0)
        {
            SexButton.SelectId(idx);
        }
        else if (_sexValues.Count > 0)
        {
            SexButton.SelectId(0);
            _draftState.Sex = _sexValues[0];
        }
    }

    private void RefreshPronouns()
    {
        var genderIdx = _genderValues.IndexOf(_draftState.Gender);
        if (genderIdx >= 0)
            PronounsButton.SelectId(genderIdx);
    }

    private void RefreshVoice()
    {
        if (_ttsEnabled)
            RebuildVoiceList();
    }

    private void RebuildVoiceList()
    {
        _filteredVoices = _protoMan.EnumeratePrototypes<TTSVoicePrototype>()
            .Where(v => v.RoundStart && HumanoidCharacterProfile.CanHaveVoice(v, _draftState.Sex))
            .OrderBy(v => v.Name)
            .ToList();

        VoiceButton.Clear();
        var selectIdx = 0;

        for (var i = 0; i < _filteredVoices.Count; i++)
        {
            var voice = _filteredVoices[i];
            VoiceButton.AddItem(voice.Name, i);

            if (voice.ID == _draftState.Voice)
                selectIdx = i;
        }

        if (_filteredVoices.Count > 0)
        {
            VoiceButton.SelectId(selectIdx);
            _draftState.Voice = _filteredVoices[selectIdx].ID;
        }
    }

    private void RefreshSizeSliders()
    {
        if (_speciesProto != null)
        {
            HeightSlider.MinValue = _speciesProto.MinHeight;
            HeightSlider.MaxValue = _speciesProto.MaxHeight;
            WidthSlider.MinValue = _speciesProto.MinWidth;
            WidthSlider.MaxValue = _speciesProto.MaxWidth;
        }

        HeightSlider.Value = _draftState.Height;
        WidthSlider.Value = _draftState.Width;
        UpdateSizeLabels();
    }

    private void UpdateSizeLabels()
    {
        HeightLabel.Text = Loc.GetString("dynamic-appearance-height-label",
            ("value", (int) Math.Round(HeightSlider.Value * 100f)));
        WidthLabel.Text = Loc.GetString("dynamic-appearance-width-label",
            ("value", (int) Math.Round(WidthSlider.Value * 100f)));
    }

    private void RefreshSkinColor()
    {
        _skinColorSelector.Color = _draftState.SkinColor;
    }

    private void RefreshEyeColor()
    {
        EyeColorPicker.SetData(_draftState.EyeColor);
    }

    private void RefreshHairPickers()
    {
        var hairList = _draftState.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var h)
            ? h.ToList()
            : new List<Marking>();

        var facialList = _draftState.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var fh)
            ? fh.ToList()
            : new List<Marking>();

        HairPicker.UpdateData(hairList, _draftState.Species, 1);
        FacialHairPicker.UpdateData(facialList, _draftState.Species, 1);
    }

    private void RefreshBodyMarkings()
    {
        Markings.SetData(
            _draftState.MarkingSet,
            _draftState.Species,
            _draftState.Sex,
            _draftState.SkinColor,
            _draftState.EyeColor);
    }
}
