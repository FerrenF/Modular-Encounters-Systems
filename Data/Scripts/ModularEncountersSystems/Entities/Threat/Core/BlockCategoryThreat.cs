using System.Xml;
using System.Xml.Serialization;

namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Core {

    [XmlRoot("Category")]
    public class BlockCategoryThreat : ThreatDefinition
    {
        public override string GetId() => $"{Category}";

        [XmlText]
        public string Category { get; set; } = string.Empty;
        public override bool Equals(ThreatDefinition other) => other != null && GetId() == other.GetId();
        public override int GetHashCode() => GetId().GetHashCode();
    }
}