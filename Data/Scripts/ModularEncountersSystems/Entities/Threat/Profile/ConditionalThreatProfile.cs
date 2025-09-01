using ModularEncountersSystems.Entities.Threat;
using System.Xml.Serialization;

namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Profile
{

    [XmlRoot("ConditionalThreatProfile")]
    public class ConditionalThreatProfile : ThreatSettingsBase
    {
        public enum ThreatProfileIdentifierType
        {
            Planet,
            Sector,
            Grid,
            FactionName
        }
        public enum ThreatProfileConditionType
        {
            Near,
            Is,
            Contains
        }

        [XmlAttribute("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlAttribute("Identifier")]
        public ThreatProfileIdentifierType Identifier;

        [XmlAttribute("Condition")]
        public ThreatProfileConditionType Condition;

        [XmlAttribute("Value")]
        public string Value;

        [XmlAttribute("Importance")]
        public int Importance;

        public new ConditionalThreatProfile copy()        
        {
            return new ConditionalThreatProfile
            {
                ThreatModVersion = this.ThreatModVersion,
                SingleBlockThreatEntries = this.SingleBlockThreatEntries,
                BlockCategoryThreatEntries = this.BlockCategoryThreatEntries,
                GridTypeMultipliers = this.GridTypeMultipliers,
                GridPowerOutputMultipliers = this.GridPowerOutputMultipliers,
                BoundingBoxSizeMultipliers = this.BoundingBoxSizeMultipliers,
                ThreatPerBlockMultipliers = this.ThreatPerBlockMultipliers,
                Name = this.Name,
                Identifier = this.Identifier,
                Condition = this.Condition,
                Value = this.Value    ,
                Importance = this.Importance,
            };
        }
    }
}