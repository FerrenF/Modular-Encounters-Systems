using ModularEncountersSystems.Configuration;
using ModularEncountersSystems.Entities.Threat;
using ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Core;
using ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Profile;
using ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Util;
using System;
using System.Collections.Generic;
using System.Linq;


namespace ModularEncountersSystems.Entities { 

    public class ThreatEvaluator  {

        public static void Log(string m) => Logger.Info(m);
        public static void Debug(string m) => Logger.Debug(m);
        public static void Errror(string m) => Logger.Error(m);

        public static TLogInterface Logger;
        private float DEFAULT_THREAT = 1.0f;
        private bool config_initialized = false;
        
        private bool test_mode = false;
        private EntityThreatProfile GridThreatProfile;
        private ThreatSettings CurrentThreatSettings;

        public string ProfileName 
        {
            get
            {
                return GridThreatProfile.DisplayName ?? "No Name";
            }
        }

        public ThreatEvaluator(EntityThreatProfile profile) 
        { 
            GridThreatProfile = profile;         
            CurrentThreatSettings = Settings.Threat;
            if (CurrentThreatSettings != null)
                config_initialized = true;
            else
                Errror("[CORE] Attempt to initialize threat settings resulted in a null value...");
        }

        public float evaluate()
        {
            if (!config_initialized || CurrentThreatSettings == null)
            {
                Errror($"[CORE] Threat settings not initialized. Could not evaluate entity '{ProfileName}'.");
                return DEFAULT_THREAT;
            }                
            
            float result = 0;
            if (GridThreatProfile == null)
            {
                Errror($"[CORE] Threat profile passed in to evaluate: '{ProfileName}', was empty.");
                return DEFAULT_THREAT;
            }
            
          
            HashSet<string> evaluatedBlockIDs = new HashSet<string>();          
            result += EvaluateSingleBlocks(evaluatedBlockIDs);
            result += EvaluateBlockCategories(evaluatedBlockIDs);

            var typeMultipliers = CurrentThreatSettings.GridTypeMultipliers;
            var total_Number_of_blocks = (float)(GridThreatProfile.Blocks.Sum((item) => item.Count));

            var type = GridThreatProfile.GridType;
            if (type.Equals("static", StringComparison.OrdinalIgnoreCase))
            {
                float threatWithPerBlock = result + (total_Number_of_blocks * CurrentThreatSettings.ThreatPerBlockMultipliers.StationMultiplier);
                float threatFromPerBlock = threatWithPerBlock - result;

                float threatWithBoundingBox = result + ((float)GridThreatProfile.GridScale * CurrentThreatSettings.BoundingBoxSizeMultipliers.StationMultiplier);
                float threatFromBoundingBox = threatWithBoundingBox - result;

                float runningTotal1 = result + threatFromPerBlock + threatFromBoundingBox;

                float tMul = (float)CurrentThreatSettings.GridTypeMultipliers.StationMultiplier;
                float threatWithTypeMultiplier = runningTotal1 * tMul;
                float threatFromTypeMultiplier = threatWithTypeMultiplier - runningTotal1;

                result = threatWithTypeMultiplier;
                Debug($"[BLK] Grid type is {type}: Added {threatFromPerBlock} threat from the block count; Added {threatFromBoundingBox} from bounding box; Type Multiplier ({tMul}); From these: {result}");
            }
            else if (type.Equals("large", StringComparison.OrdinalIgnoreCase))
            {
                float threatWithPerBlock = result + (total_Number_of_blocks * CurrentThreatSettings.ThreatPerBlockMultipliers.LargeGridMultiplier);
                float threatFromPerBlock = threatWithPerBlock - result;

                float threatWithBoundingBox = result + ((float)GridThreatProfile.GridScale * CurrentThreatSettings.BoundingBoxSizeMultipliers.LargeGridMultiplier);
                float threatFromBoundingBox = threatWithBoundingBox - result;

                float runningTotal1 = result + threatFromPerBlock + threatFromBoundingBox;

                float tMul = (float)CurrentThreatSettings.GridTypeMultipliers.LargeGridMultiplier;                
                float threatWithTypeMultiplier = runningTotal1 * tMul;
                float threatFromTypeMultiplier = threatWithTypeMultiplier - runningTotal1;

                result = threatWithTypeMultiplier;
                Debug($"[BLK] Grid type is {type}: Added {threatFromPerBlock} threat from the block count; Added {threatFromBoundingBox} from bounding box; Type multiplier {tMul} = {result}");
            }
            else
            {
                float threatWithPerBlock = result + (total_Number_of_blocks * CurrentThreatSettings.ThreatPerBlockMultipliers.SmallGridMultiplier);
                float threatFromPerBlock = threatWithPerBlock - result;

                float threatWithBoundingBox = result + ((float)GridThreatProfile.GridScale * CurrentThreatSettings.BoundingBoxSizeMultipliers.SmallGridMultiplier);
                float threatFromBoundingBox = threatWithBoundingBox - result;

                float runningTotal1 = result + threatFromPerBlock + threatFromBoundingBox;

                float tMul = (float)CurrentThreatSettings.GridTypeMultipliers.SmallGridMultiplier;
                float threatWithTypeMultiplier = runningTotal1 * tMul;
                float threatFromTypeMultiplier = threatWithTypeMultiplier - runningTotal1;

                result = threatWithTypeMultiplier;
                Debug($"[BLK] Grid type is {type}: {threatFromPerBlock} threat from the block count; {threatFromBoundingBox} from bounding box; Type multiplier {tMul} = {result}");
            }


            var powerNow = GridThreatProfile.Blocks.Sum((item) => item.TotalPowerOutput);
            if (powerNow > 0)
            {
                GridTypeThreatMultiplier multipliers = CurrentThreatSettings.GridPowerOutputMultipliers;
                float modifier = (float)(type == "Static" ? (powerNow * multipliers.StationMultiplier) : (type == "Large")? (powerNow * multipliers.LargeGridMultiplier) : (powerNow * multipliers.SmallGridMultiplier));
                float res = powerNow * modifier;
                Debug($"[BLK] Profile '{ProfileName}' has a total power output of {powerNow}. A modifier of {modifier} has been applied resulting in an additional {res} threat. ");
            }
            Debug($"[BLK] Profile '{ProfileName}' has a total evaluated threat of {result}.");
            return result;
        }

        private float EvaluateBlockCategories(HashSet<string> evaluatedBlockIDs, bool filterEvaluatedIDs = true)
        {
            if (!config_initialized || GridThreatProfile == null || CurrentThreatSettings == null)
            {
                Errror($"[CORE] Threat settings not initialized. Could not evaluate entity '{ProfileName}'.");
                return DEFAULT_THREAT;
            }

            Debug($"Evaluating block category threat for entity '{ProfileName}'.");
            var blockGroupSet = GridThreatProfile.Blocks;
            if (blockGroupSet == null)
            {
                Debug($"[CAT] Threat profile '{ProfileName}' did not contain any blocks.");
                return DEFAULT_THREAT;
            }
            Debug($"[CAT] Evaluating {blockGroupSet.Count} blocks.");
            float threatTotal = 0;
            var blockCategoryThreat = CurrentThreatSettings.BlockCategoryThreatEntries;

            Dictionary<BlockCategoryThreat, List<float>> catSpecificThreats = new Dictionary<BlockCategoryThreat, List<float>>();
            foreach (var catThreat in blockCategoryThreat)
            {
                string categoryName = catThreat.Category;

                if (categoryName == null)
                    Logger.Warn($"[CAT] Encountered a null category name assigned to block {catThreat.GetId()}. Interesting...");

                var matchingBlocks = blockGroupSet.Where(b => !string.IsNullOrWhiteSpace(b.Category) && b.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase)).ToList();                
                Debug($"[CAT] Matched {matchingBlocks.Count} blocks to category {categoryName}.");

                foreach (var block in matchingBlocks)
                    if (evaluatedBlockIDs.Contains(block.Id))
                        Debug($"[CAT] {block.Id} has already been evaluated. It will not be evaluated for category {block.Category}.");
               
                matchingBlocks.RemoveAll((b) => evaluatedBlockIDs.Contains(b.Id));

                if (matchingBlocks.Count == 0)
                {
                    Debug($"[CAT] After filtering, there were no blocks left in category {catThreat.Category} for entity {ProfileName}. Continuing.");
                    continue;
                }
                    

                Debug($"[CAT] Evaluating {matchingBlocks.Count} types of block for profile '{ProfileName}'.");
                foreach (var tracker in matchingBlocks)
                {
                    Debug($"[CAT] Evaluating {tracker.Id}");
                    float addedScore = 0f;
                    try
                    {
                        // Inventory-based threat
                        if (tracker.TotalMaxVolume > 0 && catThreat.FullVolumeThreat != 0)
                        {
                            if (test_mode)
                                tracker.TotalCurrentVolume = (float)(new Random().NextDouble() * (tracker.TotalMaxVolume * .75));
                            
                            addedScore += makeVolumeScore("CAT", catThreat.FullVolumeThreat, tracker);
                        }

                        if (!catSpecificThreats.ContainsKey(catThreat))
                            catSpecificThreats[catThreat] = new List<float>();

                        float dbg_tracker = 0.0f;
                        for (int i = 0; i < tracker.Count; i++)
                        {
                            var cur = catSpecificThreats[catThreat];
                            var newThr = addedScore + catThreat.Threat;
                            dbg_tracker += newThr;
                            cur.Add(newThr);
                        }
                        Debug($"[CAT] Added {dbg_tracker} base threat to {ProfileName} from {tracker.Count} blocks with ID {ProfileName}.");
                    }
                    catch (Exception ex)
                    {
                        Log($"{categoryName}: {ex.Message}");
                    }
                }
            }
                Debug($"[CAT] Scoring category based totals for {ProfileName}...");

            foreach (var t in catSpecificThreats)
            {
                BlockCategoryThreat threatDef = t.Key;
                List<float> threatDetected = t.Value;

                if (threatDetected.Count == 0)
                    continue;

                    else if (threatDetected.Count == 1)
                    {
                        float res = threatDetected.FirstOrDefault();
                        threatTotal += res;
                        Debug($"[CAT] {t.Key.Category} added {res} threat to profile '{ProfileName}'.");
                    }
                    else if (threatDetected.Count <= threatDef.MultiplierThreshold)
                    {
                        float res = threatDetected.Sum();
                        threatTotal += res;
                        Debug($"[CAT] {t.Key.Category} added {res} threat to profile '{ProfileName}'.");
                    }
                    else
                    {
                        int totalLength = threatDetected.Count;
                        int numberToPenalize = totalLength - threatDef.MultiplierThreshold;

                        List<float> t1 = t.Value.Take(totalLength - numberToPenalize).ToList();
                        List<float> t2 = t.Value.Skip(totalLength - numberToPenalize).ToList();   
                        float runningScore = t1.Sum();
                        float runningCum = t2[0];
                        while (t2.Count > 0)
                        {
                            runningCum = (runningCum + t2[0]) * threatDef.Multiplier;
                            t2.RemoveAt(0);
                        }
                        float res = (runningScore + runningCum);
                        threatTotal += res;
                        Debug($"[CAT] Category {t.Key.Category} added {res} threat to profile '{ProfileName}'. {runningCum} of it was added from multiplier penalties.");
                    }
                }
                return threatTotal;

        }

        private float makeVolumeScore(string src, float fullModifier, ProfileBlockTracker tracker)
        {
            float addedScore = 0f;
            float invMod = (tracker.TotalCurrentVolume / (tracker.TotalMaxVolume + 1));
            if (!float.IsNaN(invMod))
            {
                float scoreFromVolume = invMod * fullModifier;
                Debug($"[{src}] Added {scoreFromVolume} threat to {ProfileName} from {(int)Math.Round(invMod * 100)} filled volume.");
                addedScore += scoreFromVolume;
            }
            return addedScore;
        }

        private float EvaluateSingleBlocks(HashSet<string> evaluatedBlockIDs, bool filterEvaluatedIDs = true)
        {
            if (!config_initialized || GridThreatProfile == null)
            {
                Log($"Threat settings were not initialized and an evaluation could not complete for entity '{ProfileName}'.");
                return DEFAULT_THREAT;
            }
            Debug($"[BLK] Evaluating block specific threat scores for profile '{ProfileName}'.");

            float threatTotal = 0f;
            var singleBlockThreats = CurrentThreatSettings.SingleBlockThreatEntries;

            Debug($"[BLK] There are '{GridThreatProfile.Blocks.Count} block profiles in the threat profile for {ProfileName}'.");

            var blockSpecificThreats = new Dictionary<SingleBlockThreat, List<float>>();
            foreach (var blockTracker in GridThreatProfile.Blocks)
            {
                Debug($"[BLK] Evaluating block ID '{blockTracker.Id} for {ProfileName}'.");
                if (blockTracker.Count <= 0)
                    continue;

                var matchingSingleBlockThreat = singleBlockThreats.FirstOrDefault((item) => item.GetId() == blockTracker.Id);
                if (matchingSingleBlockThreat == null)
                {
                    Debug($"[BLK] There are no matches for ID '{blockTracker.Id} in current threat settings.");
                    continue;
                }
                  

                if (!blockSpecificThreats.ContainsKey(matchingSingleBlockThreat))
                    blockSpecificThreats[matchingSingleBlockThreat] = new List<float>();

                for (int i = 0; i < blockTracker.Count; i++)     
                    blockSpecificThreats[matchingSingleBlockThreat].Add(matchingSingleBlockThreat.Threat);

               
                if (blockTracker.TotalMaxVolume > 0 && matchingSingleBlockThreat.FullVolumeThreat != 0)
                {
                    threatTotal += makeVolumeScore("BLK", matchingSingleBlockThreat.FullVolumeThreat, blockTracker);   
                }

                evaluatedBlockIDs.Add(blockTracker.Id);
            }

            // Aggregate threat totals
            foreach (var kvp in blockSpecificThreats)
            {
                var threatDef = kvp.Key;
                var threatDetected = kvp.Value;

                if (threatDetected.Count == 0)
                    continue;

                if (threatDetected.Count == 1)
                {
                    float res = threatDetected[0];
                    threatTotal += res;
                    Debug($"[BLK] Added {res} threat from block ID {threatDef.GetId()} to threat profile for {ProfileName}.");
                }
                else if (threatDetected.Count <= threatDef.MultiplierThreshold)
                {
                    float res = threatDetected.Sum();
                    threatTotal += res;
                    Debug($"[BLK] Added {res} threat from block ID {threatDef.GetId()} to threat profile for {ProfileName}.");
                }
                else
                {
                    int totalLength = threatDetected.Count;
                    int numberToPenalize = totalLength - threatDef.MultiplierThreshold;

                    List<float> t1 = kvp.Value.Take(totalLength - numberToPenalize).ToList();
                    List<float> t2 = kvp.Value.Skip(totalLength - numberToPenalize).ToList();
                    float runningScore = t1.Sum();
                    float runningCum = t2[0];
                    while (t2.Count > 0)
                    {
                        runningCum = (runningCum + t2[0]) * threatDef.Multiplier;
                        t2.RemoveAt(0);
                    }
                    float res = runningScore + runningCum;
                    Debug($"[BLK] Added {res} threat from block ID {threatDef.GetId()} to threat profile for {ProfileName}. {runningCum} of it was from penalty multipliers.");
                    threatTotal += res;
                }
            }

            return threatTotal;
        }



   
    }
}
