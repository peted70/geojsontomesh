namespace Interfaces
{
    public delegate void ActionDelegate();

    public interface IProgress
    {
        void UpdateProgress(string info, float progress);
        void Clear();
    }

    public interface IUpdateHandler
    {
        void HookUpdate(ActionDelegate action);
        void UnhookUpdate(ActionDelegate action);
    }

    public interface IDialog
    {
        void DisplayDialog(string title, string message);
    }
}
