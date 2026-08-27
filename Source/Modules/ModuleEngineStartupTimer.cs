using System.Linq;
using UnityEngine;
using Waterfall;

namespace StarshipExpansionProject
{
    public class ModuleEngineStartupTimer : PartModule
    {
        [KSPField(guiActive = false, guiName = "EngineStartup")]
        public float engineStartup = 0f;

        [KSPField(guiActive = false, guiName = "Started")]
        public bool started = false;

        [KSPField]
         public string targetEngineID = "hotstage";

        ModuleEngines[] engines;
        ModuleWaterfallFX[] waterFX;

        float timer = 0f;
        const float duration = 10f; // seconds

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            engines = part.FindModulesImplementing<ModuleEngines>().ToArray();
            waterFX = part.FindModulesImplementing<ModuleWaterfallFX>().ToArray();
            
            HideEnginePAW(targetEngineID);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (!started && EngineIsRunning())
            {
                started = true;
                timer = 0f;
            }

            if (started && engineStartup < 10f)
            {
                timer += Time.fixedDeltaTime;
                engineStartup = Mathf.Clamp(timer / duration * 10f, 0f, 10f);
            }

            PushToWaterfall();
        }

        bool EngineIsRunning()
        {
            foreach (var e in engines)
                if (e.EngineIgnited)
                    return true;

            return false;
        }

        void PushToWaterfall()
        {
            foreach (var fx in waterFX)
            {
                var c = fx.Controllers
                    .FirstOrDefault(x => x.name == "engineStartup");

                if (c != null)
                    c.Set(engineStartup);
            }
        }

        private void HideEnginePAW(string idToHide)
        {
            foreach (var engine in engines)
            {
                if (engine.engineID == idToHide)
                {
                    foreach (BaseField field in engine.Fields)
                    {
                        field.guiActive = false;
                        field.guiActiveEditor = false;
                    }
                    foreach (BaseEvent evt in engine.Events)
                    {
                        evt.active = false;
                        evt.guiActive = false;
                        evt.guiActiveEditor = false;
                    }
                    break;
                }
            }
        }
    }
}