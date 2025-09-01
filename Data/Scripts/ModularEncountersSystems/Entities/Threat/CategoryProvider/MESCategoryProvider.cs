using ModularEncountersSystems.Entities;
using ModularEncountersSystems.Spawning.Manipulation;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using VRage.Game.ModAPI;

namespace ModularEncounterSystems.Data.Scripts.ModularEncountersSystems.Entities.Threat.CategoryProvider
{
    // It's not MES's category provider, but it uses a lot of the logic from MES's category provider. We can expect nearly the same result per-block that MES would give.
    public class MESCategoryProvider : BlockCategoryProvider
    {
        public string Name
        {
            get
            {
                return "MES Category";
            }
        }

        public string GetCategory(object obj)
        {
            if (obj as IMySlimBlock != null)
            {
                return GetBlockCategory(obj as IMySlimBlock);
            }
            return string.Empty;
        }

        private string GetBlockCategory(IMySlimBlock block)
        {
            try
            {
                if (block == null)
                {
                    ThreatEvaluator.Debug("[CP] Encountered a null block trying to evaluate profile.");
                    return null;
                }

                IMyTerminalBlock terminalBlock = block.FatBlock as IMyTerminalBlock;

                if (terminalBlock == null)
                {       
                    ThreatEvaluator.Debug("[CP] Block is not a terminal block: " + block.ToString());
                    return null;
                }

                var _Id = block.BlockDefinition.Id;
                var _Type = _Id.TypeId;
                var _SubType = _Id.SubtypeId;

                if (terminalBlock is IMyRadioAntenna)
                    return "Antennas";
                if (terminalBlock is IMyBeacon)
                    return "Beacons";
                if (terminalBlock is IMyButtonPanel)
                    return "Buttons";
                if (terminalBlock is IMyCargoContainer)
                    return "Containers";
                if (_SubType.String.Contains("ContractBlock"))
                    return "Contracts";
                if (terminalBlock is IMyShipController)
                    if (((IMyShipController)terminalBlock).CanControlShip)
                        return "Controllers";
                if (terminalBlock is IMyGravityGeneratorBase || terminalBlock is IMyVirtualMass)
                    return "Gravity";
                if (terminalBlock is IMyGyro)
                    return "Gyros";
                if (terminalBlock is IMyJumpDrive)
                    return "JumpDrives";
                if (terminalBlock is IMyMechanicalConnectionBlock)
                    return "Mechanical";
                if (terminalBlock is IMyMedicalRoom || _Type == typeof(MyObjectBuilder_SurvivalKit))
                    return "Medical";
                if (terminalBlock is IMyParachute)
                    return "Parachutes";
                if (terminalBlock is IMyProductionBlock)
                    return "Production";
                if (terminalBlock is IMyProjector)
                    return "Projectors";
                if (terminalBlock is IMyPowerProducer)
                    return "Power";
                if (terminalBlock is IMyCockpit)
                    return "Seats";
                if (terminalBlock is IMyStoreBlock)
                    return "Stores";
                if (terminalBlock is IMyThrust)
                    return "Thrusters";
                if (terminalBlock is IMyShipToolBase)
                    return "Tools";
                if (terminalBlock is IMyTurretControlBlock)
                    return "TurretControllers";
                if (terminalBlock is IMyLargeTurretBase)
                    return "Turrets";
                if (terminalBlock is IMyUserControllableGun)
                    return "Guns";
                if (ArmorModuleReplacement.SmallModules.Contains(_Id) || ArmorModuleReplacement.LargeModules.Contains(_Id))
                    return "Inhibitors";
                if (BlockManager.NanobotBlockIds.Contains(_Id))
                    return "NanoBots";
                if (BlockManager.ShieldBlockIds.Contains(_Id))
                    return "Shields";

                return "Other";
            }
            catch (Exception e)
            {
                ThreatEvaluator.Debug("[CP] Encountered a problem in MESCategoryProvider: [REF1] " + e.Message + " at " + e.TargetSite);
                return null;
            }
        }
    }
}
