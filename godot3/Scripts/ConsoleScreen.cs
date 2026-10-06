using Godot;
using System;
using System.Globalization;

public class ConsoleScreen : Panel
{
    [Export] public NodePath OutputPath = new NodePath("Output");
    private TextEdit output;

    public override void _Ready()
    {
        output = OutputPath == null || OutputPath.IsEmpty() ? null : GetNodeOrNull<Node>(OutputPath) as TextEdit;
        if (Godot.Object.IsInstanceValid(output)) output.Readonly = true;
    }

    public void AppendMessage(string message)
    {
        if (!Godot.Object.IsInstanceValid(output) || string.IsNullOrEmpty(message)) return;
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        output.Text += (output.Text.Length > 0 ? "\n" : "") + "[" + timestamp + "] " + message;
        output.CursorSetLine(output.GetLineCount() - 1);
    }

    public void CopyAll()
    {
        if (Godot.Object.IsInstanceValid(output)) OS.Clipboard = output.Text;
    }
}
