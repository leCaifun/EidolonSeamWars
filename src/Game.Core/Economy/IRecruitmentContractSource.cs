namespace Game.Core.Economy;

/// <summary>
/// Источник ротации контрактов наёмников (GDD §5.5). Реализация появится
/// вместе с контентом и ИИ на этапе 03/08; домен зависит только от интерфейса.
/// </summary>
public interface IRecruitmentContractSource
{
    /// <summary>
    /// Создаёт новую ротацию контрактов на указанный день.
    /// </summary>
    /// <param name="day">Номер дня после дневного тика.</param>
    IReadOnlyList<RecruitmentContract> CreateContracts(int day);
}
