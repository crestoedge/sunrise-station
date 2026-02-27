using Content.Server.Humanoid;
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

public sealed class DynamicAppearanceSystem : EntitySystem
{
    [Dependency]
    private readonly UserInterfaceSystem _uiSystem = default!;

    [Dependency]
    private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency]
    private readonly HumanoidAppearanceSystem _humanoidSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DynamicAppearanceComponent, GetVerbsEvent<Verb>>(OnVerbsRequest);
        SubscribeLocalEvent<DynamicAppearanceComponent, DynamicAppearanceUIMarkingSetMessage>(OnMarkingsSet);
        SubscribeLocalEvent<DynamicAppearanceComponent, DynamicAppearanceUIBaseLayersSetMessage>(OnBaseLayersSet);
    }

    private void OnVerbsRequest(EntityUid uid, DynamicAppearanceComponent component, GetVerbsEvent<Verb> args)
    {
        // Only allow self-modification for slime people
        if (args.User != uid)
            return;

        // Check if the entity is a slime person
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        args.Verbs.Add(
            new Verb
            {
                Text = Loc.GetString("slime-appearance-verb-text"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Species/Slime/parts.rsi"), "head_m"),
                Act = () =>
                {
                    _uiSystem.OpenUi(uid, DynamicAppearanceUiKey.Key, actor.PlayerSession);
                    _uiSystem.SetUiState(
                        uid,
                        DynamicAppearanceUiKey.Key,
                        new DynamicAppearanceUIState(
                            humanoidAppearance.MarkingSet,
                            humanoidAppearance.Species,
                            humanoidAppearance.BodyType,
                            humanoidAppearance.Sex,
                            humanoidAppearance.SkinColor,
                            humanoidAppearance.CustomBaseLayers
                        )
                    );
                },
            }
        );
    }

    private void OnMarkingsSet(
        EntityUid uid,
        DynamicAppearanceComponent component,
        DynamicAppearanceUIMarkingSetMessage message
    )
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
            return;

        // Ensure it's a slime person
        if (humanoidAppearance.Species != "SlimePerson")
            return;

        // Filter markings to only allow those appropriate for slime people
        var filteredMarkingSet = FilterSlimeMarkings(message.MarkingSet, humanoidAppearance.Species);

        humanoidAppearance.MarkingSet = filteredMarkingSet;
        Dirty(uid, humanoidAppearance);

        if (message.ResendState)
        {
            _uiSystem.SetUiState(
                uid,
                DynamicAppearanceUiKey.Key,
                new DynamicAppearanceUIState(
                    humanoidAppearance.MarkingSet,
                    humanoidAppearance.Species,
                    humanoidAppearance.BodyType,
                    humanoidAppearance.Sex,
                    humanoidAppearance.SkinColor,
                    humanoidAppearance.CustomBaseLayers
                )
            );
        }
    }

    private void OnBaseLayersSet(
        EntityUid uid,
        DynamicAppearanceComponent component,
        DynamicAppearanceUIBaseLayersSetMessage message
    )
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
            return;

        // Ensure it's a slime person
        if (humanoidAppearance.Species != "SlimePerson")
            return;

        if (message.Info == null)
        {
            humanoidAppearance.CustomBaseLayers.Remove(message.Layer);
        }
        else
        {
            humanoidAppearance.CustomBaseLayers[message.Layer] = message.Info.Value;
        }

        Dirty(uid, humanoidAppearance);

        if (message.ResendState)
        {
            _uiSystem.SetUiState(
                uid,
                DynamicAppearanceUiKey.Key,
                new DynamicAppearanceUIState(
                    humanoidAppearance.MarkingSet,
                    humanoidAppearance.Species,
                    humanoidAppearance.BodyType,
                    humanoidAppearance.Sex,
                    humanoidAppearance.SkinColor,
                    humanoidAppearance.CustomBaseLayers
                )
            );
        }
    }

    /// <summary>
    /// Filter markings to only allow those appropriate for the slime species
    /// </summary>
    private MarkingSet FilterSlimeMarkings(MarkingSet originalSet, string species)
    {
        var filteredSet = new MarkingSet();

        foreach (var (category, markings) in originalSet.Markings)
        {
            foreach (var marking in markings)
            {
                if (_prototypeManager.TryIndex<MarkingPrototype>(marking.MarkingId, out var prototype))
                {
                    // Allow marking if it has no species restriction or specifically allows slime people
                    if (
                        prototype.SpeciesRestrictions == null
                        || prototype.SpeciesRestrictions.Count == 0
                        || prototype.SpeciesRestrictions.Contains(species)
                    )
                    {
                        filteredSet.AddBack(category, marking);
                    }
                }
            }
        }

        return filteredSet;
    }
}
