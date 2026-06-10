namespace DiplomaGame.Editor
{
    /// <summary>
    /// Контракт для всех вкладок Project Forge.
    /// Каждая вкладка — отдельный класс, реализующий этот интерфейс.
    /// </summary>
    public interface IForgeTab
    {
        string Title { get; }
        void OnGUI();
    }
}
