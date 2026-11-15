namespace Game.Core.Content;

/// <summary>Операция merge для журнала переопределений.</summary>
public enum ContentChangeKind
{
    /// <summary>Запись добавлена впервые.</summary>
    Add = 0,

    /// <summary>Запись заменила одноимённую запись предыдущего пака.</summary>
    Replace = 1
}
