using ModularEncountersSystems.Configuration.Editor;
using ModularEncountersSystems.Core;
using ModularEncountersSystems.Logging;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Serialization;
using static ModularEncountersSystems.Configuration.ConfigThreat;

namespace ModularEncountersSystems.Configuration {

    //General

    [XmlRoot("ThreatSettings")]
    public class ConfigThreat {

          public string ModVersion { get; set; }

    [XmlArray("BlockThreats")]
    [XmlArrayItem("BlockType")]
    public List<BlockThreatEntry> BlockThreatEntries;

    [XmlArray("CategoryThreat")]
    [XmlArrayItem("Category")]
    public List<CategoryThreatEntry> CategoryThreatEntries;

    [XmlIgnore]
    public Dictionary<string, ThreatDefinition> BlockThreatDefinitions;

    [XmlIgnore]
    public Dictionary<string, ThreatDefinition> CategoryThreatDefinitions;

    [XmlElementAttribute("SizeMultipliers")]
    public GridMultiplier SizeMultipliers;

    [XmlElementAttribute("PowerMultipliers")]
    public GridMultiplier PowerMultipliers;

    public bool UsePowerMultipliers;
    public bool UseSizeMultipliers;

    [XmlIgnore]
    public bool ConfigLoaded;

    [XmlIgnore]
    public Dictionary<string, Func<string, object, bool>> EditorReference;

        public ConfigThreat(){

            ModVersion = MES_SessionCore.ModVersion;

            BlockThreatEntries = new List<BlockThreatEntry>();
            CategoryThreatEntries = new List<CategoryThreatEntry>();

            BlockThreatDefinitions = new Dictionary<string, ThreatDefinition>();
            CategoryThreatDefinitions = new Dictionary<string, ThreatDefinition>();

            SizeMultipliers = new GridMultiplier();
            PowerMultipliers = new GridMultiplier();

            UsePowerMultipliers = true;
            UseSizeMultipliers = true;

            ConfigLoaded = false;


            EditorReference = new Dictionary<string, Func<string, object, bool>> {

				{"UsePowerMultipliers", (s, o) => EditorTools.SetCommandValueBool(s, ref UsePowerMultipliers) },
				{"UseSizeMultipliers", (s, o) => EditorTools.SetCommandValueBool(s, ref UseSizeMultipliers) }				

			};

		}

        public ConfigThreat LoadSettings(string phase)
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("Config-Threat.xml", typeof(ConfigThreat)))
            {
                try
                {
                    ConfigThreat config = null;
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("Config-Threat.xml", typeof(ConfigThreat));

                    string xmlText = reader.ReadToEnd();

                    if (xmlText != null)
                    {
                        config = MyAPIGateway.Utilities.SerializeFromXML<ConfigThreat>(xmlText);


                        SpawnLogger.WriteToGameLog($"Deserialization: Config is {(config != null ? "not null" : "null")}", SpawnerDebugEnum.Threat, true);
                        SpawnLogger.WriteToGameLog($"BlockThreatEntries count: {config?.BlockThreatEntries?.Count ?? -1}", SpawnerDebugEnum.Threat, true);
                        SpawnLogger.WriteToGameLog($"CategoryThreatEntries count: {config?.CategoryThreatEntries?.Count ?? -1}", SpawnerDebugEnum.Threat, true);

                        config.BlockThreatDefinitions = config.BlockThreatEntries
                        .Where(e => !string.IsNullOrWhiteSpace(e.BlockName))
                        .ToDictionary(e => e.BlockName, e => e.ToDefinition());

                        config.CategoryThreatDefinitions = config.CategoryThreatEntries
                            .Where(e => !string.IsNullOrWhiteSpace(e.CategoryName))
                            .ToDictionary(e => e.CategoryName, e => e.ToDefinition());

                        config.ConfigLoaded = true;
                        SpawnLogger.Write("Loaded Existing Settings from Config-Threat.xml. Phase: " + phase, SpawnerDebugEnum.Startup, true);
                        return config;
                    }
                    else
                    {
                        SpawnLogger.WriteToGameLog("ERROR loading Config-Threat.xml:Opening file returned null result. ", SpawnerDebugEnum.Startup, true);
                    }
                }
                catch (Exception exc)
                {
                    SpawnLogger.WriteToGameLog("ERROR loading Config-Threat.xml: " + exc, SpawnerDebugEnum.Error, true);
                }
            }

            var defaultSettings = new ConfigThreat();
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Config-Threat.xml", typeof(ConfigThreat)))
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(defaultSettings));
            }
            catch (Exception exc)
            {
                SpawnLogger.Write("ERROR creating Config-Threat.xml: " + exc, SpawnerDebugEnum.Error, true);
            }

            return defaultSettings;
        }


        public string SaveSettings()
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Config-Threat.xml", typeof(ConfigThreat)))
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(this));
                return "Settings Updated Successfully.";
            }
            catch
            {
                return "Settings Changed, But Could Not Be Saved.";
            }
        }

        public string EditFields(string receivedCommand)
        {
            var split = receivedCommand.Split('.');
            if (split.Length < 5) return "Invalid command.";
            Func<string, object, bool> reference;
            if (!EditorReference.TryGetValue(split[3], out reference)) return $"Field {split[3]} not found.";
            if (!reference.Invoke(receivedCommand, null)) return $"Invalid value for {split[3]}.";
            return SaveSettings();
        }

        


    }

    public class ThreatDefinition
    {
        public double Threat;
        public double Multiplier;
        public double PotentialVolume;
    }
    public class BlockThreatEntry
    {
        [XmlText]
        public string BlockName;

        [XmlAttribute("Threat")]
        public double Threat = 0;

        [XmlAttribute("Multiplier")]
        public double Multiplier = 1.0;

        [XmlAttribute("PotentialVolume")]
        public double PotentialVolume = 1.0;

        public ThreatDefinition ToDefinition()
        {
            return new ThreatDefinition
            {
                Threat = Threat,
                Multiplier = Multiplier,
                PotentialVolume = PotentialVolume
            };
        }
    }


    public class CategoryThreatEntry
    {
        [XmlText]
        public string CategoryName;

        [XmlAttribute("Threat")]
        public double Threat = 0;

        [XmlAttribute("Multiplier")]
        public double Multiplier = 1.0;

        [XmlAttribute("PotentialVolume")]
        public double PotentialVolume = 1.0;

        public ThreatDefinition ToDefinition()
        {
            return new ThreatDefinition
            {
                Threat = Threat,
                Multiplier = Multiplier,
                PotentialVolume = PotentialVolume
            };
        }
    }

   
    public class GridMultiplier
    {
        public double SmallGridMultiplier = 0.5;
        public double LargeGridMultiplier = 1.0;
        public double StationMultiplier = 1.25;
    }
}