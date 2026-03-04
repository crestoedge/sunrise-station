using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.ThermalVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XRayCamoComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CamoLevel { get; set; } = 1.0f; // 0.0 = fully visible, 1.0 = fully camouflaged
}
