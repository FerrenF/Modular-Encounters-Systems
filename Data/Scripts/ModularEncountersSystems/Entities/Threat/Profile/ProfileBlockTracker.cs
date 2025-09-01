using System.Xml.Serialization;

namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Profile
{
    [XmlRoot("ProfileBlockTracker")]
    public class ProfileBlockTracker : System.IEquatable<ProfileBlockTracker>
    {
        [XmlIgnore]
        public string Id => $"{Type}/{SubType}";

        [XmlAttribute("Type")]
        public string Type { get; set; } = string.Empty;

        [XmlAttribute("SubType")]
        public string SubType { get; set; } = string.Empty;

        [XmlAttribute("Category")]
        public string Category { get; set; } = string.Empty;

        [XmlAttribute("TotalPowerOutput")]
        public float TotalPowerOutput { get; set; } = 0;

        [XmlAttribute("MaxPowerOutput")]
        public float MaxPowerOutput { get; set; } = 0;

        [XmlAttribute("Count")]
        public int Count { get; set; } = 0;

        [XmlAttribute("TotalCurrentVolume")]
        public float TotalCurrentVolume { get; set; } = 0;

        [XmlAttribute("TotalMaxVolume")]
        public float TotalMaxVolume { get; set; } = 0;

        public override bool Equals(object obj) => Equals(obj as ProfileBlockTracker);
        public bool Equals(ProfileBlockTracker other)
        {
            return other != null && Type == other.Type && SubType == other.SubType && Category == other.Category;              
        }
        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(ProfileBlockTracker left, ProfileBlockTracker right)
        {
            return System.Collections.Generic.EqualityComparer<ProfileBlockTracker>.Default.Equals(left, right);
        }

        public static bool operator !=(ProfileBlockTracker left, ProfileBlockTracker right)
        {
            return !(left == right);
        }
    }
}
