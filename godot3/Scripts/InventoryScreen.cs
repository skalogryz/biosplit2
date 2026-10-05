using Godot;

public class InventoryScreen : Panel
{
    [Signal] public delegate void HealRequested();

    public void RequestHeal()
    {
        EmitSignal(nameof(HealRequested));
    }

    public void Close()
    {
        Hide();
    }
}