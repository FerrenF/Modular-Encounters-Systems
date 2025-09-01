using ModularEncountersSystems.Configuration.Editor;
using ModularEncountersSystems.Core;
using ModularEncountersSystems.Entities;
using ModularEncountersSystems.Logging;
using ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ModularEncountersSystems.Configuration {

    public enum ThreatProfileIdentifierType
    {
        Planet,
        Sector,
        GridName
    }

    [XmlRoot("ThreatSettings")]
    public class ConfigThreat : ModularEncountersSystems.Entities.Threat.ThreatSettings {


        [XmlElement("DebugThreat")]
    public bool DebugThreat = false;

        [XmlIgnore]
    public bool ConfigLoaded = false;

        [XmlIgnore]
    public Dictionary<string, Func<string, object, bool>> EditorReference;

        public ConfigThreat()
        {
            ThreatEvaluator.Logger = TSELogger.Default;
            ThreatModVersion = MES_SessionCore.ModVersion;
            ConfigLoaded = false;

            EditorReference = new Dictionary<string, Func<string, object, bool>> 
            {	
                {   
                    "DebugThreat", (s, o) => EditorTools.SetCommandValueBool(s, ref DebugThreat)
                }
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
                        if (config != null)
                        {
                            config.ConfigLoaded = true;
                            SpawnLogger.Write("Loaded Existing Settings from Config-Threat.xml. Phase: " + phase, SpawnerDebugEnum.Startup, true);
                            return config;
                        }
                    }
                    else
                    {
                        SpawnLogger.Write("ERROR loading Config-Threat.xml:Opening file returned null result. ", SpawnerDebugEnum.Startup, true);
                    }
                }
                catch (Exception exc)
                {
                    SpawnLogger.Write("ERROR loading Config-Threat.xml: " + exc, SpawnerDebugEnum.Error, true);
                }
            }

            var defaultSettings = new ConfigThreat();
            try
            {
                SpawnLogger.Write("Writing default settings for Config-Threat.xml", SpawnerDebugEnum.Threat, true);
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


        public new ConfigThreat copy()
        {
            return new ConfigThreat()
            {
                ThreatModVersion = this.ThreatModVersion,
                SingleBlockThreatEntries = this.SingleBlockThreatEntries,
                BlockCategoryThreatEntries = this.BlockCategoryThreatEntries,
                GridTypeMultipliers = this.GridTypeMultipliers,
                GridPowerOutputMultipliers = this.GridPowerOutputMultipliers,
                ConfigLoaded = this.ConfigLoaded,
                BoundingBoxSizeMultipliers = this.BoundingBoxSizeMultipliers,
                ThreatPerBlockMultipliers = this.ThreatPerBlockMultipliers,
                ThreatProfiles = this.ThreatProfiles,
                DebugThreat = this.DebugThreat,
            };

        }

    }
}