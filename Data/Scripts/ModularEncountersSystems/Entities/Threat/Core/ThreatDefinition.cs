using System.Xml.Serialization;

namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Core
{
    public abstract class ThreatDefinition : System.IEquatable<ThreatDefinition>
    {
        public abstract string GetId();
        [XmlAttribute("Threat")]
        public float Threat { get; set; } = 0.0f;

        [XmlAttribute("Multiplier")]
        public float Multiplier { get; set; } = 1.0f;

        [XmlAttribute("MultiplierThreshold")]
        public int MultiplierThreshold { get; set; } = 2;

        [XmlAttribute("FullVolumeThreat")]
        public float FullVolumeThreat { get; set; } = 0.0f;
        public abstract override int GetHashCode();
        public abstract bool Equals(ThreatDefinition other);
    }
}