namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.CategoryProvider
{
    public interface BlockCategoryProvider
    {
        string Name { get; }
        string GetCategory(object obj);
    }
}
