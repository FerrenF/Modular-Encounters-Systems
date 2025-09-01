
using ModularEncountersSystems.Entities;
using ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.CategoryProvider;
using ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Profile;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI;

namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.Util
{
    public struct blockId : IEquatable<blockId>
    {
        public string Type;
        public string SubType;

        public override string ToString()
        {
            return $"{Type}/{SubType}";
        }
        public override bool Equals(object obj)
        {
            return obj is blockId && Equals((blockId)obj);
        }

        public bool Equals(blockId other)
        {
            return Type == other.Type &&
                   SubType == other.SubType;
        }

        public override int GetHashCode()
        {
            int hashCode = -1555956430;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SubType);
            return hashCode;
        }

        public static bool operator ==(blockId left, blockId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(blockId left, blockId right)
        {
            return !(left == right);
        }
    }
    public static class ThreatUtil
    {
        public static string ExtractBlockType(string xsiType)
        {
            if (string.IsNullOrWhiteSpace(xsiType))
                return string.Empty;

            int underscoreIndex = xsiType.IndexOf('_');
            if (underscoreIndex >= 0 && underscoreIndex < xsiType.Length - 1)
            {
                return xsiType.Substring(underscoreIndex + 1);
            }

            return xsiType;
        }


        public static void MakeBlockCounts(Dictionary<blockId, ProfileBlockTracker> output, GridEntity targetEntity, BlockCategoryProvider categoryProvider)
        {         
            
            if (targetEntity == null)
            {
                ThreatEvaluator.Debug($"[UTIL] Attempted to make block counts for a null entity! Cancelling... ");
                return;
            }

            if (categoryProvider == null)
            {
                ThreatEvaluator.Debug($"[UTIL] Attempted to make block counts when the BlockCategoryProvider given was null. Cancelling... ");
                return;
            }

            HashSet<MyDefinitionId> banned = new HashSet<MyDefinitionId>();         
            List<IMyCubeGrid> cubeGrids = new List<IMyCubeGrid> ();
            IMyGridGroupData grp = targetEntity.CubeGrid.GetGridGroup(GridLinkTypeEnum.Physical);
            grp.GetGrids(cubeGrids);

            ThreatEvaluator.Debug($"[UTIL] Evaluating grid group with ({cubeGrids.Count}) grids for '{targetEntity.GridName}'. ");
            foreach (IMyCubeGrid grid in cubeGrids)
            {
                List<IMySlimBlock> evalList = new List<IMySlimBlock>();
                grid.GetBlocks(evalList);
                foreach (var block in evalList)
                {
                    if ((block?.BlockDefinition?.Id == null))
                    {
                        ThreatEvaluator.Debug($"[UTIL] Encountered a block with a null definition or definition ID provided by grid '{targetEntity.GridName}'. ");
                        continue;
                    }

                    if (banned.Contains(block.BlockDefinition.Id))
                        continue;

                    var _Id = block.BlockDefinition.Id;
                    var _Type = ExtractBlockType(_Id.TypeId.IsNull ? "" : _Id.TypeId.ToString());
                    var _SubType = _Id.SubtypeName ?? "";

                    if (_Type == "" && _SubType == "")
                    {
                        ThreatEvaluator.Debug($"[UTIL] Encountered a block whose type and subtype were empty: '{targetEntity.GridName}'. ");
                        banned.Add(_Id);
                        continue;
                    }

                    var key = new blockId() { Type = _Type, SubType = _SubType };
                    try
                    {
                        var category = categoryProvider.GetCategory(block);

                        if (category == null)
                        {
                            ThreatEvaluator.Debug($"[UTIL] Couldn't find the right category for: '{key.ToString()}'. Skipping it's evaluation. ");
                            banned.Add(_Id);
                            continue;
                        }
                      
                        if (!output.ContainsKey(key) || output[key] == null)
                        {
                            ThreatEvaluator.Debug($"[UTIL] Assigned category {category} to: '{key.ToString()}'. ");
                            output.Add(key, new ProfileBlockTracker() { Category = category, Type = _Type, SubType = _SubType, Count = 1});
                        }
                        else
                        {
                            output[key].Count += 1;
                        }

                        try
                        {
                            if (block as IMyPowerProducer != null)
                            {
                                output[key].TotalPowerOutput += (block as IMyPowerProducer).CurrentOutput;
                                output[key].TotalPowerOutput += (block as IMyPowerProducer).MaxOutput;
                            }
                            if (block as IMyCargoContainer != null || block as IMyInventory != null)
                            {
                                var iv = block as IMyInventory;
                                output[key].TotalCurrentVolume += ((float)iv.CurrentVolume);
                                output[key].TotalMaxVolume += ((float)iv.MaxVolume);
                            }
                        }
                        catch (NullReferenceException e)
                        {
                            ThreatEvaluator.Debug($"[UTIL] Error trying to access block {_Type}/{_SubType} power/volume details! REF3" + e.Message);
                            banned.Add(_Id);
                            continue;
                        }

                    }
                    catch (NullReferenceException e)
                    {
                        ThreatEvaluator.Debug($"[UTIL] Error trying to process category or details of block! REF2" + e.Message);
                        banned.Add(_Id);
                        continue;
                    }

                }
            }
           
        }


        public static EntityThreatProfile GridToThreatProfile(GridEntity g)
        {
            
            Dictionary<blockId, ProfileBlockTracker> BlockCounts = new Dictionary<blockId, ProfileBlockTracker>();
            ThreatEvaluator.Debug($"Grid to threat profile requested for {g.GridName} owned by faction [{g.FactionOwner()}]! ");
          
            try
            {
                MakeBlockCounts(BlockCounts, g, new MESCategoryProvider());
            }
            catch (Exception ex)
            {
                ThreatEvaluator.Debug($"Problem creating a threat profile for {g.GridName}! REF1" +ex.Message);
            }
         
            return new EntityThreatProfile()
            {
                DisplayName = g.GridName ?? "No Name",
                GridType = g.IsStatic() ? "Static" : g.CubeGrid.GridSizeEnum.ToString(),
                GridScale = (float)g.BoundingBoxSize(),
                Blocks = BlockCounts.Values.ToHashSet()               
            };
    }

    }
}
