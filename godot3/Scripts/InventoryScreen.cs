using Godot;

public class InventoryScreen : Panel
{
    [Signal] public delegate void HealRequested();
    [Signal] public delegate void Closed();

    public void RequestHeal()
    {
        EmitSignal(nameof(HealRequested));
    }

    public void Close()
    {
        Hide();
        EmitSignal(nameof(Closed));
    }
}