using System;
using UnityEngine;
using System.Collections.Generic;
using KSPCommunityFixes;
using System.Linq;
using StarshipExpansionProject.Utils.DataTypes;
using KSP.Localization;
// TODO: Remove unused items from State dict, probably in OnLoad? 
//       Throw error if State dict already contains a key at load time
//       Known issue: OnLoad throws on Load due to stock code UpdateMass. Harmless but not nice.

namespace StarshipExpansionProject.Modules
{
    // Part Module
    public class ModuleSEPProceduralEngineGUI : PartModule, IPartMassModifier
    {
        #region Static strings
        // Static strings
        public const string MODULENAME = nameof(ModuleSEPProceduralEngineGUI);

        public const string CustomSelectableNodeName = "CUSTOMSELECTABLE";
        public const string EngineSetNodeName = "ENGINESET";
        public const string EngineTypeNodeName = "ENGINETYPE";
        #endregion

        #region Serialized Fields
        // Serialized Fields
        [SerializeField]
        public List<CustomSelectable> customSelectables;

        [SerializeField]
        public List<EngineSet> engineSets;

        [SerializeField]
        public List<EngineType> engineTypes;

        [SerializeField]
        public List<Transform> transforms;

        [SerializeField]
        private float Mass;
        #endregion

        #region KSP Fields
        // KSP Fields
        [KSPField]
        public string transformNameSeperator = string.Empty;

        [KSPField(isPersistant = true)]
        public PersistentDictionaryValueTypeKey<string, PersistentDictionaryValueTypes<string, bool>> State = new PersistentDictionaryValueTypeKey<string, PersistentDictionaryValueTypes<string, bool>>();

        [KSPField(isPersistant = true)]
        public int activeEngineType = 0;

        [KSPField]
        public string modifierKey = "LeftShift";
        private KeyCode? _modifierKey;
        public KeyCode ModifierKey
        {
            get
            {
                if (_modifierKey == null)
                {
                    _modifierKey = (KeyCode) Enum.Parse(typeof(KeyCode), modifierKey);
                }
                return (KeyCode) _modifierKey;
            }
        }

        [KSPField]
        public bool usingEngineSwitch = true;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "#LOC_SEP_SelectEnginesGUIName")]
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "#LOC_SEP_SelectEnginesGUIName")]
        public void OpenGui()
        {
            isVisible = true;
        }
        #endregion

        #region Private UI Fields
        // Private UI Fields
        private Rect windowRect = new Rect(275, 200, 250, 374);
        private bool isVisible = false;
        private Vector2 customSelectablesScrollPosition = Vector2.zero;
        private string uiName = Localizer.GetStringByTag("#LOC_SEP_GUILabelName");
        private string uiEngineRingTooltip = Localizer.Format("#LOC_SEP_EngineRingTooltip", new object[] { "{0}", "{1}", "{2}" });
        private string uiRemove = Localizer.GetStringByTag("#LOC_SEP_remove");
        private string uiInstall = Localizer.GetStringByTag("#LOC_SEP_install");
        private float uiWidth = 236;
        private static int highestuiIndex = -1;
        private int? _uiIndex;
        private int uiIndex
        {
            get
            {
                if (_uiIndex is null)
                {
                    highestuiIndex++;
                    _uiIndex = highestuiIndex;
                }
                return (int) _uiIndex;
            }
        }
        private float? _uiScale;
        private float uiScale
        {
            get
            {
                if (_uiScale == null)
                {
                    var tmpPositions = EnginePositionsDict.SelectMany(ep => ep.Value);
                    var maxExtent = tmpPositions.Max(t => t.magnitude);
                    _uiScale = (uiWidth / 2 - engineSize / 2 - 3) / maxExtent;
                }

                return (float) _uiScale;
            }
        }
        private Texture2D _uiTexture;
        private Texture2D uiTexture
        {
            get
            {
                if (_uiTexture == null)
                {
                    _uiTexture = new Texture2D(2, 2);
                    Color color = new Color(0, 0, 0, 0.8f);
                    Color[] pixels = new Color[2 * 2];
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = color;
                    _uiTexture.SetPixels(pixels);
                    _uiTexture.Apply();
                }
                return _uiTexture;
            }
        }
        private float engineSize = 20;
        private float uiTooltipTime;
        #endregion

        #region Private Fields
        // Private Fields
        private Dictionary<int, List<CustomSelectable>> _customSelectablesDict;
        private Dictionary<int, List<CustomSelectable>> CustomSelectablesDict
        {
            get
            {
                if (_customSelectablesDict == null)
                {
                    _customSelectablesDict = new Dictionary<int, List<CustomSelectable>>();
                    for (int i = 0; i < engineTypes.Count; i++)
                    {
                        _customSelectablesDict.Add(i, customSelectables.Where(s => engineTypes[i].CustomSelectableNames.Contains(s.name)).ToList());
                    }
                }
                return _customSelectablesDict;
            }
        }

        private Dictionary<int, Dictionary<string, EngineSet>> _engineSetDict;
        private Dictionary<int, Dictionary<string, EngineSet>> EngineSetDict
        {
            get
            {
                if (_engineSetDict == null)
                {
                    _engineSetDict = new Dictionary<int, Dictionary<string, EngineSet>>();
                    for (int i = 0; i < engineTypes.Count; i++)
                    {
                        _engineSetDict.Add(i, engineSets.Where(s => engineTypes[i].EngineSetNames.Contains(s.name)).ToDictionary(s => s.name, s => s));
                    }
                }
                return _engineSetDict;
            }
        }

        private Dictionary<string, List<Vector2>> _enginePositionsDict;
        private Dictionary<string, List<Vector2>> EnginePositionsDict
        {
            get
            {
                if (_enginePositionsDict == null)
                {
                    _enginePositionsDict = new Dictionary<string, List<Vector2>>();
                    for (int i = 0; i < engineSets.Count(); i++)
                    {
                        var tmpTransformPositions = engineSets[i]
                                                   .selectableItems
                                                   .Select(t => part.transform
                                                               .InverseTransformPoint(TransformsDict[t.name][0].position));
                        _enginePositionsDict.Add(engineSets[i].name
                                               , tmpTransformPositions.Select(p => new Vector2(p.x, p.z)).ToList());
                    }
                }
                return _enginePositionsDict;
            }
        }

        private Dictionary<string, List<Transform>> _transformsDict;
        private Dictionary<string, List<Transform>> TransformsDict
        {
            get
            {
                if (_transformsDict == null)
                {
                    _transformsDict = new Dictionary<string, List<Transform>>();
                    foreach (var set in engineSets)
                    {
                        foreach (SelectableItem selectable in set.selectableItems)
                        {
                            _transformsDict.Add(selectable.name, selectable.transformIndexes.Select(i => transforms[i]).ToList());
                        }
                    }
                    foreach (var sel in customSelectables)
                    {
                        foreach (SelectableItem selectable in sel.selectableItems)
                        {
                            _transformsDict.Add(selectable.name, selectable.transformIndexes.Select(i => transforms[i]).ToList());
                        }
                    }
                }
                return _transformsDict;
            }
        }

        private Dictionary<int, Dictionary<string, Dictionary<RelationshipType, bool>>> _relationshipsDict;
        private Dictionary<int, Dictionary<string, Dictionary<RelationshipType, bool>>> RelationshipsDict
        {
            get
            {
                if (_relationshipsDict == null)
                {
                    _relationshipsDict = new Dictionary<int, Dictionary<string, Dictionary<RelationshipType, bool>>>();
                    for (int i = 0; i < engineTypes.Count(); i++)
                    {
                        foreach (string Key in EngineSetDict[i].Keys)
                        {
                            SetRelationshipState(Key, pEngineType: i);
                        }
                        for (int j = 0; j < CustomSelectablesDict[i].Count; j++)
                        {
                            SetRelationshipState(CustomSelectablesDict[i][j].name, pEngineType: i);
                        }
                    }
                }
                return _relationshipsDict;
            }
        }

        private Dictionary<int, Dictionary<string, List<CustomSelectable>>> _relatedSelectablesDict;
        private Dictionary<int, Dictionary<string, List<CustomSelectable>>> RelatedSelectablesDict
        {
            get
            {
                if (_relatedSelectablesDict == null)
                {
                    _relatedSelectablesDict = new Dictionary<int, Dictionary<string, List<CustomSelectable>>>();
                    foreach (var kvp in CustomSelectablesDict)
                    {
                        _relatedSelectablesDict.Add(kvp.Key, new Dictionary<string, List<CustomSelectable>>());
                        foreach (var selectable in kvp.Value)
                        {
                            foreach (var rel in selectable.relationships)
                            {
                                if (!_relatedSelectablesDict[kvp.Key].ContainsKey(rel.referencedGroupName))
                                    _relatedSelectablesDict[kvp.Key][rel.referencedGroupName] = new List<CustomSelectable>();

                                _relatedSelectablesDict[kvp.Key][rel.referencedGroupName].Add(selectable);
                            }
                        }
                    }
                }

                return _relatedSelectablesDict;
            }
        }

        private Dictionary<int, Dictionary<string, List<SelectableItem>>> _activeSelectableItemsDict;
        private Dictionary<int, Dictionary<string, List<SelectableItem>>> ActiveSelectableItemsDict
        {
            get
            {
                if (_activeSelectableItemsDict == null)
                {
                    _activeSelectableItemsDict = new Dictionary<int, Dictionary<string, List<SelectableItem>>>();
                    for (int i = 0; i < engineTypes.Count; i++)
                    {
                        var tmpDict = new Dictionary<string, List<SelectableItem>>();
                        foreach (var kvp in EngineSetDict[i])
                        {
                            tmpDict.Add(kvp.Key, kvp.Value.selectableItems);
                        }
                        foreach (var selectable in CustomSelectablesDict[i])
                        {
                            tmpDict.Add(selectable.name, selectable.selectableItems);
                        }
                        _activeSelectableItemsDict.Add(i, tmpDict);
                    }
                }
                return _activeSelectableItemsDict;
            }
        }

        private Dictionary<int, Dictionary<string, List<SelectableItem>>> _inactiveSelectableItemsDict;
        private Dictionary<int, Dictionary<string, List<SelectableItem>>> InactiveSelectableItemsDict
        {
            get
            {
                if (_inactiveSelectableItemsDict == null)
                {
                    _inactiveSelectableItemsDict = new Dictionary<int, Dictionary<string, List<SelectableItem>>>();
                    for (int i = 0; i < engineTypes.Count; i++)
                    {
                        var tmpDict = new Dictionary<string, List<SelectableItem>>();
                        foreach (var set in engineSets)
                        {
                            if (EngineSetDict[i].ContainsKey(set.name)) continue;
                            tmpDict.Add(set.name, set.selectableItems);
                        }
                        foreach (var selectable in customSelectables)
                        {
                            if (CustomSelectablesDict[i].Contains(selectable)) continue;
                            tmpDict.Add(selectable.name, selectable.selectableItems);
                        }
                        _inactiveSelectableItemsDict.Add(i, tmpDict);
                    }
                }
                return _inactiveSelectableItemsDict;
            }
        }

        private Dictionary<string, AttachNode> _attachedNodesDict;
        private Dictionary<string, AttachNode> AttachedNodesDict
        {
            get
            {
                if (_attachedNodesDict == null)
                    _attachedNodesDict = part.attachNodes.ToDictionary(item => item.id, item => item);
                return _attachedNodesDict;
            }
        }

        private Dictionary<string, ModuleEngines> _engineModulesDict;
        private Dictionary<string, ModuleEngines> EngineModulesDict
        {
            get
            {
                if (_engineModulesDict == null)
                {
                    var modules = part.FindModulesImplementing<ModuleEngines>();
                    var engineIds = engineSets.Select(es => es.engineId);
                    _engineModulesDict = modules.Where(m => engineIds.Contains(m.engineID)).ToDictionary(m => m.engineID, m => m);
                }
                return _engineModulesDict;
            }
        }

        private Dictionary<int, Dictionary<string, List<EngineSet>>> _activeEngineModulesDict;
        private Dictionary<int, Dictionary<string, List<EngineSet>>> ActiveEngineModulesDict
        {
            get
            {
                if (_activeEngineModulesDict == null)
                {
                    _activeEngineModulesDict = new Dictionary<int, Dictionary<string, List<EngineSet>>>();
                    for (int i = 0; i < engineTypes.Count; i++)
                    {
                        _activeEngineModulesDict.Add(i, new Dictionary<string, List<EngineSet>>());

                        foreach (var kvp in EngineSetDict[i])
                        {
                            if (!_activeEngineModulesDict[i].ContainsKey(kvp.Value.engineId))
                                _activeEngineModulesDict[i][kvp.Value.engineId] = new List<EngineSet> { kvp.Value };
                            else _activeEngineModulesDict[i][kvp.Value.engineId].Add(kvp.Value);
                        }
                    }
                }
                return _activeEngineModulesDict;
            }
        }

        private ModuleSEPEngineSwitch _engineSwitch;
        private ModuleSEPEngineSwitch EngineSwitch
        {
            get
            {
                if (_engineSwitch == null)
                {
                    _engineSwitch = part.FindModuleImplementingFast<ModuleSEPEngineSwitch>();
                }
                return _engineSwitch;
            }
        }
        #endregion

        #region KSP Methods
        // KSP Methods
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            // this assumes b9ps sends a complete confnode, just modified
            bool isInitialise = node.HasNode(CustomSelectableNodeName) || node.HasNode(EngineSetNodeName) || node.HasNode(EngineTypeNodeName);

            if (customSelectables == null || isInitialise) customSelectables = new List<CustomSelectable>();
            if (engineSets == null || isInitialise) engineSets = new List<EngineSet>();
            if (engineTypes == null || isInitialise) engineTypes = new List<EngineType>();
            if (transforms == null || isInitialise) transforms = new List<Transform>();
            if (isInitialise)
            {
                _customSelectablesDict = null;
                _engineSetDict = null;
                _enginePositionsDict = null;
                _transformsDict = null;
                _relationshipsDict = null;
                _relatedSelectablesDict = null;
                _activeSelectableItemsDict = null;
                _inactiveSelectableItemsDict = null;
                _attachedNodesDict = null;
                _activeEngineModulesDict = null;

                _uiScale = null;
            }
            else if (State.Keys.Count == 0) GetStateFromPrefab();

            foreach (var tmpNode in node.GetNodes())
            {
                var nodeType = tmpNode.name;
                switch (nodeType)
                {
                    case CustomSelectableNodeName:
                        if (!ParseCustomSelectable(node: tmpNode))
                            Debug.LogError($"[{MODULENAME}] Failed to parse {nodeType} found in config for part {part.partInfo?.title}\n\n{tmpNode}");
                        break;
                    case EngineSetNodeName:
                        if (!ParseEngineSet(node: tmpNode))
                            Debug.LogError($"[{MODULENAME}] Failed to parse {nodeType} found in config for part {part.partInfo?.title}\n\n{tmpNode}");
                        break;
                    case EngineTypeNodeName:
                        if (!ParseEngineType(node: tmpNode))
                            Debug.LogError($"[{MODULENAME}] Failed to parse {nodeType} found in config for part {part.partInfo?.title}\n\n{tmpNode}");
                        break;
                }
            }

            if (isInitialise)
            {
                ProcessRelationships(out _);
                ProcessMassState();
                ProcessThrustState();
                //if (HighLogic.LoadedScene == GameScenes.LOADING) part.UpdateMass();
                if (HighLogic.LoadedScene == GameScenes.LOADING) part.mass = Mass;
            }
        }

        public void OnGUI()
        {
            if (isVisible) windowRect = GUI.Window(uiIndex, windowRect, DrawUI, uiName);
        }

        public void Start()
        {
            if (State.Keys.Count == 0) GetStateFromPrefab();
            ProcessRelationships(out _);
            ProcessTransformState();
            ProcessMassState();
            ProcessThrustState();
            GameEvents.onEditorSymmetryModeChange.Add(SetSymmetryState);
        }

        public void OnDestroy()
        {
            GameEvents.onEditorSymmetryModeChange.Remove(SetSymmetryState);
        }

        public float GetModuleMass(float baseMass, ModifierStagingSituation situation)
        {
            return Mass;
        }
        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;
        #endregion

        #region Custom Methods
        // Custom Methods
        private bool ParseCustomSelectable(ConfigNode node)
        {
            List<(VmlSeverity, string)> vml = new List<(VmlSeverity, string)>();
            var tmpCustomSelectable = ScriptableObject.CreateInstance<CustomSelectable>();

            // Fill toplevel object
            if (node.HasValue(nameof(tmpCustomSelectable.name)))
                tmpCustomSelectable.name = node.GetValue(nameof(tmpCustomSelectable.name));
            else vml.Add((VmlSeverity.Error, $"{CustomSelectableNodeName} \"{tmpCustomSelectable.name}\" has no value {nameof(tmpCustomSelectable.name)}"));

            if (node.HasValue(nameof(tmpCustomSelectable.guiName)))
                tmpCustomSelectable.guiName = node.GetValue(nameof(tmpCustomSelectable.guiName));
            else
            {
                tmpCustomSelectable.guiName = tmpCustomSelectable.name;
                vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpCustomSelectable.name}\" has no value {nameof(tmpCustomSelectable.guiName)}, using default: {tmpCustomSelectable.name}"));
            }

            // This one is a little special because we don't need to store it in this object
            bool tmpDefaultState = true;
            if (node.HasValue("defaultState"))
                tmpDefaultState = bool.Parse(node.GetValue("defaultState"));
            else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpCustomSelectable.name}\" has no value defaultState, using default: {tmpDefaultState}"));

            if (!State.ContainsKey(tmpCustomSelectable.name)) State.Add(tmpCustomSelectable.name, new PersistentDictionaryValueTypes<string, bool>());

            // Fill Items
            if (node.HasNode("ITEM"))
            {
                foreach (var tmpNode in node.GetNodes("ITEM"))
                {
                    var tmpItem = ScriptableObject.CreateInstance<SelectableItem>();
                    if (tmpNode.HasValue(nameof(tmpItem.name)))
                        tmpItem.name = tmpNode.GetValue(nameof(tmpItem.name));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value {nameof(tmpItem.name)} using transformName"));

                    if (tmpNode.HasValue("transformName"))
                    {
                        tmpItem.transformName = tmpNode.GetValue("transformName");
                        if (string.IsNullOrEmpty(tmpItem.name)) tmpItem.name = tmpItem.transformName;
                    }
                    
                    else vml.Add((VmlSeverity.Error, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value transformName"));

                    if (tmpNode.HasValue(nameof(tmpItem.invertTransformState)))
                        tmpItem.invertTransformState = bool.Parse(tmpNode.GetValue(nameof(tmpItem.invertTransformState)));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value {nameof(tmpItem.invertTransformState)}, using default: {tmpItem.invertTransformState}"));

                    if (tmpNode.HasValue("nodeTransformName"))
                        tmpItem.nodeId = tmpNode.GetValue("nodeTransformName");
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value nodeTransformName, no node will be created"));

                    if (tmpNode.HasValue(nameof(tmpItem.invertNodeState)))
                        tmpItem.invertNodeState = bool.Parse(tmpNode.GetValue(nameof(tmpItem.invertNodeState)));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value {nameof(tmpItem.invertNodeState)}, using default: {tmpItem.invertNodeState}"));

                    int tmpNodeSize = 2;
                    if (tmpNode.HasValue("nodeSize"))
                        tmpNodeSize = int.Parse(tmpNode.GetValue("nodeSize"));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value nodeSize, using default: {tmpNodeSize}"));

                    if (tmpNode.HasValue(nameof(tmpItem.mass)))
                        tmpItem.mass = float.Parse(tmpNode.GetValue(nameof(tmpItem.mass)));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value {nameof(tmpItem.mass)}, using default: {tmpItem.mass}"));

                    if (tmpItem.name == string.Empty) continue;

                    var tmpTransforms = part.FindModelTransforms(tmpItem.transformName);
                    Transform tmpNodeTransform = null;
                    if (tmpItem.nodeId != null && tmpItem.nodeId != string.Empty)
                        tmpNodeTransform = part.FindModelTransform(tmpItem.nodeId);
                    if (tmpTransforms.Length == 0)
                    {
                        continue;
                    }
                    if (tmpItem.nodeId != null && tmpItem.nodeId != string.Empty && tmpNodeTransform == null)
                    {
                        Debug.LogError($"[{MODULENAME}] {CustomSelectableNodeName} no transform found with name \"{tmpItem.nodeId}\"");
                    }
                    tmpItem.transformIndexes.AddRange(Enumerable.Range(transforms.Count, tmpTransforms.Length));
                    transforms.AddRange(tmpTransforms);
                    if (tmpItem.nodeId != string.Empty && tmpNodeTransform != null)
                        CreateAttachNodeIfNotExists(NodeId: tmpItem.nodeId, NodeSize: tmpNodeSize);
                    if (!State[tmpCustomSelectable.name].ContainsKey(tmpItem.name))
                        State[tmpCustomSelectable.name].Add(tmpItem.name, tmpDefaultState);
                    tmpCustomSelectable.selectableItems.Add(tmpItem);
                }
            }

            // Fill Relationships
            if (node.HasNode("RELATIONSHIP"))
            {
                foreach (var tmpNode in node.GetNodes("RELATIONSHIP"))
                {
                    var tmpRelationship = ScriptableObject.CreateInstance<Relationship>();
                    tmpRelationship.name = tmpCustomSelectable.name + "Relationship" + transformNameSeperator + tmpCustomSelectable.relationships.Count;
                    if (tmpNode.HasValue(nameof(tmpRelationship.referencedGroupName)))
                        tmpRelationship.referencedGroupName = tmpNode.GetValue(nameof(tmpRelationship.referencedGroupName));
                    else vml.Add((VmlSeverity.Error, $"{CustomSelectableNodeName} \"{tmpRelationship.name}\" has no value {nameof(tmpRelationship.referencedGroupName)}"));

                    if (tmpNode.HasValue(nameof(tmpRelationship.guiContext)))
                        tmpRelationship.guiContext = tmpNode.GetValue(nameof(tmpRelationship.guiContext));
                    else
                    {
                        tmpRelationship.guiContext = $"Locked by {tmpRelationship.referencedGroupName} due to {tmpRelationship.typeRelationship.ToString()}";
                        vml.Add((VmlSeverity.Warning, $"{CustomSelectableNodeName} \"{tmpRelationship.name}\" has no value {nameof(tmpRelationship.guiContext)}, using default {tmpRelationship.guiContext}"));
                    }

                    if (tmpNode.HasValue(nameof(tmpRelationship.invertRelationship)))
                        tmpRelationship.invertRelationship = bool.Parse(tmpNode.GetValue(nameof(tmpRelationship.invertRelationship)));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpRelationship.name}\" has no value {nameof(tmpRelationship.invertRelationship)}, using default: {tmpRelationship.invertRelationship}"));

                    if (tmpNode.HasValue(nameof(tmpRelationship.typeRelationship))
                     && Enum.TryParse(tmpNode.GetValue(nameof(tmpRelationship.typeRelationship)), out tmpRelationship.typeRelationship)) { }
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpRelationship.name}\" has no value {nameof(tmpRelationship.typeRelationship)}, using default: {tmpRelationship.typeRelationship}"));

                    if (tmpRelationship.name == string.Empty) continue;

                    tmpCustomSelectable.relationships.Add(tmpRelationship);
                }
            }

            customSelectables.Add(tmpCustomSelectable);

            var vmlResult = LogVml(Vml: vml);
            return vmlResult;
        }
        private bool ParseEngineSet(ConfigNode node)
        {
            List<(VmlSeverity, string)> vml = new List<(VmlSeverity, string)>();
            var tmpEngineSet = ScriptableObject.CreateInstance<EngineSet>();
            
            // Fill toplevel object
            if (node.HasValue(nameof(tmpEngineSet.name))) 
                tmpEngineSet.name = node.GetValue(nameof(tmpEngineSet.name));
            else vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.name)}"));

            if (node.HasValue(nameof(tmpEngineSet.guiName)))
                tmpEngineSet.guiName = node.GetValue(nameof(tmpEngineSet.guiName));
            else
            {
                tmpEngineSet.guiName = tmpEngineSet.name;
                vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.guiName)}, using default: {tmpEngineSet.name}"));
            }

            if (node.HasValue(nameof(tmpEngineSet.engineId)))
            {
                tmpEngineSet.engineId = node.GetValue(nameof(tmpEngineSet.engineId));
                if (part.FindModulesImplementingReadOnly<ModuleEngines>().FirstOrDefault(m => (m as ModuleEngines).engineID == tmpEngineSet.engineId) == null)
                    vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} part \"{part.partInfo?.title}\" has no module implementing ModuleEngines with engineId \"{tmpEngineSet.engineId}\""));
            }
            else vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.engineId)}"));

            if (node.HasValue(nameof(tmpEngineSet.singleEngineThrust)))
                tmpEngineSet.singleEngineThrust = float.Parse(node.GetValue(nameof(tmpEngineSet.singleEngineThrust)));
            else vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.singleEngineThrust)}"));

            if (node.HasValue(nameof(tmpEngineSet.singleEngineMinThrust)))
                tmpEngineSet.singleEngineMinThrust = float.Parse(node.GetValue(nameof(tmpEngineSet.singleEngineMinThrust)));
            else vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.singleEngineMinThrust)}, using default: {tmpEngineSet.singleEngineMinThrust}"));

            // These ones are a little special because we don't need to store them in this object
            bool tmpDefaultState = true;
            if (node.HasValue("defaultState"))
                tmpDefaultState = bool.Parse(node.GetValue("defaultState"));
            else vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value defaultState, using default: {tmpDefaultState}"));

            float tmpMass = 0f;
            if (node.HasValue("mass"))
                tmpMass = float.Parse(node.GetValue("mass"));
            else vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value mass, using default: {tmpMass}"));

            string tmpTransformName = string.Empty;
            if (node.HasValue("transformName"))
                tmpTransformName = node.GetValue("transformName");
            else vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value transformName"));

            string tmpNodeTransformName = string.Empty;
            if (node.HasValue("nodeTransformName"))
                tmpNodeTransformName = node.GetValue("nodeTransformName");
            else vml.Add((VmlSeverity.Warning, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value nodeTransformName, no nodes will be created"));

            bool tmpInvertNodeState = true;
            if (node.HasValue("invertNodeState"))
                tmpInvertNodeState = bool.Parse(node.GetValue("invertNodeState"));
            else vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value invertNodeState, using default: {tmpInvertNodeState}"));

            int tmpNodeSize = 2;
            if (node.HasValue("nodeSize"))
                tmpNodeSize = int.Parse(node.GetValue("nodeSize"));
            else vml.Add((VmlSeverity.Warning, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value nodeSize, using default: {tmpNodeSize}"));

            if (!State.ContainsKey(tmpEngineSet.name)) State.Add(tmpEngineSet.name, new PersistentDictionaryValueTypes<string, bool>());

            int i = 0;
            var vmlResult = LogVml(Vml: vml);
            while (vmlResult)
            {
                if (tmpTransformName == string.Empty) break;
                var tmpFullTransformName = tmpTransformName + transformNameSeperator + i;
                var tmpFullNodeTransformName = tmpNodeTransformName + transformNameSeperator + i;

                var tmpTransforms = part.FindModelTransforms(tmpFullTransformName);
                Transform tmpNodeTransform = null;
                if (tmpNodeTransformName != string.Empty) 
                    tmpNodeTransform = part.FindModelTransform(tmpFullNodeTransformName);
                if (tmpTransforms.Length == 0) break;
                if (tmpNodeTransformName != string.Empty && tmpNodeTransform == null)
                {
                    Debug.LogError($"[{MODULENAME}] {EngineSetNodeName} no transform found with name \"{tmpNodeTransformName}\"");
                }

                // Fill the object
                var tmpSelectableItem = ScriptableObject.CreateInstance<SelectableItem>();
                tmpSelectableItem.name = tmpFullTransformName;
                tmpSelectableItem.mass = tmpMass;
                if (!State[tmpEngineSet.name].ContainsKey(tmpSelectableItem.name)) 
                    State[tmpEngineSet.name].Add(tmpSelectableItem.name, tmpDefaultState);
                tmpSelectableItem.transformIndexes.AddRange(Enumerable.Range(transforms.Count, tmpTransforms.Length));
                transforms.AddRange(tmpTransforms);
                if (tmpNodeTransformName != string.Empty && tmpNodeTransform != null)
                {
                    tmpSelectableItem.nodeId = tmpFullNodeTransformName;
                    tmpSelectableItem.invertNodeState = tmpInvertNodeState;
                    CreateAttachNodeIfNotExists(NodeId: tmpSelectableItem.nodeId, NodeSize: tmpNodeSize);
                }
                tmpEngineSet.selectableItems.Add(tmpSelectableItem);
                i++;
            }
            Debug.Log($"[{MODULENAME}] {EngineSetNodeName} \"{tmpEngineSet.name}\" found {i} matching transforms");

            // Replace the engineset if it already exists, this should only happen when B9PS calls OnLoad.
            var tmpExistingEngineSetIndex = engineSets.FindIndex(e => e.name == tmpEngineSet.name);
            if (tmpExistingEngineSetIndex != -1) engineSets[tmpExistingEngineSetIndex] = tmpEngineSet;
            else engineSets.Add(tmpEngineSet);

            return vmlResult;
        }
        private bool ParseEngineType(ConfigNode node)
        {
            List<(VmlSeverity, string)> vml = new List<(VmlSeverity, string)>();
            var tmpEngineType = ScriptableObject.CreateInstance<EngineType>();

            // Fill toplevel object
            if (node.HasValue(nameof(tmpEngineType.name)))
                tmpEngineType.name = node.GetValue(nameof(tmpEngineType.name));
            else vml.Add((VmlSeverity.Error, $"{EngineTypeNodeName} \"{tmpEngineType.name}\" has no value {nameof(tmpEngineType.name)}"));

            if (node.HasValue("engineSetName"))
                tmpEngineType.EngineSetNames = node.GetValues("engineSetName").ToList();
            else vml.Add((VmlSeverity.Error, $"{EngineTypeNodeName} \"{tmpEngineType.name}\" has no value(s) for engineSetName"));

            if (node.HasValue("customSelectableName"))
                tmpEngineType.CustomSelectableNames = node.GetValues("customSelectableName").ToList();
            else vml.Add((VmlSeverity.Information, $"{EngineTypeNodeName} \"{tmpEngineType.name}\" has no value(s) for customSelectableName"));

            var vmlResult = LogVml(Vml: vml);
            engineTypes.Add(tmpEngineType);

            return vmlResult;
        }

        private bool LogVml(List<(VmlSeverity type, string msg)> Vml)
        {
            var errors = false;
            foreach (var msg in Vml)
            {
                switch (msg.type)
                {
                    case VmlSeverity.Error:
                        errors = true;
                        Debug.LogError($"[{MODULENAME}] {msg.msg}");
                        break;
                    case VmlSeverity.Warning:
                        Debug.LogWarning($"[{MODULENAME}] {msg.msg}");
                        break;
                    default:
                        Debug.Log($"[{MODULENAME}] {msg.msg}");
                        break;
                }
            }
            return errors == false;
        }
        private void CreateAttachNodeIfNotExists(string NodeId, int NodeSize)
        {
            if (part.FindAttachNode(NodeId) == null)
            {
                ConfigNode node = new ConfigNode();
                node.AddValue("name", NodeId);
                node.AddValue("transform", NodeId);
                node.AddValue("size", NodeSize.ToString());
                node.AddValue("method", "FIXED_JOINT");
                part.AddAttachNode(node);
            }
        }
        private void DrawUI(int windowIndex)
        {
            // Setup
            GUI.DragWindow(new Rect(0, 0, windowRect.width - 20, 20));
            float barButtonWidth = 60;
            float barButtonHeight = 35;

            Rect closeButtonRect = new Rect(windowRect.width - 22, 2, 20, 20);
            GUIStyle buttonStyle = GUI.skin.button;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.fontSize = 15;
            GUIStyle labelStyle = GUI.skin.label;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.wordWrap = true;
            GUIStyle engineStyle = new GUIStyle(GUI.skin.toggle);
            engineStyle.padding = new RectOffset(5, 5, 5, 5);
            engineStyle.margin = new RectOffset(0, 0, 0, 0);

            GUI.skin.window.padding = new RectOffset(0, 0, 20, 0);

            if (GUI.Button(closeButtonRect, "X", buttonStyle))
            {
                isVisible = false;
            }

            // Main Window
            GUILayout.BeginVertical();
            GUILayout.Space(5);

            // Top Bar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<"
                               , buttonStyle
                               , GUILayout.Width(barButtonWidth)
                               , GUILayout.Height(barButtonHeight)))
            {
                UpdateActiveEngineType(-1);
            }

            GUILayout.Label(engineTypes[activeEngineType].name
                          , labelStyle
                          , GUILayout.Width(uiWidth - 2 * barButtonWidth)
                          , GUILayout.Height(barButtonHeight));

            if (GUILayout.Button(">"
                               , buttonStyle
                               , GUILayout.Width(barButtonWidth)
                               , GUILayout.Height(barButtonHeight)))
            {
                UpdateActiveEngineType(1);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            // Custom Selectables
            customSelectablesScrollPosition = GUILayout.BeginScrollView(customSelectablesScrollPosition
                                                                      , GUI.skin.box
                                                                      , GUILayout.Height(60));
            GUILayout.BeginVertical();
            foreach(var selectable in CustomSelectablesDict[activeEngineType])
            {
                List<string> tooltips = new List<string>();
                foreach (var relationship in selectable.relationships)
                {
                    if (IsDisabledByRelationship(relationship))
                    {
                        GUI.enabled = false;
                        tooltips.Add(relationship.guiContext);
                    }
                }
                GUIContent content = new GUIContent(selectable.guiName);
                if (tooltips.Count != 0) content.tooltip = string.Join("\n", tooltips);
                if (!State.TryGetValue(selectable.name, out var stateDictionary))
                {
                    Debug.LogError($"[{MODULENAME}] No state found for {selectable.name}. Initializing default state.");
                    var tmpDict = new PersistentDictionaryValueTypes<string, bool>();
                    foreach (var item in selectable.selectableItems) tmpDict.Add(item.name, true);
                    State[selectable.name] = stateDictionary = tmpDict;
                }
                else
                {
                    foreach (var item in selectable.selectableItems)
                    {
                        if (!stateDictionary.ContainsKey(item.name))
                        {
                            stateDictionary[item.name] = true;
                        }
                    }
                }
                bool currentState = State[selectable.name][selectable.selectableItems[0].name];
                if (GUILayout.Toggle(currentState, content, toggleStyle) != currentState)
                {
                    ProcessSelectableButtonEvent(pState: !currentState, pSelectable: selectable);
                }
                if (!GUI.enabled)
                {
                    GUI.enabled = true;
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.Space(3);

            // Engines
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(uiWidth));
            GUILayout.Label(GUIContent.none);
            GUILayout.EndVertical();

            Rect canvasRect = GUILayoutUtility.GetLastRect();
            foreach (var set in EngineSetDict[activeEngineType].Values)
            {
                for (int i = 0; i < set.selectableItems.Count; i++)
                {
                    float tmpX = canvasRect.x + canvasRect.width / 2 + EnginePositionsDict[set.name][i].x * uiScale - engineSize / 2;
                    float tmpY = canvasRect.y + canvasRect.height / 2 + EnginePositionsDict[set.name][i].y * uiScale - engineSize / 2;
                    var tmpPosition = new Rect(tmpX, tmpY, engineSize, engineSize);
                    if (!State.ContainsKey(set.name) || !State[set.name].ContainsKey(set.selectableItems[i].name))
                    {
                        Debug.LogError($"[{MODULENAME}] No state found for {set.name} : {set.selectableItems[i].name} using true");
                        if (!State.ContainsKey(set.name))
                            State[set.name] = new PersistentDictionaryValueTypes<string, bool> { { set.selectableItems[i].name, true } };
                        else
                        {
                            State[set.name][set.selectableItems[i].name] = true;
                        }
                    }
                    bool currentState = State[set.name][set.selectableItems[i].name];
                    GUIContent content = new GUIContent();
                    content.tooltip = string.Format(format: uiEngineRingTooltip
                                                  , arg0: ModifierKey.ToString()
                                                  , arg1: currentState ? uiRemove : uiInstall
                                                  , arg2: set.guiName);
                    if (GUI.Toggle(tmpPosition, currentState, content, engineStyle) != currentState)
                    {
                        ProcessEngineButtonEvent(pState: !currentState
                                               , pEngineSet: set
                                               , pSelectableIndex: i
                                               , pModifierPressed: Input.GetKey(ModifierKey));
                    }
                }
            }
            GUILayout.Space(3);

            GUILayout.EndVertical();

            // Tooltip
            if (!string.IsNullOrWhiteSpace(GUI.tooltip))
            {
                if (uiTooltipTime == 0f)
                {
                    uiTooltipTime = Time.time;
                }
                else if (Time.time - uiTooltipTime >= 0.75f)
                {
                    Vector2 mousePos = Event.current.mousePosition;
                    GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box);
                    tooltipStyle.wordWrap = true;
                    tooltipStyle.alignment = TextAnchor.UpperLeft;
                    tooltipStyle.normal.background = uiTexture;
                    GUIContent content = new GUIContent(GUI.tooltip);
                    Vector2 tooltipSize = tooltipStyle.CalcSize(content);
                    if (tooltipSize.x > windowRect.width)
                    {
                        tooltipSize.x = windowRect.width;
                        tooltipSize.y = tooltipStyle.CalcHeight(content, windowRect.width);
                    }
                    float tooltipX = mousePos.x + engineSize;
                    float tooltipY = mousePos.y;

                    if (tooltipX + tooltipSize.x > windowRect.width)
                    {
                        tooltipX = windowRect.width - tooltipSize.x;
                        tooltipY += engineSize;
                    }
                    if (tooltipY + tooltipSize.y > windowRect.height)
                    {
                        tooltipY = windowRect.height - tooltipSize.y;
                    }
                    Rect tooltipRect = new Rect(tooltipX, tooltipY, tooltipSize.x, tooltipSize.y);
                    GUI.Box(tooltipRect, GUI.tooltip, tooltipStyle);
                }
            }
            else if (Event.current.type == EventType.Repaint)
            {
                uiTooltipTime = 0;
            }
        }
        public void UpdateActiveEngineType(int dir)
        {
            activeEngineType = (activeEngineType + dir + engineTypes.Count) % engineTypes.Count;
            foreach (var Key in EngineSetDict[activeEngineType].Keys)
            {
                SetRelationshipState(Key);
            }
            foreach (var selectable in CustomSelectablesDict[activeEngineType])
            {
                SetRelationshipState(selectable.name);
            }
            ProcessRelationships(out _);
            ProcessTransformState();
            ProcessMassState();
            ProcessThrustState();
        }
        public void ProcessSelectableButtonEvent(bool pState, CustomSelectable pSelectable)
        {
            if (!State.TryGetValue(pSelectable.name, out var innerDictionary))
            {
                var tmpDict = new PersistentDictionaryValueTypes<string, bool>();
                foreach (var item in pSelectable.selectableItems) tmpDict.Add(item.name, pState);
                State[pSelectable.name] = innerDictionary = tmpDict;
            }
            else
            {
                foreach (var item in pSelectable.selectableItems) innerDictionary[item.name] = pState;
            }

            ProcessCommonEvent(pGroupName: pSelectable.name);
        }
        public void ProcessEngineButtonEvent(bool pState, EngineSet pEngineSet, int pSelectableIndex, bool pModifierPressed)
        {
            if (!State.ContainsKey(pEngineSet.name))
            {
                State[pEngineSet.name] = new PersistentDictionaryValueTypes<string, bool>();
            }

            if (pModifierPressed)
            {
                for (int i = 0; i < pEngineSet.selectableItems.Count; i++)
                {
                    State[pEngineSet.name][pEngineSet.selectableItems[i].name] = pState;
                }
            }
            else
            {
                State[pEngineSet.name][pEngineSet.selectableItems[pSelectableIndex].name] = pState;
            }

            ProcessCommonEvent(pGroupName: pEngineSet.name);
            ProcessThrustState(pEngineId: pEngineSet.engineId);
        }
        public void ProcessCommonEvent(string pGroupName)
        {
            List<string> changedGroups = new List<string>();
            SetRelationshipState(pGroupName: pGroupName);
            if (RelatedSelectablesDict[activeEngineType].ContainsKey(pGroupName))
                ProcessRelationships(out changedGroups, RelatedSelectablesDict[activeEngineType][pGroupName]);
            changedGroups.Add(pGroupName);
            var selectablesDict = changedGroups.ToDictionary(item => item, item => ActiveSelectableItemsDict[activeEngineType][item]);
            ProcessTransformState(selectablesDict);
            ProcessMassState();
        }
        public void ProcessRelationships(out List<string> oEverChangedGroups, List<CustomSelectable> pSelectables = null)
        {
            oEverChangedGroups = new List<string>();
            if (pSelectables == null || pSelectables.Count == 0)
                pSelectables = CustomSelectablesDict[activeEngineType];

            // This effectively acts as a while loop, just with a cap on it so it won't go forever if my logic fails somehow.
            for (int i = 0; i < CustomSelectablesDict[activeEngineType].Count + 1; i++)
            {
                List<string> changedGroups = new List<string>();
                foreach (var selectable in pSelectables)
                {
                    bool currentState = State[selectable.name].First().Value;

                    foreach (var rel in selectable.relationships)
                    {
                        if (IsDisabledByRelationship(rel) && currentState == rel.invertRelationship)
                        {
                            foreach (var sel in selectable.selectableItems)
                                State[selectable.name][sel.name] = !rel.invertRelationship;
                            if (!changedGroups.Contains(selectable.name)) changedGroups.Add(selectable.name);
                        }
                    }
                }
                if (changedGroups.Count == 0) break;
                pSelectables = new List<CustomSelectable>();
                foreach (var group in changedGroups)
                {
                    if (oEverChangedGroups.Contains(group))
                    {
                        Debug.LogError("Recursive Relationship Loop active, module might be left in an unstable state");
                        return;
                    }
                    else
                    {
                        oEverChangedGroups.Add(group);
                    }
                    SetRelationshipState(group);
                    if (RelatedSelectablesDict[activeEngineType].ContainsKey(group))
                        pSelectables.AddRange(RelatedSelectablesDict[activeEngineType][group]);
                }
            }
        }
        public void ProcessTransformState(Dictionary<string, List<SelectableItem>> pSelectables = null, bool? pOverrideState = null)
        {
            if (pSelectables == null)
            {
                ProcessTransformState(pSelectables: ActiveSelectableItemsDict[activeEngineType]);
                ProcessTransformState(pSelectables: InactiveSelectableItemsDict[activeEngineType], pOverrideState: false);
            }
            else
            {
                foreach (var kvp in pSelectables)
                {
                    foreach (var selectable in kvp.Value)
                    {
                        bool state = State[kvp.Key][selectable.name];
                        bool transformState = pOverrideState ?? (selectable.invertTransformState ? !state : state);
                        foreach (var t in TransformsDict[selectable.name])
                        {
                            t.gameObject.SetActive(transformState);
                        }
                        if (!string.IsNullOrWhiteSpace(selectable.nodeId) && AttachedNodesDict.ContainsKey(selectable.nodeId))
                        {
                            bool nodeState = pOverrideState ?? (selectable.invertNodeState ? !state : state);
                            AttachNode node = AttachedNodesDict[selectable.nodeId];
                            if (nodeState)
                            {
                                node.nodeType = AttachNode.NodeType.Stack;
                                node.radius = 0.4f;
                            }
                            else
                            {
                                node.nodeType = AttachNode.NodeType.Dock;
                                node.radius = 1f / 1000f;
                            }
                        }
                    }
                }
            }
        }
        public void ProcessMassState()
        {
            float tmpMass = -part.prefabMass;
            if (ActiveSelectableItemsDict.TryGetValue(activeEngineType, out var items))
            {
                foreach (var kvp in items)
                {
                    if (State.TryGetValue(kvp.Key, out var state))
                    {
                        foreach (var selectable in kvp.Value)
                        {
                            if (state.TryGetValue(selectable.name, out var isActive) && isActive)
                            {
                                tmpMass += selectable.mass;
                            }
                        }
                    }
                }
            }
            Mass = tmpMass;
            if (HighLogic.LoadedSceneIsEditor) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }
        public void ProcessThrustState(string pEngineId = null)
        {
            var engineIds = new List<string>();
            if (string.IsNullOrWhiteSpace(pEngineId))
                engineIds.AddRange(EngineModulesDict.Keys);
            else
                engineIds.Add(pEngineId);

            foreach (var engineId in engineIds)
            {
                var engineModule = EngineModulesDict[engineId];
                var thrustTuple = (minThrust: 0f, maxThrust: 0f);
                if (ActiveEngineModulesDict[activeEngineType].ContainsKey(engineId))
                    foreach (var set in ActiveEngineModulesDict[activeEngineType][engineId])
                    {
                        int setEngineCount = set.selectableItems
                                            .Count(selectable => State.TryGetValue(set.name, out var state)
                                                && state.TryGetValue(selectable.name, out var isEnabled)
                                                && isEnabled);

                        thrustTuple.minThrust += setEngineCount * set.singleEngineMinThrust;
                        thrustTuple.maxThrust += setEngineCount * set.singleEngineThrust;
                    }

                float isp0 = engineModule.atmosphereCurve.Evaluate(0f);
                engineModule.maxThrust = thrustTuple.maxThrust;
                engineModule.minThrust = thrustTuple.minThrust;
                engineModule.maxFuelFlow = thrustTuple.maxThrust / isp0 / engineModule.g;
                engineModule.minFuelFlow = thrustTuple.minThrust / isp0 / engineModule.g;

                var hasThrust = thrustTuple.maxThrust > 0;
                if (!usingEngineSwitch && engineModule.isEnabled != hasThrust)
                {
                    engineModule.isEnabled = hasThrust;
                    engineModule.SetStagingState(hasThrust);
                }
            }

            if (usingEngineSwitch)
            {
                bool anyThrust = EngineSwitch.engines.Sum(en => en.maxThrust) > 0;
                if (EngineSwitch.isEnabled != anyThrust)
                {
                    for (int i = 0; i < EngineSwitch.engines.Count; i++)
                        EngineSwitch.engines[i].SetStagingState(anyThrust);
                    EngineSwitch.isEnabled = anyThrust;
                    EngineSwitch.SetStagingState(anyThrust);
                }
            }
        }
        public bool IsDisabledByRelationship(Relationship pRelationship)
        {
            if (!RelationshipsDict[activeEngineType].ContainsKey(pRelationship.referencedGroupName)) return false;
            return RelationshipsDict[activeEngineType][pRelationship.referencedGroupName][pRelationship.typeRelationship];
        }
        public void SetRelationshipState(string pGroupName, int pEngineType = -1)
        {
            if (pEngineType == -1) pEngineType = activeEngineType;
            var newStates = new Dictionary<RelationshipType, bool>
            {
                { RelationshipType.AllEnabled, State[pGroupName].All(kvp => kvp.Value) },
                { RelationshipType.AllDisabled, State[pGroupName].All(kvp => !kvp.Value) },
                { RelationshipType.AnyEnabled, State[pGroupName].Any(kvp => kvp.Value) },
                { RelationshipType.AnyDisabled, State[pGroupName].Any(kvp => !kvp.Value) },
            };

            if (!_relationshipsDict.ContainsKey(pEngineType)) _relationshipsDict[pEngineType] = new Dictionary<string, Dictionary<RelationshipType, bool>>();
            _relationshipsDict[pEngineType][pGroupName] = newStates;
        }
        public void GetStateFromPrefab()
        {
            var prefab = part.partInfo.partPrefab;
            State = new PersistentDictionaryValueTypeKey<string, PersistentDictionaryValueTypes<string, bool>>();
            var tmpNode = new ConfigNode(nameof(State));
            ((ModuleSEPProceduralEngineGUI) prefab.Modules.GetModule(moduleName)).State.Save(tmpNode);
            State.Load(tmpNode);
        }
        public void SetSymmetryState(int pSymmetryMode)
        {
            if (part.stackSymmetry != pSymmetryMode) part.stackSymmetry = pSymmetryMode;
        }
        #endregion
    }

    #region Custom Data Classes
    // Custom Data Classes
    [Serializable]
    public class CustomSelectable : ScriptableObject
    {
        public string guiName;
        public List<Relationship> relationships = new List<Relationship>();
        public List<SelectableItem> selectableItems = new List<SelectableItem>();
    }

    [Serializable]
    public class EngineSet : ScriptableObject
    {
        public string guiName;
        public string engineId;
        public float singleEngineThrust;
        public float singleEngineMinThrust = 0;

        public List<SelectableItem> selectableItems = new List<SelectableItem>();
    }

    [Serializable]
    public class EngineType : ScriptableObject
    {
        public List<string> EngineSetNames = new List<string>();
        public List<string> CustomSelectableNames = new List<string>();
    }

    [Serializable]
    public class SelectableItem : ScriptableObject
    {
        public string transformName;
        public float mass = 0f;
        public string nodeId;
        public List<int> transformIndexes = new List<int>();
        public bool invertNodeState = true;
        public bool invertTransformState = false;
    }

    [Serializable]
    public class Relationship : ScriptableObject
    {
        public string referencedGroupName;
        public string guiContext;
        public bool invertRelationship = false;
        public RelationshipType typeRelationship = RelationshipType.AnyEnabled;
    }

    public enum VmlSeverity
    {
        Error,
        Warning,
        Information,
    }

    public enum RelationshipType
    {
        AllEnabled,
        AllDisabled,
        AnyEnabled,
        AnyDisabled,
    }
    #endregion
}
