using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.DynamicAppearance;

/// <summary>
/// Allows entities with <see cref="DynamicAppearanceComponent"/> to edit their
/// appearance in-round through a BUI (markings, skin color, eye color, etc.).
/// </summary>
public sealed class DynamicAppearanceSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    private const int AgeMin = 17;
    private const int AgeMax = 120;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DynamicAppearanceComponent, GetVerbsEvent<AlternativeVerb>>(OnVerbsRequest);

        Subs.BuiEvents<DynamicAppearanceComponent>(DynamicAppearanceUiKey.Key, subs =>
        {
            subs.Event<DynamicAppearanceSaveMessage>(OnSaveMessage);
        });
    }

    #region Verb

    private void OnVerbsRequest(EntityUid uid, DynamicAppearanceComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.User != uid)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("dynamic-appearance-verb"),
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Species/Slime/parts.rsi"), "head_m"),
            Act = () =>
            {
                _ui.OpenUi(uid, DynamicAppearanceUiKey.Key, actor.PlayerSession);
                SendState(uid, humanoid);
            },
            Priority = -2,
        });
    }

    #endregion

    #region BUI message handlers

    private void OnSaveMessage(Entity<DynamicAppearanceComponent> ent, ref DynamicAppearanceSaveMessage args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        // Markings filter to species-allowed only, then apply sex restrictions
        var filtered = FilterMarkings(args.State.MarkingSet, humanoid.Species);
        humanoid.MarkingSet = filtered;

        // Sex (also runs EnsureSexes on the marking set)
        _humanoid.SetSex(ent, args.State.Sex, humanoid: humanoid);

        // Skin color
        _humanoid.SetSkinColor(ent, args.State.SkinColor, humanoid: humanoid);

        // Eye color
        humanoid.EyeColor = args.State.EyeColor;

        // Gender (pronouns)
        humanoid.Gender = args.State.Gender;

        // TTS voice
        if (!string.IsNullOrEmpty(args.State.Voice))
            _humanoid.SetTTSVoice(ent, args.State.Voice, humanoid);

        if (_prototypeManager.TryIndex<SpeciesPrototype>(humanoid.Species, out var speciesProto))
        {
            // Size clamped to species bounds
            humanoid.Width = Math.Clamp(args.State.Width, speciesProto.MinWidth, speciesProto.MaxWidth);
            humanoid.Height = Math.Clamp(args.State.Height, speciesProto.MinHeight, speciesProto.MaxHeight);

            // Age clamped to species age bounds
            humanoid.Age = Math.Clamp(args.State.Age, speciesProto.MinAge, speciesProto.MaxAge);
        }

        // Custom base layers
        humanoid.CustomBaseLayers.Clear();
        foreach (var (layer, info) in args.State.CustomBaseLayers)
            humanoid.CustomBaseLayers[layer] = info;

        Dirty(ent, humanoid);
        SendState(ent, humanoid);
    }

    #endregion

    #region Helpers

    private void SendState(EntityUid uid, HumanoidAppearanceComponent humanoid)
    {
        _ui.SetUiState(uid, DynamicAppearanceUiKey.Key,
            new DynamicAppearanceBUIState(
                new DynamicAppearanceState(
                    humanoid.MarkingSet,
                    humanoid.Species,
                    humanoid.Sex,
                    humanoid.Age,
                    humanoid.Gender,
                    humanoid.Voice,
                    humanoid.SkinColor,
                    humanoid.EyeColor,
                    humanoid.CustomBaseLayers,
                    humanoid.Width,
                    humanoid.Height
                ),
                GetNetEntity(uid)
            ));
    }

    /// <summary>
    /// Filters a marking set to only keep markings allowed for the given species.
    /// </summary>
    private MarkingSet FilterMarkings(MarkingSet originalSet, string species)
    {
        var filtered = new MarkingSet();

        foreach (var (category, markings) in originalSet.Markings)
        {
            foreach (var marking in markings)
            {
                if (!_prototypeManager.TryIndex<MarkingPrototype>(marking.MarkingId, out var proto))
                    continue;

                if (proto.SpeciesRestrictions == null
                    || proto.SpeciesRestrictions.Count == 0
                    || proto.SpeciesRestrictions.Contains(species))
                {
                    filtered.AddBack(category, marking);
                }
            }
        }

        return filtered;
    }

    #endregion
}
