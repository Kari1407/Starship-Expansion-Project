using System.Linq;
using UnityEngine;
using Waterfall;

public class ModuleEngineStartupTimer : PartModule
{
    /* ===============================
     *  Public Debug
     * =============================== */
    [KSPField(guiActive = true, guiName = "EngineStartup")]
    public float engineStartup = 0f;

    [KSPField(guiActive = true, guiName = "Started")]
    public bool started = false;

    /* ===============================
     *  Internal
     * =============================== */
    ModuleEngines[] engines;
    ModuleEnginesFX[] enginesFX;
    ModuleWaterfallFX[] waterFX;

    float timer = 0f;
    const float duration = 10f; // seconds

    /* ===============================
     *  Lifecycle
     * =============================== */
    public override void OnStart(StartState state)
    {
        base.OnStart(state);

        engines = part.FindModulesImplementing<ModuleEngines>().ToArray();
        enginesFX = part.FindModulesImplementing<ModuleEnginesFX>().ToArray();
        waterFX = part.FindModulesImplementing<ModuleWaterfallFX>().ToArray();
    }

    public void FixedUpdate()
    {
        if (!HighLogic.LoadedSceneIsFlight)
            return;

        /* ===============================
         *  Detect first ignition
         * =============================== */
        if (!started && EngineIsRunning())
        {
            started = true;
            timer = 0f;
        }

        /* ===============================
         *  Progress timer
         * =============================== */
        if (started && engineStartup < 10f)
        {
            timer += Time.fixedDeltaTime;
            engineStartup = Mathf.Clamp(timer / duration * 10f, 0f, 10f);
        }

        PushToWaterfall();
    }

    /* ===============================
     *  Helpers
     * =============================== */
    bool EngineIsRunning()
    {
        foreach (var e in engines)
            if (e.EngineIgnited)
                return true;

        foreach (var e in enginesFX)
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
}
