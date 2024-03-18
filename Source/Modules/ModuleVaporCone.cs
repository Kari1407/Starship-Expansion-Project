using UnityEngine;
using Random = System.Random;

namespace ModuleVaporCone
{
    public class ModuleVaporCone : PartModule
    {
        public const string MODULENAME = "ModuleVaporCone";

        [KSPField] public string transform;

        [KSPField] public int chance = 1; // Made nullable for those who don't fill it out! Setting it equal to 1 KSP didn't like

        private ModuleEnginesFX engineModule;

        private bool isShutdown = false;
        private bool hasRolledChance = false;

        public override void OnLoad(ConfigNode node)
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;
            foreach (ModuleEnginesFX moduleEngine in part.FindModulesImplementing<ModuleEnginesFX>())
            {
                if (moduleEngine.thrustVectorTransformName.Equals(transform))
                {
                    Debug.Log($"[{MODULENAME}] Found Vapor Cone EngineFX");

                    moduleEngine.Events["Shutdown"].guiActive = false;
                    moduleEngine.Events["Activate"].guiActive = false;
                    moduleEngine.Actions["OnAction"].guiName = "Toggle Vapor (Manual Override)";
                    moduleEngine.Actions["ShutdownAction"].guiName = "Shutdown Vapor (Manual Override)";
                    moduleEngine.Actions["ActivateAction"].guiName = "Activate Vapor (Manual Override)";
                    engineModule = moduleEngine;

                    if (new Random().Next(1, chance) == 1)
                    {
                        Debug.Log($"[{MODULENAME}] Activating Vapor Cone Engine [Chance]");
                        isShutdown = false; // Ensure reload is able to shutdown if falling and isShutdown above was set to true
                        moduleEngine.Activate();
                    }
                    else
                    {
                        moduleEngine.Shutdown(); // If you revert and come back and roll an L you take the L
                    }

                    hasRolledChance = true; // Has already rolled this go around! Without this else if bellow will spam until it rolls a winner
                }
            }
        }

        public void Update()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;
            if (vessel.verticalSpeed < -1 && !isShutdown && engineModule) // E.g. Re-entry
            {
                Debug.Log($"[{MODULENAME}] Shutting Down Vapor Cone Engine | Vertical Speed < -1");
                engineModule.Shutdown();
                isShutdown = true;
                hasRolledChance = false; // Get a chance to try again next go around bellow
            }
            else if (vessel.verticalSpeed > 1 && isShutdown && engineModule && !hasRolledChance) // E.g. Refly or recover from fall
            {
                if (new Random().Next(1, chance) == 1)
                {
                    Debug.Log($"[{MODULENAME}] Activating Vapor Cone Engine | Vertical Speed > 1 [Chance]");
                    engineModule.Activate();
                    isShutdown = false;
                }
                hasRolledChance = true;
            }
            else if (vessel.verticalSpeed < -1 && isShutdown && engineModule)
            {
                // Fires implicitly separate from the other < -1 only to allow another roll chance from > 1
                hasRolledChance = false;
            }
        }
    }
}