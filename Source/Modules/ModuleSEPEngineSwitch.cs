using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Waterfall;

namespace StarshipExpansionProject.Modules
{
	// Full Credit to Vali Zockt for writing this module, Initially for Tundra Exploration
	// Later adjusted for StarshipExpansionProject by Sofie Brink.
	public class ModuleSEPEngineSwitch : PartModule, IEngineStatus
	{
		public const string MODULENAME = "ModuleSEPEngineSwitch";
        public const string EngineIDNodeName = "EngineID";

        [KSPField(guiName = "Mode", isPersistant = true, guiActive = true, guiActiveEditor = true)]
		public string currentEngineDisplay;

		[KSPField(isPersistant = true)]
		public int selectedIndex = 0;

		[KSPField]
		public string WaterfallControllerName = "EngineMode";

		// UI Elements
		[KSPField(guiFormat = "F5", guiActive = true, guiName = "#autoLOC_6001375", guiUnits = "#autoLOC_7001409")]
		public float fuelFlowGui;

		[KSPField(guiFormat = "F2", guiActive = false, guiName = "#autoLOC_6001376", guiUnits = "%")]
		public float propellantReqMet = 0;

		[KSPField(guiFormat = "F1", guiActive = true, guiName = "#autoLOC_6001377", guiUnits = "#autoLOC_7001408")]
		public float finalThrust;

		[KSPField(guiFormat = "F1", guiActive = true, guiName = "#autoLOC_6001378", guiUnits = "#autoLOC_7001400")]
		public float realIsp;

		[KSPField(guiActive = true, guiName = "#autoLOC_6001352")]
		public string status = "Nominal";

		[KSPField(guiActive = false, guiName = "#autoLOC_6001379")]
		public string statusL2 = " ";

		[UI_Toggle(disabledText = "#autoLOC_6013010", scene = UI_Scene.All, enabledText = "#autoLOC_6005051", affectSymCounterparts = UI_Scene.All)]
		[KSPField(advancedTweakable = true, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_900770")]
		public bool independentThrottle;

		[UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f, affectSymCounterparts = UI_Scene.All)]
		[KSPAxisField(incrementalSpeed = 20f, isPersistant = true, maxValue = 100f, minValue = 0f, guiFormat = "F1", axisMode = KSPAxisMode.Incremental, guiActiveEditor = true, guiActive = true, guiName = "#autoLOC_900770")]
		public float independentThrottlePercentage;

		[UI_FloatRange(requireFullControl = true, stepIncrement = 0.5f, maxValue = 100f, minValue = 0f)]
		[KSPAxisField(minValue = 0f, incrementalSpeed = 20f, isPersistant = true, axisMode = KSPAxisMode.Incremental, maxValue = 100f, guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_6001363")]
		public float thrustPercentage = 100f;

		[KSPEvent(guiActive = true, guiName = "#LOC_SEP_NextEngineMode")]
		public void NextEngineModeEvent() => SetNextEngine();

		[KSPEvent(guiActive = true, guiName = "#LOC_SEP_PreviousEngineMode")]
		public void PreviousEngineModeEvent() => SetPreviousEngine();

		[KSPAction(guiName = "#autoLOC_6001382")]
		public void ActivateEngineAction(KSPActionParam param) => ActivateActiveEngine();

		[KSPEvent(guiName = "#autoLOC_6001382", guiActive = false, guiActiveEditor = false)]
		public void ActivateEngineEvent() => ActivateActiveEngine();

		[KSPAction(guiName = "#autoLOC_6001381")]
		public void ShutdownEngineAction(KSPActionParam param) => ShutdownActiveEngine();

		[KSPEvent(guiName = "#autoLOC_6001381", guiActive = false, guiActiveEditor = false)]
		public void ShutdownEngineEvent() => ShutdownActiveEngine();

		[KSPAction(guiName = "#autoLOC_6001380")]
		public void ToggleEngineAction(KSPActionParam param) => ToggleActiveEngine();

		[KSPAction(guiName = "#LOC_SEP_NextEngineMode")]
		public void NextEngineModeAction(KSPActionParam param) => SetNextEngine();

		[KSPAction(guiName = "#LOC_SEP_PreviousEngineMode")]
		public void PreviousEngineModeAction(KSPActionParam param) => SetPreviousEngine();

		[KSPAction(guiName = "#autoLOC_6005052")]
		public void ToggleIndependentThrottleAction(KSPActionParam param) => ToggleIndependentThrottle(param);

		private List<ModuleEngines> activeEngines;
		private List<ModuleEngines> oldEngines;
		public List<ModuleEngines> engines = new List<ModuleEngines>();
		private UI_FloatRange independentThrottlePercentField;
		[SerializeField]
		private List<string> engineIDs = new List<string>();

		private WaterfallController waterfallController
		{
			get { 
				if (_waterfallController == null && HighLogic.LoadedSceneIsFlight && !(waterfallModule?.Controllers.Count == 0))
				{
					foreach (var controller in waterfallModule?.Controllers)
					{
						if (controller.name == WaterfallControllerName)
						{
							_waterfallController = controller;
							break;
						}
					}
					if (_waterfallController == null)
						Debug.LogError($"[{MODULENAME}] Could not find waterfall controller with name '{WaterfallControllerName}' on part '{part.name}'");
				}
				return _waterfallController;
			}
		}
		private WaterfallController _waterfallController;
		private ModuleWaterfallFX waterfallModule;

		public override void OnStart(StartState state)
		{

			base.OnStart(state);

			for (int i = 0; i < engineIDs.Count; i++)
			{
				ModuleEnginesFX moduleEnginesFX = part.Modules.GetModules<ModuleEnginesFX>()
															  .Where(eng => eng.engineID == engineIDs[i])
															  .FirstOrDefault();
				if (moduleEnginesFX == null)
				{
					Debug.LogError($"[{MODULENAME}] Could not find engine with ID '{engineIDs[i]}' on part '{part.name}'");
					continue;
				}

				for (int i2 = 0; i2 < moduleEnginesFX.Actions.Count; i2++)
				{
					moduleEnginesFX.Actions[i2].active = false;
					//moduleEnginesFX.Actions[12].independentThrottle = false;
				}

				moduleEnginesFX.manuallyOverridden = true;
				moduleEnginesFX.isEnabled = false;

				engines.Add(moduleEnginesFX);
				foreach(var en in engines)
				{
					foreach (var item in en.Fields)
					{
						if (item.guiActive) item.guiActive = false;
						if (item.guiActiveEditor) item.guiActiveEditor = false;
					}
					foreach (var item in en.Events)
					{
						if (item.guiActive) item.guiActive = false;
						if (item.guiActiveEditor) item.guiActiveEditor = false;
					}
					foreach (var item in en.Actions)
					{
						if (item.activeEditor) item.activeEditor = false;
					}
				}
			}
			waterfallModule = part.Modules.GetModule<ModuleWaterfallFX>();

			SetActiveEngine(selectedIndex, true);
			Events["ActivateEngineEvent"].guiActive = activeEngines.Any(en => !en.EngineIgnited);
			Events["ShutdownEngineEvent"].guiActive = activeEngines.Any(en => en.EngineIgnited);
			if (activeEngines.Any(en => en.throttleLocked)) Fields["independentThrottlePercentage"].guiActive = false;
			Fields.TryGetFieldUIControl("independentThrottlePercentage", out independentThrottlePercentField);
			if (!independentThrottle && independentThrottlePercentField != null)
			{
				independentThrottlePercentField.SetSceneVisibility(UI_Scene.None, false);
			}
			Fields["independentThrottle"].OnValueModified += throttleModeChanged;
			Fields["independentThrottlePercentage"].OnValueModified += throttleValueChanged;
			Fields["thrustPercentage"].OnValueModified += thrustValueChanged;
		}

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
			if (node.HasValue(EngineIDNodeName))
			{
				engineIDs = node.GetValuesList(EngineIDNodeName);
			}
        }

        public override void OnActive()
		{
			StartCoroutine(DisableFields());
			Events["ActivateEngineEvent"].guiActive = false;
			Events["ShutdownEngineEvent"].guiActive = true;
			Fields["propellantReqMet"].guiActive = true;
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			fuelFlowGui = activeEngines.Sum(en => en.fuelFlowGui);
			propellantReqMet = activeEngines.Average(en => en.propellantReqMet);
			finalThrust = activeEngines.Sum(en => en.finalThrust);
			realIsp = activeEngines.Average(en => en.realIsp);
			status = activeEngines[0].status;
			Fields["statusL2"].guiActive = activeEngines[0].flameout;
			activeEngines.ForEach(en => en.Fields["statusL2"].guiActive = false);
			activeEngines.ForEach(en => en.independentThrottlePercentField.SetSceneVisibility(UI_Scene.None, en.independentThrottle));
			if (waterfallController != null && (waterfallController.Get().Length == 0 || waterfallController.Get()[0] != (float)selectedIndex / 2))
				waterfallController.Set((float) selectedIndex / 2);
		}

		public void OnDestroy()
		{
			StopCoroutine(DisableFields());
			Fields["independentThrottle"].OnValueModified -= throttleModeChanged;
			Fields["independentThrottlePercentage"].OnValueModified -= throttleValueChanged;
			Fields["thrustPercentage"].OnValueModified -= thrustValueChanged;
		}

		public void throttleModeChanged(object obj)
		{
			activeEngines.ForEach(en => en.independentThrottle = independentThrottle);
			if (independentThrottle)
			{
				independentThrottlePercentField.SetSceneVisibility(UI_Scene.All, independentThrottle);
			}
			else
			{
				independentThrottlePercentField.SetSceneVisibility(UI_Scene.None, independentThrottle);
			}
		}

		public void throttleValueChanged(object obj)
		{
			activeEngines.ForEach(en => en.independentThrottlePercentage = independentThrottlePercentage);
		}

		public void thrustValueChanged(object obj)
		{
			activeEngines.ForEach(en => en.thrustPercentage = thrustPercentage);
		}

		public void ShutdownActiveEngine()
		{
			activeEngines.ForEach(en => en.Shutdown());
			Events["ActivateEngineEvent"].guiActive = true;
			Events["ShutdownEngineEvent"].guiActive = false;
			Fields["propellantReqMet"].guiActive = false;
		}

		public void ActivateActiveEngine()
		{
			activeEngines.ForEach(en => en.Activate());
			activeEngines.ForEach(en => en.Fields["propellantReqMet"].guiActive = false);
			Events["ActivateEngineEvent"].guiActive = false;
			Events["ShutdownEngineEvent"].guiActive = true;
			Fields["propellantReqMet"].guiActive = true;
		}

		public void ToggleActiveEngine()
		{
			if (activeEngines.Any(en => en.EngineIgnited))
				ShutdownActiveEngine();
			else
				ActivateActiveEngine();
		}

		public void ToggleIndependentThrottle(KSPActionParam param)
		{
			independentThrottle = !independentThrottle;
			throttleModeChanged(param);
		}

		public void SetNextEngine()
		{
			int newIndex = selectedIndex + 1;

			if (newIndex >= engines.Count)
				newIndex = 0;

			SetActiveEngine(newIndex);
		}

		public void SetPreviousEngine()
		{
			int newIndex = selectedIndex - 1;

			if (newIndex < 0)
				newIndex = engines.Count - 1;

			SetActiveEngine(newIndex);
		}

		public void SetActiveEngine(int index, bool setup = false)
		{
			oldEngines = activeEngines;
			
			activeEngines = new List<ModuleEngines>();
			for (int i = index; i < engines.Count; i++) activeEngines.Add(engines[i]);

			activeEngines.ForEach(en => en.manuallyOverridden = false);
			//activeEngines.ForEach(en => en.isEnabled = true);
			activeEngines.ForEach(en => en.independentThrottle = independentThrottle);
			activeEngines.ForEach(en => en.independentThrottlePercentage = independentThrottlePercentage);
			activeEngines.ForEach(en => en.thrustPercentage = thrustPercentage);

			currentEngineDisplay = Regex.Replace(activeEngines[0].engineName, "([a-z])([A-Z])", "$1 $2");
			selectedIndex = index;
			waterfallController?.Set((float) index / 2);

			if (setup)
				return;

			if (activeEngines.Any(en => en.EngineIgnited))
			{
				activeEngines.ForEach(en => {if (!en.EngineIgnited) { en.Activate(); en.Fields["propellantReqMet"].guiActive = false; } });
				//activeEngines.ForEach(en => en.currentThrottle = oldEngines[0].currentThrottle);
				foreach (var engine in oldEngines)
				{
					if (!activeEngines.Contains(engine) && engine.EngineIgnited)
					{
						engine.Shutdown();
						//engine.isEnabled = false;
					}
				}
			}

			activeEngines.ForEach(en => en.manuallyOverridden = true);
		}

		private IEnumerator DisableFields()
		{
			yield return new WaitUntil(() =>  activeEngines.Any(en => en.Fields["propellantReqMet"].guiActive));
			activeEngines.ForEach(en => en.Fields["propellantReqMet"].guiActive = false);
		}

		public bool isOperational
		{
			get
			{
				return activeEngines[0].isOperational;
			}
		}

		public float normalizedOutput
		{
			get
			{
				return activeEngines[0].normalizedOutput;
			}
		}

		public float throttleSetting
		{
			get
			{
				return activeEngines[0].throttleSetting;
			}
		}

		public string engineName
		{
			get
			{
				return activeEngines[0].engineName;
			}
		}
	}
}
