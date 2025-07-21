
using ModularEncountersSystems.API;
using ModularEncountersSystems.Configuration;
using ModularEncountersSystems.Helpers;
using ModularEncountersSystems.Logging;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace ModularEncountersSystems.Entities {

	public enum BlockTypeEnum {

		None,
		All,
		Antennas,
		Beacons,
		Buttons,
		Containers,
		Controllers,
		Contract,
		Gravity,
		Guns,
		Gyros,
		Inhibitors,
		JumpDrives,
		Mechanical,
		Medical,
		NanoBots,
		Power,
		Parachutes,
		Production,
		Projectors,
		RivalAi,
		Seats,
		Shields,
		Stores,
		Thrusters,
		Tools,
		Turrets,
		TurretControllers

	}

	public enum OwnerTypeEnum {

		None = 0,
		Unowned = 1,
		Player = 1 << 1,
		NPC = 1 << 2

	}

	public enum RelationTypeEnum {

		None = 0,
		Enemy = 1,
		Neutral = 1 << 1,
		Friends = 1 << 2,
		Faction = 1 << 3

	}

	public static class EntityEvaluator {

		public static double AltitudeAtPosition(Vector3D coords) {

			foreach (var planet in PlanetManager.Planets) {

				if (planet.Closed)
					continue;

				if (planet.IsPositionInRange(coords))
					return planet.AltitudeAtPosition(coords);

			}

			return -1000000;
		
		}

		public static List<GridEntity> GetAttachedGrids(IMyCubeGrid cubeGrid) {

			var gridList = new List<GridEntity>();

			if (cubeGrid == null)
				return gridList;

			GetAttachedGrids(cubeGrid, gridList);
			return gridList;


		}

		public static void GetAttachedGrids(IMyCubeGrid cubeGrid, List<GridEntity> gridList) {

			lock(gridList)
				gridList.Clear();

			var gridGroup = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Physical);

			foreach (var grid in GridManager.Grids) {

				if (grid.IsClosed() || !grid.HasPhysics)
					continue;

				if (gridGroup.Contains(grid.CubeGrid))
					gridList.Add(grid);

			}

		}

		public static void GetAttachedGrids(GridEntity parent) {

			//SpawnLogger.Write("Method Start", SpawnerDebugEnum.Dev, true);

			if (parent?.CubeGrid == null)
				return;

			//SpawnLogger.Write("Clear Lists", SpawnerDebugEnum.Dev, true);

			lock (parent.PhysicalLinkedGrids)
				parent.PhysicalLinkedGrids.Clear();

			lock (parent.LinkedGrids)
				parent.LinkedGrids.Clear();

			//SpawnLogger.Write("Get Group", SpawnerDebugEnum.Dev, true);
			MyAPIGateway.GridGroups.GetGroup(parent.CubeGrid, GridLinkTypeEnum.Physical, parent.PhysicalLinkedGrids);
			parent.RefreshLinkedGrids = false;

			//SpawnLogger.Write("First Loop", SpawnerDebugEnum.Dev, true);

			foreach (var grid in GridManager.Grids) {

				if (grid == null || grid.IsClosed() || !grid.HasPhysics)
					continue;

				if (grid.CubeGrid != null && parent.PhysicalLinkedGrids.Contains(grid.CubeGrid))
					parent.LinkedGrids.Add(grid);

			}

			if (parent.LinkedGrids.Count == 0)
				parent.LinkedGrids.Add(parent);

			//SpawnLogger.Write("Second Loop", SpawnerDebugEnum.Dev, true);

			for (int i = parent.LinkedGrids.Count - 1; i >= 0; i--) {

				var grid = parent.LinkedGrids[i];

				if (grid == null || grid == parent)
					continue;

				grid.LinkedGrids = parent.LinkedGrids;
				grid.RefreshLinkedGrids = false;

			}

		}

		public static GridEntity GetGridProfile(IMyCubeGrid cubeGrid) {

			foreach (var grid in GridManager.Grids) {

				if (grid.IsClosed() || !grid.HasPhysics)
					continue;

				if (grid.CubeGrid == cubeGrid)
					return grid;

			}

			return null;

		}

		public static OwnerTypeEnum GetOwnersFromList(List<long> owners) {

			var result = OwnerTypeEnum.Unowned;

			if (owners.Count == 0)
				return result;

			foreach (var owner in owners) {

				if (FactionHelper.IsIdentityNPC(owner)) {

					result |= OwnerTypeEnum.NPC;
					result &= ~OwnerTypeEnum.Unowned;

				} else {

					result |= OwnerTypeEnum.Player;
					result &= ~OwnerTypeEnum.Unowned;

				}

			}

			return result;

		}

		public static GridOwnershipEnum GetGridOwnerships(List<GridEntity> gridList, bool overrideCheck = false) {

			GridOwnershipEnum result = GridOwnershipEnum.None;

			for (int i = gridList.Count - 1; i >= 0; i--) {

				var grid = GridManager.GetSafeGridFromIndex(i);

				if(grid != null)
					result |= GetGridOwnerships(grid, overrideCheck);

			}

			return result;

		}

		public static GridOwnershipEnum GetGridOwnerships(GridEntity grid, bool overrideCheck = false) {

			if (!grid.ActiveEntity())
				return GridOwnershipEnum.None;

			if (!grid.RecheckOwnershipMajority && !overrideCheck)
				return grid.Ownership;

			GridOwnershipEnum result = GridOwnershipEnum.None;

			foreach (var owner in grid.CubeGrid.BigOwners) {

				if (owner == 0)
					continue;

				if (FactionHelper.IsIdentityPlayer(owner))
					result |= GridOwnershipEnum.PlayerMajority;
				else
					result |= GridOwnershipEnum.NpcMajority;
			
			}

			foreach (var owner in grid.CubeGrid.SmallOwners) {

				if (owner == 0 || grid.CubeGrid.BigOwners.Contains(owner))
					continue;

				if (FactionHelper.IsIdentityPlayer(owner))
					result |= GridOwnershipEnum.PlayerMinority;
				else
					result |= GridOwnershipEnum.NpcMinority;

			}

			grid.RecheckOwnershipMajority = false;
			grid.Ownership = result;
			return result;

		}

		public static RelationTypeEnum GetRelationsFromList(long ownerId, List<long> owners) {

			var result = RelationTypeEnum.None;

			if (owners.Count == 0) {

				result = RelationTypeEnum.Enemy;
				return result;

			}

			foreach (var owner in owners) {

				var relation = GetRelationBetweenIdentities(ownerId, owner);

				if (!result.HasFlag(relation))
					result |= relation;

			}

			return result;

		}

		public static RelationTypeEnum GetRelationBetweenIdentities(long ownerA, long ownerB) {

			var factionA = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerA);
			var factionB = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerB);

			if (factionA != null && factionA == factionB)
				return RelationTypeEnum.Faction;

			if (FactionHelper.IsIdentityNPC(ownerA)) {

				if (factionA != null)
					return GetRelationFromReputation(MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(ownerB, factionA.FactionId));

			}

			if (FactionHelper.IsIdentityNPC(ownerB)) {

				if (factionB != null)
					return GetRelationFromReputation(MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(ownerA, factionB.FactionId));

			}

			if (factionA != null && factionB != null) {

				return GetRelationFromReputation(MyAPIGateway.Session.Factions.GetReputationBetweenFactions(factionA.FactionId, factionB.FactionId));

			}

			return RelationTypeEnum.Enemy;

		}

		public static RelationTypeEnum GetRelationFromReputation(int reputation) {

			if (reputation < -500)
				return RelationTypeEnum.Enemy;

			if (reputation > 500)
				return RelationTypeEnum.Friends;

			return RelationTypeEnum.Neutral;
		
		}

		public static double GravityAtPosition(Vector3D coords) {

			foreach (var planet in PlanetManager.Planets) {

				if (planet.Closed)
					continue;

				if (planet.IsPositionInRange(coords))
					return GravityAtPosition(coords, planet);


			}

			return 0;

		}

		//150m Box of Red Ship

		public static ITarget GetTargetFromBlockEntity(IMyCubeBlock entity) {

			if (entity == null) {

				//Logger.Write(" - IMyCubeBlock Entity Null", BehaviorDebugEnum.TargetAcquisition);
				return null;

			}
				

			var grid = entity.SlimBlock.CubeGrid;

			foreach (var gridEntity in GridManager.Grids) {

				if (!gridEntity.ActiveEntity() || grid != gridEntity.CubeGrid)
					continue;

				foreach (var block in gridEntity.AllTerminalBlocks) {

					if (!block.ActiveEntity())
						continue;

					if (entity.EntityId == block.Block.EntityId)
						return block;
				
				}
			
			}

			//Logger.Write(" - IMyCubeBlock Entity Not Found In Existing Grids", BehaviorDebugEnum.TargetAcquisition);
			return null;

		}

		public static ITarget GetTargetFromEntity(IMyEntity entity) {

			//Try Player
			var character = entity as IMyCharacter;
			var block = entity as IMyCubeBlock;
			
			if (character != null) {

				return GetTargetFromPlayerEntity(character);

			} else if(block == null) {

				var toolBase = entity as IMyEngineerToolBase;
				var gunBase = entity as IMyGunBaseUser;

				if (gunBase != null) {

					return GetTargetFromPlayerEntity(gunBase.Owner as IMyCharacter);

				}

				if (toolBase != null) {

					IMyEntity charEntity = null;

					if (MyAPIGateway.Entities.TryGetEntityById(toolBase.OwnerId, out charEntity))
						return GetTargetFromPlayerEntity(charEntity as IMyCharacter);

				}

			}

			//Try Block/Grid
			var grid = entity as IMyCubeGrid;

			if (grid != null)
				return GetTargetFromGridEntity(grid);

			if (block != null)
				return GetTargetFromBlockEntity(block);

			return null;

		}

		public static ITarget GetTargetFromGridEntity(IMyCubeGrid entity) {

			if (entity == null)
				return null;

			foreach (var gridEntity in GridManager.Grids) {

				if (!gridEntity.ActiveEntity() || entity != gridEntity.CubeGrid)
					continue;

				return gridEntity;

			}

			return null;

		}

		public static ITarget GetTargetFromPlayerEntity(IMyCharacter entity) {

			if (entity == null)
				return null;

			foreach (var playerEntity in PlayerManager.Players) {

				if (!playerEntity.ActiveEntity() || entity.EntityId != playerEntity.ParentEntity.EntityId)
					continue;

				return playerEntity;

			}

			return null;

		}

		public static double GravityAtPosition(Vector3D coords, PlanetEntity planet) {

			return planet.Gravity.GetGravityMultiplier(coords);

		}

		public static double GridBroadcastRange(List<GridEntity> grids, bool onlyAntenna = false) {

			double result = 0;

			foreach (var grid in grids) {

				var power = GridBroadcastRange(grid);

				if (power > result)
					result = power;

			}

			return result;

		}

		public static double GridBroadcastRange(GridEntity grid, bool onlyAntenna = false, bool allowNpcSignals = false, string matchFaction = null, string matchName = null) {

			double result = 0;

			foreach (var antenna in grid.Antennas) {

				if (antenna.IsClosed() || !antenna.Working || !antenna.Functional)
					continue;

				var antennaBlock = antenna.Block as IMyRadioAntenna;

				if (antennaBlock == null || !antennaBlock.IsBroadcasting)
					continue;

				if (!allowNpcSignals && matchFaction == null && FactionHelper.IsIdentityNPC(antennaBlock.OwnerId))
					continue;

				if (matchFaction != null) {

					var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(antennaBlock.OwnerId);

					if (faction?.Tag != matchFaction)
						continue;
				
				}

				if (matchName != null && matchName != antennaBlock.CustomName)
					continue;

				if (antennaBlock.Radius > result)
					result = antennaBlock.Radius;

			}

			if (onlyAntenna)
				return result;

			foreach (var beacon in grid.Beacons) {

				if (beacon.IsClosed() || !beacon.Working || !beacon.Functional)
					continue;

				var beaconBlock = beacon.Block as IMyBeacon;

				if (beaconBlock == null)
					continue;

				if (!allowNpcSignals && matchFaction == null && FactionHelper.IsIdentityNPC(beaconBlock.OwnerId))
					continue;

				if (matchFaction != null) {

					var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(beaconBlock.OwnerId);

					if (faction?.Tag != matchFaction)
						continue;

				}

				if (matchName != null && matchName != beaconBlock.CustomName)
					continue;

				if (beaconBlock.Radius > result)
					result = beaconBlock.Radius;

			}

			return result;

		}

		public static bool GridPowered(List<GridEntity> grids) {

			foreach (var grid in grids) {

				if (GridPowered(grid))
					return true;

			}

			return false;

		}

		public static bool GridPowered(GridEntity grid) {

			return grid.IsPowered();

		}

		public static Vector2 GridPowerOutput(List<GridEntity> grids) {

			var result = Vector2.Zero;

			for (int i = grids.Count - 1; i >= 0; i--) {

				var grid = GridManager.GetSafeGridFromIndex(i, grids);

				if (grid != null)
					result += GridPowerOutput(grid);


			}
			
			return result;

		}

		public static Vector2 GridPowerOutput(GridEntity grid) {

			var result = Vector2.Zero;

			if (grid.IsClosed())
				return result;

			foreach (var block in grid.Power) {

				if (block.IsClosed() || !block.Working || !block.Functional)
					continue;

				var powerBlock = block.Block as IMyPowerProducer;

				if (powerBlock == null)
					continue;

				result.X += powerBlock.CurrentOutput;
				result.Y += powerBlock.MaxOutput;

			}

			return result;

		}

		public static bool GridShielded(List<GridEntity> grids) {

			foreach (var grid in grids) {

				if (GridShielded(grid))
					return true;
			
			}

			return false;

		}

		public static bool GridShielded(GridEntity grid) {

			if (grid.IsClosed())
				return false;

			if (EntityShielded(grid.CubeGrid))
				return true;

			foreach (var shield in grid.Shields) {

				if (shield.IsClosed() || !shield.Working || !shield.Functional)
					continue;

				return true;
			
			}

			return false;

		}

		public static float GridTargetValue(List<GridEntity> gridList) {

			float result = 0;

			foreach (var grid in gridList) {

				result += GridTargetValue(grid);

			}

			return result;

		}
		public static float GridTargetValue(GridEntity grid) {

			var timespan = MyAPIGateway.Session.GameDateTime - grid.LastThreatCalculationTime;
            ConfigThreat currentThreatSettings = Settings.Threat;
           
            float result = 0;

            if (timespan.TotalMilliseconds < 5000)
                return grid.ThreatScore;

            if (grid.IsClosed() || grid.AllBlocks.Count <= 1)
			{
				return result;
			}

            result += GetTargetValueFromBlockList(grid.Antennas, "Antennas");
            result += GetTargetValueFromBlockList(grid.Beacons, "Beacons");
            result += GetTargetValueFromBlockList(grid.Containers, "Containers", true);
            result += GetTargetValueFromBlockList(grid.Controllers, "Controllers");
            result += GetTargetValueFromBlockList(grid.Gravity, "Gravity", true);
            result += GetTargetValueFromBlockList(grid.Guns, "Guns", true);
            result += GetTargetValueFromBlockList(grid.JumpDrives, "JumpDrives");
            result += GetTargetValueFromBlockList(grid.Mechanical, "Mechanical");
            result += GetTargetValueFromBlockList(grid.Medical, "Medical");
            result += GetTargetValueFromBlockList(grid.NanoBots, "NanoBots");
            result += GetTargetValueFromBlockList(grid.Production, "Production", true);
            result += GetTargetValueFromBlockList(grid.Power, "Power", true);
            result += GetTargetValueFromBlockList(grid.Shields, "Shields");
            result += GetTargetValueFromBlockList(grid.Thrusters, "Thrusters");
            result += GetTargetValueFromBlockList(grid.Tools, "Tools", true);
            result += GetTargetValueFromBlockList(grid.Turrets, "Turrets", true);


			// Add threat based on the number of blocks.
            result += ((float)(grid.AllBlocks.Count 
				* (currentThreatSettings.UseThreatPerBlockMultiplier ? currentThreatSettings.ThreatPerBlockMultiplier : 0.0F)));


			// Add threat based on the size of the bounding box of the grid. (Original)
            result += ((float)(Vector3D.Distance(grid.CubeGrid.WorldAABB.Min, grid.CubeGrid.WorldAABB.Max)
				* (currentThreatSettings.UseGridBoundingBoxThreatMultiplier ? currentThreatSettings.BoundingBoxSizeMultiplier : 0.0F)));


			// Multiply threat based on the type of grid we are evaluating
            if (currentThreatSettings.UseSizeMultipliers)
            {
                if (grid.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                    result *= (float)currentThreatSettings.SizeMultipliers.LargeGridMultiplier;
                else
                    result *= (float)currentThreatSettings.SizeMultipliers.SmallGridMultiplier;

                if (grid.CubeGrid.IsStatic)
                    result *= (float)currentThreatSettings.SizeMultipliers.StationMultiplier;
            }
			
			// Add threat based on the amount of power being produced, modified by the type of grid producing power.
            if (currentThreatSettings.UsePowerMultipliers && grid.PowerOutput().Y > 0)
            {
                if (grid.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                    result += (grid.PowerOutput().Y * (float)currentThreatSettings.PowerMultipliers.LargeGridMultiplier);
                else
                    result += (grid.PowerOutput().Y * (float)currentThreatSettings.PowerMultipliers.SmallGridMultiplier);

                if (grid.CubeGrid.IsStatic)
                    result += (grid.PowerOutput().Y * (float)currentThreatSettings.PowerMultipliers.StationMultiplier);

            }

            grid.ThreatScore = result;

            grid.LastThreatCalculationTime = MyAPIGateway.Session.GameDateTime;         


            return grid.ThreatScore;


        }

        public static int GridPcuValue(List<GridEntity> gridList) {

			int result = 0;

			foreach (var grid in gridList) {

				result += GridPcuValue(grid);

			}

			return result;

		}

		public static int GridPcuValue(GridEntity grid) {

			var timespan = MyAPIGateway.Session.GameDateTime - grid.LastPcuCalculationTime;

			if (timespan.TotalMilliseconds < 5000)
				return grid.PcuScore;

			int result = 0;

			if (grid.IsClosed())
				return result;

			for (int i = grid.AllBlocks.Count - 1; i >= 0; i--) {

				var block = grid.AllBlocks[i];
				var def = block?.BlockDefinition as MyCubeBlockDefinition;

				if (def == null)
					continue;

				result += def.PCU;

			}

			grid.PcuScore = result;
			grid.LastPcuCalculationTime = MyAPIGateway.Session.GameDateTime;
			return result;

		}

        public static float GetTargetValueFromBlockList(List<BlockEntity> blockList, string categoryName, bool scanInventory = false)
        {

			float totalThreatResult = 0F;

			// Current Threat Config
            ConfigThreat currentThreatSettings = Settings.Threat;

            // Used to track specific blocks within the block's 'category' assigned by MES            
            Dictionary<string, List<float>> blockSpecificThreats = new Dictionary<string, List<float>>();

            // Tally for the threat limited to non-specific 'category' based blocks
            List<float> categoryThreats = new List<float>();

            ThreatDefinition categoryThreatDef = null;

			// Try to get a value for the category from the current threat definitions
            currentThreatSettings.CategoryThreatDefinitions.TryGetValue(categoryName, out categoryThreatDef);


            foreach (var block in blockList)
            {

				// We don't count non-functional blocks here. They DO contribute to threat insofar as overall block count.
                if (block.IsClosed() || !block.Functional)
                    continue;

				// First, try and get the block's subtype ID. If it doesn't have one, then use the blocks main type ID.
                string blockType = String.IsNullOrEmpty(block.Block.BlockDefinition.SubtypeId)
             ? block.Block.BlockDefinition.TypeIdString
             : block.Block.BlockDefinition.SubtypeId;

                ThreatDefinition threatDef = null;

                // Before we consider category threat, let's try and get a more granular definition if it exists. Also, use it to set a flag for later.
                bool isBlockSpecific = currentThreatSettings.BlockThreatDefinitions.TryGetValue(blockType, out threatDef);

                if (!isBlockSpecific)        
                    threatDef = categoryThreatDef;
                
                // we didn't find ANY threat. Don't continue calculation.
                if (threatDef == null)
                    continue;


                float value = (float)threatDef.Threat;
                if (scanInventory 
					&& block.Block.HasInventory 
					&& block.Block.GetInventory().MaxVolume > 0)
                {
					// This value will range from 0ish-1.0, representing how filled the container is in percentage.
					// 0.54 = 54% full. 

                    float invMod = ((float)block.Block.GetInventory().CurrentVolume / (float)block.Block.GetInventory().MaxVolume) + 1;
                    if (!float.IsNaN(invMod))
                    {
						// We add an amount of threat based on how full the container is times the potential volume modifier
                        value += (float)(invMod * threatDef.PotentialVolume);
                    }
                }

				// Finally. If the threat is calculated based on a specific block type, then it goes into the dictionary.
				// If not, then we are safe to add it to the category score
                
				if (isBlockSpecific)
                {

					if (!blockSpecificThreats.ContainsKey(blockType)){
						// Initialize a new list if it doesn't exist
						blockSpecificThreats[blockType] = new List<float>();
					}
                    // And add the value for tallying later.
                    blockSpecificThreats[blockType].Add(value);
                }
                else
                {
					// Add the category threat to the list of threat values.
					categoryThreats.Add(value);
                }
            }

			// Now, we tally things up and apply penalties.

            // Apply a progressive penalty for category-level blocks
            if (categoryThreatDef != null 
				&& categoryThreats.Count > 0)
            {
				// Our penalty multiplier.
                float multiplier = (float)categoryThreatDef.Multiplier;

                // The running tally. Start with first element as the base value so we apply the penalty appropriately.
                float compoundedThreat = categoryThreats[0];
                for (int i = 1; i < categoryThreats.Count; i++)
                {
                    compoundedThreat = (compoundedThreat + categoryThreats[i]) * multiplier;
                }

                totalThreatResult += compoundedThreat;
            }

            // Apply progressive penalty to each specific block type
            foreach (var kvp in blockSpecificThreats)
            {
                string blockType = kvp.Key;
                List<float> threats = kvp.Value;

				// We need to retrieve this again because we need the multiplier. Perhaps something to improve on.
				ThreatDefinition threatD;
                if (!currentThreatSettings.BlockThreatDefinitions.TryGetValue(blockType, out threatD))
                    continue;

                float multiplier = (float)threatD.Multiplier;
                
                float compoundedThreat = threats[0];
                for (int i = 1; i < threats.Count; i++)
                {
                    compoundedThreat = (compoundedThreat + threats[i]) * multiplier;
                }
                totalThreatResult += compoundedThreat;
            }

			// And we are done. Threatening, yes?
            return totalThreatResult;
        }

        public static int GridMovementScore(List<GridEntity> grids) {

			int result = 0;

			foreach (var grid in grids) {

				var score = GridVisibleMovementScore(grid);

				if (score > result)
					result = score;

			}

			return result;

		}

		public static int GridVisibleMovementScore(GridEntity grid) {

			var speed = grid.CurrentSpeed();

			if (speed < 1)
				return 0;

			return (int)(grid.BoundingBoxSize() * speed);

		}


		public static int GridWeaponCount(List<GridEntity> grids) {

			int result = 0;

			foreach (var grid in grids) {

				result += GridWeaponCount(grid);

			}

			return result;
		
		}

		public static int GridWeaponCount(GridEntity grid) {

			int result = 0;

			foreach (var gun in grid.Guns) {

				if (!gun.ActiveEntity())
					continue;

				result++;
			
			}

			foreach (var turret in grid.Turrets) {

				if (!turret.ActiveEntity())
					continue;

				result++;

			}

			return result;

		}

		public static bool IsPlayerControlled(GridEntity cubeGrid) {

			foreach (var controller in cubeGrid.Controllers) {

				if (!controller.ActiveEntity())
					continue;

				IMyShipController shipController = controller.Block as IMyShipController;

				if (shipController == null)
					continue;

				if (shipController.CanControlShip && shipController.IsUnderControl)
					return true;
			
			}

			return false;
			
		}

		public static bool IsPositionNearEntity(Vector3D coords, double distance) {

			foreach (var grid in GridManager.Grids) {

				if (!grid.ActiveEntity())
					continue;
				
				if (grid.Distance(coords) < distance) 
					return true;

			}

			foreach (var player in PlayerManager.Players) {

				if (!player.ActiveEntity())
					continue;

				if (player.Distance(coords) < distance)
					return true;

			}

			return false;

		}

		public static Vector3D EntityAcceleration(IMyEntity entity) {

			if (entity == null || entity.MarkedForClose || entity.Closed)
				return Vector3D.Zero;

			if (entity.Physics == null)
				return Vector3D.Zero;

			return entity.Physics.LinearAcceleration;

		}

		public static bool EntityShielded(IMyEntity entity) {

			if (entity == null || entity.MarkedForClose || entity.Closed)
				return false;

			//Begin Defense Shield API Check
			if (APIs.Shields.ProtectedByShield(entity))
				return true;
			//End Defense Shield API Check
			
			return false;

		}

		public static double EntityMaxSpeed(IMyEntity entity) {

			if (entity == null || entity.MarkedForClose || entity.Closed)
				return 0;

			if (entity as IMyCubeGrid != null) {

				var grid = entity as IMyCubeGrid;

				if (grid.GridSizeEnum == MyCubeSize.Large)
					return MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed;

			}

			return MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;

		}

		public static double EntitySpeed(IMyEntity entity) {

			if (entity == null || entity.MarkedForClose || entity.Closed)
				return -1;

			if (entity.Physics == null)
				return -1;

			return entity.Physics.LinearVelocity.Length();
			
		}

		public static Vector3D EntityVelocity(IMyEntity entity) {

			if (entity == null || entity.MarkedForClose || entity.Closed)
				return Vector3D.Zero;

			if (entity.Physics == null)
				return Vector3D.Zero;

			return entity.Physics.LinearVelocity;

		}



	}

}
