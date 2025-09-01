using System.Xml;
using System.Xml.Serialization;

namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Core
{
    [XmlRoot("Block")]
    public class SingleBlockThreat : ThreatDefinition
    {       
        public override string GetId() => $"{BlockType}/{BlockSubType}";
        
        [XmlAttribute("Type")]
        public string BlockType { get; set; } = string.Empty;

        [XmlAttribute("SubType")]
        public string BlockSubType { get; set; } = string.Empty;
        public override bool Equals(ThreatDefinition other) => other != null && GetId() == other.GetId();
        public override int GetHashCode() => GetId().GetHashCode();
    }
}