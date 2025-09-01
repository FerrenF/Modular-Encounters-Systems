using ModularEncountersSystems.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Profile
{

    [XmlRoot("EntityThreatProfile")]
    public class EntityThreatProfile
    {
        [XmlAttribute("DisplayName")]
        public string DisplayName { get; set; }

        [XmlAttribute("GridType")]
        public string GridType { get; set; }

        [XmlAttribute("GridScale")]
        public float GridScale { get; set; }

        [XmlArray("Blocks")]
        [XmlArrayItem("ProfileBlockTracker")]
        public HashSet<ProfileBlockTracker> Blocks { get; set; } = new HashSet<ProfileBlockTracker>();

        [XmlIgnore]
        public int NumBlocks => Blocks.Sum((b) => b.Count);

        [XmlIgnore]
        public float Threat => new ThreatEvaluator(this).evaluate();
    }
}
