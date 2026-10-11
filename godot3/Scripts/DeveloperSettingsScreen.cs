using Godot;

public class DeveloperSettingsScreen : Panel
{
    [Export] public string ConfigPath = "user://developer_settings.cfg";
    private CheckBox scrollOnHit;
    private bool scrollEnabled = true;
    public bool ScrollOnHitEnabled => scrollEnabled;

    public override void _Ready()
    {
        scrollOnHit = GetNode<CheckBox>("ScrollOnHit");
        var config = new ConfigFile();
        var error = config.Load(ConfigPath);
        if (error == Error.Ok)
        {
            object value = config.GetValue("developer", "scroll_on_hit", true);
            if (value is bool enabled) scrollEnabled = enabled;
        }
        else if (error != Error.FileNotFound) GD.PushWarning("Cannot load developer settings: " + error);
        scrollOnHit.Pressed = scrollEnabled;
        scrollOnHit.Connect("toggled", this, nameof(OnScrollOnHitToggled));
        GetNode<Button>("CloseButton").Connect("pressed", this, nameof(Close));
        Hide();
    }
    public void OnScrollOnHitToggled(bool enabled)
    {
        scrollEnabled = enabled;
        var config = new ConfigFile();
        config.Load(ConfigPath);
        config.SetValue("developer", "scroll_on_hit", enabled);
        var error = config.Save(ConfigPath);
        if (error != Error.Ok) GD.PushWarning("Cannot save developer settings: " + error);
    }
    public void Toggle() => Visible = !Visible;
    public void Close() => Hide();
}
