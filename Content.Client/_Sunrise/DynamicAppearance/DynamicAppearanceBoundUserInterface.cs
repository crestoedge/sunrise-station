using Content.Shared._Sunrise.DynamicAppearance;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.DynamicAppearance;

public sealed class DynamicAppearanceBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DynamicAppearanceWindow? _window;

    private DynamicAppearanceBUIState? _lastState;

    public DynamicAppearanceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DynamicAppearanceWindow>();

        _window.OnSave += () =>
        {
            if (_window == null)
                return;

            SendMessage(new DynamicAppearanceSaveMessage(_window.DraftState));
        };

        _window.OnReset += () =>
        {
            if (_lastState != null)
                _window?.UpdateState(_lastState);
        };

        // If the server already pushed a state before Open(), apply it now.
        if (_lastState != null)
            _window.UpdateState(_lastState);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DynamicAppearanceBUIState data)
            return;

        _lastState = data;
        _window?.UpdateState(data);
    }
}
