using ModularEncountersSystems.Configuration;
using ModularEncountersSystems.Logging;
using System;


namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Util
{
    public class TSELogger : TLogInterface
    {
        public static TSELogger Default = new TSELogger();
        private static bool debug => Settings.Threat.DebugThreat;
        
        public void Debug(string message)
        {
            if (!debug) return;
            SpawnLogger.Write(message, SpawnerDebugEnum.Threat, debug);
        }
        public void Error(string message)
        {
            SpawnLogger.Write(message, SpawnerDebugEnum.Error, true);
        }

        public void Info(string message)
        {
            SpawnLogger.Write(message, SpawnerDebugEnum.Threat);
        }

        public void Warn(string message)
        {
            SpawnLogger.Write(message, SpawnerDebugEnum.Threat);
        }
    }
}
