
using ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Core;
using ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Profile;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ModularEncountersSystems.Entities.Threat
{

    [XmlRoot("ThreatSettings")]
    public class ThreatSettings : ThreatSettingsBase
    {
        [XmlArray("ThreatProfiles")]
        [XmlArrayItem("ConditionalThreatProfile")]
        public List<ConditionalThreatProfile> ThreatProfiles { get; set; }
            = new List<ConditionalThreatProfile>();

        public new ThreatSettings copy()
        {
            return new ThreatSettings
            {
                ThreatModVersion = this.ThreatModVersion,
                SingleBlockThreatEntries = this.SingleBlockThreatEntries,
                BlockCategoryThreatEntries = this.BlockCategoryThreatEntries,
                GridTypeMultipliers = this.GridTypeMultipliers,
                GridPowerOutputMultipliers = this.GridPowerOutputMultipliers,
                BoundingBoxSizeMultipliers = this.BoundingBoxSizeMultipliers,
                ThreatPerBlockMultipliers = this.ThreatPerBlockMultipliers,
                ThreatProfiles = this.ThreatProfiles
            };
        }
    }
}