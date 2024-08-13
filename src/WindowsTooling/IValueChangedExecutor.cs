namespace WindowsTooling;

public interface IValueChangedExecutor
{
    event EventHandler<CommandValueChanged> ValueChanged;
}
