using Newtonsoft.Json;
using RadiusSaveConvertor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RadiusSaveEditorGUI
{
    public partial class Form1 : Form
    {
        private void UpdateTitle(string message)
        {
            Text = "ITR Save Editor by Neo_Kesha - " + message;
        }

        ITR_SaveFile openedSave;
        Dictionary<Type, GroupBox> editors = new Dictionary<Type, GroupBox>();
        Dictionary<Type, Action> editorUpdate = new Dictionary<Type, Action>();
        GroupBox currentEditor = null;
        TreeNode currentNode = null;
        TreeNode collectionNode = null;
        int addAmountValue = 1;

        string[] levelTags = new string[] { "Level.None", "Level.Hub", "Level.Zone.Buffer", "Level.Zone.Factory", "Level.Zone.Kolhoz", "Level.Zone.Castle", "Level.Zone.Construct", "Level.Zone.Railyard", "Level.Zone.River" };
        Dictionary<string, string> levelTagsToNames = new Dictionary<string, string>() {
            { "Level.None", "None" },
            { "Level.Hub", "Hub" },
            { "Level.Zone.Buffer","Buffer"},
            { "Level.Zone.Factory", "Factory" },
            { "Level.Zone.Kolhoz", "Kolhoz" },
            { "Level.Zone.Castle", "Castle" },
            { "Level.Zone.Construct", "Construct" },
            { "Level.Zone.Railyard", "Railyard" },
            { "Level.Zone.River", "River" },
        };
        Dictionary<string, float> levelSizes = new Dictionary<string, float>(){
            { "None", 1.0f },
            { "Hub", 1.0f }, 
            { "Buffer", 1.0f },
            { "Factory", 1.0f }, 
            { "Kolhoz", 1.0f }, 
            { "Castle", 1.0f }, 
            { "Construct", 1.0f }, 
            { "Railyard", 1.0f }, 
            { "River", 1.0f }, 
        };
        Dictionary<string, MapSurveyInfo> levelSurvey = new Dictionary<string, MapSurveyInfo>();

        ITR_PROP selectedProp;
        ITR_ICollection selectedCollection;
        ITR_ITEM selectedItem;
        public Form1()
        {
            InitializeComponent();
            var defaultLocation = editFSingle.Location;
            editors.Add(typeof(ITR_FSingle),            editFSingle);           editorUpdate.Add(typeof(ITR_FSingle),               UpdateEditorFSingle);
            editors.Add(typeof(ITR_FString),            editFString);           editorUpdate.Add(typeof(ITR_FString),               UpdateEditorFString);
            editors.Add(typeof(ITR_FVector),            editFVector);           editorUpdate.Add(typeof(ITR_FVector),               UpdateEditorFVector);
            editors.Add(typeof(ITR_FQuaternion),        editFQuaternion);       editorUpdate.Add(typeof(ITR_FQuaternion),           UpdateEditorFQuaternion);
            editors.Add(typeof(ITR_FTransform),         editFTransform);        editorUpdate.Add(typeof(ITR_FTransform),            UpdateEditorFTransform);
            editors.Add(typeof(ITR_FInt32),             editFInt32);            editorUpdate.Add(typeof(ITR_FInt32),                UpdateEditorFInt32);
            editors.Add(typeof(ITR_FDateTime),          editFDateTime);         editorUpdate.Add(typeof(ITR_FDateTime),             UpdateEditorFDateTime);
            editors.Add(typeof(ITR_FGameplayTagsTuple), editFTagTuple);         editorUpdate.Add(typeof(ITR_FGameplayTagsTuple),    UpdateEditorFTagTuple);
            editors.Add(typeof(ITR_FBool),              editFBool);             editorUpdate.Add(typeof(ITR_FBool),                 UpdateEditorFBool);
            editors.Add(typeof(ITR_FEnum),              editFEnum);             editorUpdate.Add(typeof(ITR_FEnum),                 UpdateEditorFEnum);
            editors.Add(typeof(ITR_FInt64),             editFInt64);            editorUpdate.Add(typeof(ITR_FInt64),                UpdateEditorFInt64);
            editors.Add(typeof(ITR_FUInt64),            editFUInt64);           editorUpdate.Add(typeof(ITR_FUInt64),               UpdateEditorFUInt64);
            editors.Add(typeof(ITR_FTagToTagsList),     editFTagToTag);         editorUpdate.Add(typeof(ITR_FTagToTagsList),        UpdateEditorFTagToTags);

            foreach (var editor in editors.Values)
            {
                editor.Location = defaultLocation;
            }
            foreach (var level in levelTags)
            {
                var levelName = levelTagsToNames[level];
                listOfLevels.Items.Add(levelName);
                levelSurvey.Add(levelName, new MapSurveyInfo(levelSizes[levelName]));
            }
            UpdateTitle("");
            HideAllEditors();
            collectionManipulator.Location = new Point(573, 506);
            HideCollectionManipulator();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void UpdateMapSurvey()
        {

            foreach (var level in levelSizes.Keys)
            {
                levelSurvey[level].Clear();
            }
            var levelInventories = (ITR_MAP)((ITR_OBJ)openedSave.mainObject.GetProp("InventoryData").value).GetProp("LevelInventories").value;            
            var spawnSaveData = (ITR_OBJ)openedSave.mainObject.GetProp("SpawnSaveData").value;
            var npcSpawners = (ITR_MAP)spawnSaveData.GetProp("npcSpawners").value;
            var anomalySpawners = (ITR_MAP)spawnSaveData.GetProp("anomalySpawners").value;
            foreach (var levelTag in levelTags)
            {
                var levelName = levelTagsToNames[levelTag];
                foreach (var levelInventory in levelInventories.keys)
                {
                    if (levelInventory.ToString() == levelTag)
                    {
                        ITR_ARRAY items = (ITR_ARRAY)((ITR_OBJ)levelInventories.Get(levelInventory)).GetProp("Items").value;
                        levelSurvey[levelName].levelItems.AddRange(items.items.Select(o => (ITR_OBJ)o).ToArray());
                        break;
                    }
                }

                foreach (var npcSpawner in npcSpawners.keys)
                {
                    if (npcSpawner.ToString().Contains(levelName))
                    {
                        var enemies = (ITR_FNPCDataMap)npcSpawners.Get(npcSpawner);
                        levelSurvey[levelName].levelEnemies.AddRange(enemies.enemies);
                    }
                }
                foreach (var anomalySpawner in anomalySpawners.keys)
                {
                    if (anomalySpawners.ToString().Contains(levelName))
                    {
                        var anomalyField = (ITR_FAnomalyFieldData)npcSpawners.Get(anomalySpawner);
                        levelSurvey[levelName].levelAnomalies.AddRange(anomalyField.anomalies.anomalies);
                        levelSurvey[levelName].levelArtifacts.AddRange(anomalyField.artifacts.artifacts);
                    }
                }
                levelSurvey[levelName].CountBoundaries();
                levelSurvey[levelName].Render();
            }
        }
        public void HideAllEditors()
        {
            currentEditor = null;
            foreach(var key in editors.Keys)
            {
                editors[key].Hide();
            }
        }
        public void ShowEditor(Type type)
        {
            currentEditor?.Hide();
            if (editors.ContainsKey(type))
            {
                currentEditor = editors[type];
                currentEditor.Show();
                UpdateEditor(type);
            } else
            {
                currentEditor = null;
            }
        }

        public void ShowCollectionManipulator(bool allowClone)
        {
            collectionManipulator.Show();
            cloneSelectedItem.Enabled = allowClone;
            removeSelectedItem.Enabled = allowClone;
        }
        public void HideCollectionManipulator()
        {
            collectionManipulator.Hide();
        }
        public void UpdateEditor(Type type)
        {
            editorUpdate[type].Invoke();
        }
        public void UpdateEditorFSingle()
        {
            if (selectedItem != null)
            {
                editorFSingleValue.Text = selectedItem.ToString();
            }
        }
        public void UpdateEditorFString()
        {
            if (selectedItem != null)
            {
                editorFStringValue.Text = selectedItem.ToString();
            }
        }
        public void UpdateEditorFVector()
        {
            if (selectedItem != null)
            {
                var vector = (ITR_FVector)selectedItem;
                editorFVectorXValue.Text = vector.x.ToString();
                editorFVectorYValue.Text = vector.y.ToString();
                editorFVectorZValue.Text = vector.z.ToString();
            }
        }
        public void UpdateEditorFQuaternion()
        {
            if (selectedItem != null)
            {
                var quaternion = (ITR_FQuaternion)selectedItem;
                editorFQuaternionXValue.Text = quaternion.x.ToString();
                editorFQuaternionYValue.Text = quaternion.y.ToString();
                editorFQuaternionZValue.Text = quaternion.z.ToString();
                editorFQuaternionWValue.Text = quaternion.w.ToString();
            }
        }
        public void UpdateEditorFTransform()
        {
            if (selectedItem != null)
            {
                var transform = (ITR_FTransform)selectedItem;
                editorFTransformRotX.Text = transform.rotation.x.ToString();
                editorFTransformRotY.Text = transform.rotation.y.ToString();
                editorFTransformRotZ.Text = transform.rotation.z.ToString();
                editorFTransformRotW.Text = transform.rotation.w.ToString();
                editorFTransformPosX.Text = transform.position.x.ToString();
                editorFTransformPosY.Text = transform.position.y.ToString();
                editorFTransformPosZ.Text = transform.position.z.ToString();
                editorFTransformScaleX.Text = transform.scale.x.ToString();
                editorFTransformScaleY.Text = transform.scale.y.ToString();
                editorFTransformScaleZ.Text = transform.scale.z.ToString();
            }
        }
        public void UpdateEditorFInt32()
        {
            if (selectedItem != null)
            {
                editorFInt32Value.Text = selectedItem.ToString();
            }
        }
        public void UpdateEditorFDateTime()
        {
            if (selectedItem != null)
            {
                editorFDateTimeValue.Text = selectedItem.ToString();
            }
        }
        public void UpdateEditorFTagTuple()
        {
            if (selectedItem != null)
            {
                var tagTuple = (ITR_FGameplayTagsTuple)selectedItem;
                editorFTagTupleValue1.Text = tagTuple.key.ToString();
                editorFTagTupleValue2.Text = tagTuple.value.ToString();
            }
        }
        public void UpdateEditorFBool()
        {
            if (selectedItem != null)
            {
                editorFBoolValue.Text = selectedItem.ToString();
            }
        }

        public void UpdateEditorFEnum()
        {
            if (selectedItem != null)
            {
                editorFEnumValue.Text = selectedItem.ToString();
            }
        }
        public void UpdateEditorFInt64()
        {
            if (selectedItem != null)
            {
                editorFInt64Value.Text = selectedItem.ToString();
            }
        }
        public void UpdateEditorFUInt64()
        {
            if (selectedItem != null)
            {
                editorFUInt64Value.Text = selectedItem.ToString();
            }
        }
        public void UpdateEditorFTagToTags()
        {
            if (selectedItem != null)
            {
                var tagToTag = (ITR_FTagToTagsList)selectedItem;
                editorFTagToTagValue.Text = tagToTag.tag;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = openFile.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (File.Exists(openFile.FileName))
                {
                    var ext = Path.GetExtension(openFile.FileName);
                    if (ext == ".save")
                    {
                        using (var stream = new FileStream(openFile.FileName, FileMode.Open, FileAccess.Read))
                        {
                            var reader = new BinaryReader(stream);
                            openedSave = new ITR_SaveFile(reader);
                        }
                        HideAllEditors();
                        HideCollectionManipulator();
                        UpdateMapSurvey();
                        UpdateTree();
                        UpdateTitle(openFile.FileName);
                    }
                    if (ext == ".json")
                    {
                        using (var reader = new StreamReader(openFile.FileName))
                        {
                            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                            openedSave = JsonConvert.DeserializeObject<ITR_SaveFile>(reader.ReadToEnd(), settings);
                        }
                        HideAllEditors();
                        HideCollectionManipulator();
                        UpdateMapSurvey();
                        UpdateTree();
                        UpdateTitle(openFile.FileName);
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openedSave == null)
            {
                return;
            }

            var result = saveFile.ShowDialog();
            if (result == DialogResult.OK)
            {
                var ext = Path.GetExtension(saveFile.FileName);
                if (ext == ".save")
                {
                    using (var stream = new FileStream(saveFile.FileName, FileMode.Create, FileAccess.Write))
                    {
                        var writer = new BinaryWriter(stream);
                        openedSave.Write(writer);
                    }
                }
                if (ext == ".json")
                {
                    using (var writer = new StreamWriter(saveFile.FileName))
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented };
                        writer.Write(JsonConvert.SerializeObject(openedSave, settings));
                    }
                }
            }
        }

        private void UpdateTree()
        {
            saveBrowser.BeginUpdate();
            saveBrowser.Nodes.Clear();
            UpdateTreeObj(saveBrowser.Nodes.Add("saveRoot", "saveRoot"), openedSave.mainObject);
            saveBrowser.EndUpdate();
        }
        private void UpdateTreeObj(TreeNode root, ITR_OBJ obj)
        {
            root.Tag = obj;
            root.Text = obj.ToString();
            foreach (var prop in obj.props)
            {
                var node = root.Nodes.Add("");
                UpdateTreeProp(node, prop);
            }
        }
        private void UpdateTreeProp(TreeNode root, ITR_PROP prop)
        {
            root.Tag = prop;
            root.Text = prop.ToString();
            if (prop.className != "AActor*")
            {
                MapPropertyTypeToUpdate(root, prop.value, prop.propType, prop.className);
            }
        }
        private void UpdateTreeArray(TreeNode root, ITR_ARRAY array)
        {
            root.Tag = array;
            root.Text = array.ToString();
            foreach (var item in array.items)
            {
                var node = root.Nodes.Add(item.ToString());
                node.Tag = item;
                MapPropertyTypeToUpdate(node, item, array.propertyType, array.className, false);
            }
        }
        private void UpdateTreeMap(TreeNode root, ITR_MAP map)
        {
            root.Tag = map;
            root.Text = map.ToString();
            for (var i = 0; i < map.keys.Count; ++i)
            {
                var keyNode = root.Nodes.Add(map.keys[i].ToString());
                keyNode.Tag = map.keys[i];
                MapClassNameToUpdate(keyNode, map.keys[i], map.keyClass, false);
                var valueNode = keyNode.Nodes.Add("-");
                valueNode.Tag = map.values[i];
                if (map.valuePropertyType == "ObjectProperty")
                {
                    MapPropertyTypeToUpdate(valueNode, map.values[i], map.valuePropertyType, map.valueClass, false);
                }
                else
                {
                    MapClassNameToUpdate(valueNode, map.values[i], map.valueClass, false);
                }
            }
        }
        private void MapPropertyTypeToUpdate(TreeNode root, object value, string propType, string className, bool addNewNode = true)
        {
            switch (propType)
            {
                case "ObjectProperty":
                    var objectNode = (addNewNode) ? root.Nodes.Add("") : root;
                    UpdateTreeObj(objectNode, (ITR_OBJ)value);
                    break;
                case "ArrayProperty":
                    var arrayNode = (addNewNode) ? root.Nodes.Add("") : root;
                    UpdateTreeArray(arrayNode, (ITR_ARRAY)value);
                    break;
                case "MapProperty":
                    var mapNode = (addNewNode) ? root.Nodes.Add("") : root;
                    UpdateTreeMap(mapNode, (ITR_MAP)value);
                    break;
                case "StructProperty":
                    MapClassNameToUpdate(root, ((ITR_STRUCT)value).value, className, addNewNode);
                    break;
                default:
                    break;
            }

        }
        private void MapClassNameToUpdate(TreeNode root, object value, string className, bool addNewNode = true)
        {
            var actualRoot = (addNewNode)?root.Nodes.Add(""):root;
            switch (className)
            {
                case "FGameplayTag":
                    break;
                case "FTransform":
                    break;
                case "FVector":
                    break;
                case "FQuaternion":
                    break;
                case "FTagToTagsList":
                    UpdateTreeFTagToTags(actualRoot, (ITR_FTagToTagsList)value);
                    break;
                case "FTagToItemConfigsList":
                    UpdateTreeFTagToItemConfigsList(actualRoot, (ITR_FTagToItemConfigsList)value);
                    break;
                case "FAmmoContainerData":
                    UpdateTreeFAmmoContainerData(actualRoot, (ITR_FAmmoContainerData)value);
                    break;
                case "FStringToTagsList":
                    UpdateTreeFTagToTags(actualRoot, (ITR_FTagToTagsList)value);
                    break;
                case "FString":
                    break;
                case "FNPCDataMap":
                    UpdateTreeFNPCDataMap(actualRoot, (ITR_FNPCDataMap)value);
                    break;
                case "FNPCData":
                    UpdateTreeFNPCData(actualRoot, (ITR_FNPCData)value);
                    break;
                case "FAnomalyFieldData":
                    UpdateTreeFAnomalyFieldData(actualRoot, (ITR_FAnomalyFieldData)value);
                    break;
                case "FAnomalyData":
                    UpdateTreeFAnomaly(actualRoot, (ITR_FAnomaly)value);
                    break;
                case "FArtifactData":
                    UpdateTreeFArtifact(actualRoot, (ITR_FArtifact)value);
                    break;
                case "FFurnitureData":
                    UpdateTreeFFurnitureData(actualRoot, (ITR_FFurnitureData)value);
                    break;
                case "FFurniture":
                    UpdateTreeFFurniture(actualRoot, (ITR_FFurniture)value);
                    break;
                case "FNameArray":
                    UpdateTreeFNameArray(actualRoot, (ITR_FNameArray)value);
                    break;
                case "FSpawnSlotData":
                    UpdateTreeFSpawnSlotData(actualRoot, (ITR_FSpawnSlotData)value);
                    break;
                case "FSpawnSlot":
                    UpdateTreeFSpawnSlot(actualRoot, (ITR_FSpawnSlot)value);
                    break;
                case "int32":
                    break;
                case "float":
                    break;
                case "FDateTime":
                    break;
                case "FPlayerStats":
                    UpdateTreeFPlayerStats(actualRoot, (ITR_FPlayerStats)value);
                    break;
                case "FGameplayTagsTuple":
                    break;
                case "FGameDifficulty":
                    UpdateTreeFGameDifficulty(actualRoot, (ITR_FGameDifficulty)value);
                    break;
                case "FRadiusGameDifficulty":
                    UpdateTreeFRadiusGameDifficulty(actualRoot, (ITR_FRadiusGameDifficulty)value);
                    break;
                case "FMapData":
                    break;
                case "FLevelDecorData":
                    UpdateTreeFLevelDecorData(actualRoot, (ITR_FLevelDecorData)value);
                    break;
                case "FLevelDecor":
                    UpdateTreeFLevelDecor(actualRoot, (ITR_FLevelDecor)value);
                    break;
            }

            actualRoot.Tag = value;
            actualRoot.Text = value.ToString();
        }
        private void UpdateTreeFTagToItemConfigsList(TreeNode root, ITR_FTagToItemConfigsList configs)
        {
            root.Nodes.Add(" Tag").Tag = configs.tag;
            foreach (var config in configs.itemConfigs)
            {
                var node = root.Nodes.Add(config.ToString());
                node.Tag = config;
                UpdateTreeFItemConfig(node, config);
            }
        }
        private void UpdateTreeFItemConfig(TreeNode root, ITR_ItemConfig config)
        {
            root.Nodes.Add(" Name").Tag = config.itemName;
            root.Nodes.Add(" Unknown Int").Tag = config.someInt;
            root.Nodes.Add(" Unknown String").Tag = config.someString1;
            root.Nodes.Add(" Unknown String").Tag = config.someString2;
            root.Nodes.Add(" Unknown Vector").Tag = config.someVector;
            root.Nodes.Add(" Unknown Vector").Tag = config.someVectors[0];
            root.Nodes.Add(" Unknown Vector").Tag = config.someVectors[1];
            root.Nodes.Add(" Unknown Vector").Tag = config.someVectors[2];
            root.Nodes.Add(" Unknown Vector").Tag = config.someVectors[3];
        }
        private void UpdateTreeFNPCDataMap(TreeNode root, ITR_FNPCDataMap npcDataMap)
        {
            foreach(var npc in npcDataMap.enemies)
            {
                var node = root.Nodes.Add(npc.ToString());
                node.Tag = npc;
                UpdateTreeFNPCData(node, npc);
            }
        }
        private void UpdateTreeFNPCData(TreeNode root, ITR_FNPCData data)
        {
            root.Nodes.Add(" Name").Tag = data.name1;
            root.Nodes.Add(" Transform").Tag = data.transform;
            root.Nodes.Add(" ID").Tag = data.name2;
            root.Nodes.Add(" Archetype").Tag = data.name3;
            root.Nodes.Add(" Health").Tag = data.health;
            root.Nodes.Add(" Unknown Float").Tag = data.floats[0];
            root.Nodes.Add(" Unknown Float").Tag = data.floats[1];
            root.Nodes.Add(" Unknown Float").Tag = data.floats[2];
            root.Nodes.Add(" Unknown Float").Tag = data.floats[3];
            root.Nodes.Add(" Unknown Enum").Tag = data.govno;
        }
        private void UpdateTreeFAnomalyFieldData(TreeNode root, ITR_FAnomalyFieldData fieldData)
        {
            var anomaliesNode = root.Nodes.Add(fieldData.anomalies.ToString());
            anomaliesNode.Tag = fieldData.anomalies;
            foreach (var anomaly in fieldData.anomalies.anomalies)
            {
                var node = anomaliesNode.Nodes.Add(anomaly.ToString());
                node.Tag = anomaly;
                UpdateTreeFAnomaly(node, anomaly);
            }
            var artifactsNode = root.Nodes.Add(fieldData.artifacts.ToString());
            artifactsNode.Tag = fieldData.artifacts;
            foreach (var artifact in fieldData.artifacts.artifacts)
            {
                var node = artifactsNode.Nodes.Add(artifact.ToString());
                node.Tag = artifact;
                UpdateTreeFArtifact(node, artifact);
            }
        }
        private void UpdateTreeFAnomaly(TreeNode root, ITR_FAnomaly anomaly)
        {
            root.Nodes.Add(" ID").Tag = anomaly.anomalyId;
            root.Nodes.Add(" Name").Tag = anomaly.anomalyName;
            root.Nodes.Add(" Blueprint").Tag = anomaly.anomalyBpName;
            root.Nodes.Add(" Transform").Tag = anomaly.transform;
            root.Nodes.Add(" Scale?").Tag = anomaly.scale;
            var transformsNode = root.Nodes.Add(" Additional Transforms");
            foreach (var transform in anomaly.transforms)
            {
                transformsNode.Nodes.Add(transform.ToString()).Tag = transform;
            }
        }
        private void UpdateTreeFArtifact(TreeNode root, ITR_FArtifact artifact)
        {
            root.Nodes.Add(" ID").Tag = artifact.artifactId;
            root.Nodes.Add(" Name").Tag = artifact.artifactName;
            root.Nodes.Add(" Transform").Tag = artifact.transform;
        }
        private void UpdateTreeFFurnitureData(TreeNode root, ITR_FFurnitureData data)
        {
            foreach (var furniture in data.furnitureList)
            {
                var node = root.Nodes.Add(furniture.ToString());
                node.Tag = furniture;
                UpdateTreeFFurniture(node, furniture);
            }
        }
        private void UpdateTreeFFurniture(TreeNode root, ITR_FFurniture data)
        {
            root.Nodes.Add(" Name").Tag = data.furnitureName;
            root.Nodes.Add(" Transform").Tag = data.transform;
        }
        private void UpdateTreeFSpawnSlotData(TreeNode root, ITR_FSpawnSlotData data)
        {
            foreach (var slot in data.slots)
            {
                var node = root.Nodes.Add(slot.ToString());
                node.Tag = slot;
                UpdateTreeFSpawnSlot(node, slot);
            }
        }
        private void UpdateTreeFSpawnSlot(TreeNode root, ITR_FSpawnSlot slot)
        {
            UpdateTreeObj(root.Nodes.Add(""), slot.obj);
            root.Nodes.Add(" Transform").Tag = slot.transform;
        }
        private void UpdateTreeFLevelDecorData(TreeNode root, ITR_FLevelDecorData data)
        {
            var decorationNode = root.Nodes.Add(data.decorList.ToString());
            decorationNode.Tag = data.decorList;
            foreach (var decor in data.decorList.decorList)
            {
                var node = decorationNode.Nodes.Add(decor.ToString());
                node.Tag = decor;
                UpdateTreeFLevelDecor(node, decor);
            }
            var decorationIdsNode = root.Nodes.Add(data.decorIdList.ToString());
            decorationIdsNode.Tag = data.decorIdList;
            foreach (var id in data.decorIdList.decorIdList)
            {
                var node = decorationIdsNode.Nodes.Add(id.ToString());
                node.Tag = id;
            }
        }
        private void UpdateTreeFLevelDecor(TreeNode root, ITR_FLevelDecor decor)
        {
            root.Nodes.Add(" Name").Tag = decor.name;
            root.Nodes.Add(" Transform").Tag = decor.transform;
        }
        private void UpdateTreeFAmmoContainerData(TreeNode root, ITR_FAmmoContainerData ammoContainerData)
        {
            UpdateTreeProp(root.Nodes.Add(""), ammoContainerData.ammo);
            UpdateTreeProp(root.Nodes.Add(""), ammoContainerData.chamberAmmo);
        }
        private void UpdateTreeFPlayerStats(TreeNode root, ITR_FPlayerStats stats)
        {
            UpdateTreeProp(root.Nodes.Add(""), stats.deaths);
            UpdateTreeProp(root.Nodes.Add(""), stats.kills);
        }
        private void UpdateTreeFGameDifficulty(TreeNode root, ITR_FGameDifficulty gameDifficulty)
        {
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.sleepRestoreHealth);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.locationOnMap);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.enemySense);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.showTracers);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.itemsDropType);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.showTips);
        }
        private void UpdateTreeFRadiusGameDifficulty(TreeNode root, ITR_FRadiusGameDifficulty gameDifficulty)
        {
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.sleepRestoreHealth);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.locationOnMap);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.showTips);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.hunger);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.enemySense);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.enemyHealth);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.enemyDamage);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.enemyCount);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.itemsDropType);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.itemSellPrice);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.weaponShootDamage);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.anomalyDamage);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.anomalyAmount);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.showTracers);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.missionMoneyReward);
            UpdateTreeProp(root.Nodes.Add(""), gameDifficulty.tideTime);
        }
        private void UpdateTreeFNameArray(TreeNode root, ITR_FNameArray names)
        {
            foreach (var name in names.names)
            {
                var node = root.Nodes.Add(name.ToString());
                node.Tag = name;
            }
        }
        private void UpdateTreeFTagToTags(TreeNode root, ITR_FTagToTagsList tags)
        {
            foreach (var tag in tags.tags)
            {
                var node = root.Nodes.Add(tag.ToString());
                node.Tag = tag;
            }
        }

        private void saveBrowser_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (currentNode != null && !currentNode.Text.StartsWith(" ") && currentNode.Tag != null) //im sorry
            {
                var itemText = currentNode.Tag.ToString();
                if (currentNode.Text != itemText)
                {
                    currentNode.Text = itemText;
                }
            }

            ITR_ITEM item = (ITR_ITEM)((TreeView)sender).SelectedNode.Tag;
            if (item != null)
            {
                currentNode = ((TreeView)sender).SelectedNode;
                selectedItem = item;
                if (item is ITR_PROP)
                {
                    selectedProp = (ITR_PROP)item;
                    selectedItem = selectedProp.value;
                } 
                else
                {
                    selectedProp = null;
                    if (selectedItem is ITR_ICollection)
                    {
                        selectedCollection = (ITR_ICollection)selectedItem;
                        collectionNode = currentNode;
                        ShowCollectionManipulator(false);
                    }
                    else if (currentNode.Parent != null && currentNode.Parent.Tag is ITR_ICollection)
                    {
                        selectedCollection = (ITR_ICollection)currentNode.Parent.Tag;
                        collectionNode = currentNode.Parent;
                        ShowCollectionManipulator(true);
                    }
                    else
                    {
                        selectedCollection = null;
                        HideCollectionManipulator();
                    }
                }
                if (selectedItem != null)
                {
                    ShowEditor(selectedItem.GetType());
                } else
                {
                    HideAllEditors();
                }
            } else
            {
                HideAllEditors();
            }
        }
        private bool ColorParseSingle(TextBox textBox, out Single result)
        {
            try
            {
                result = Single.Parse(textBox.Text);
                textBox.BackColor = Color.White;
                return true;
            }
            catch
            {
                result = 0;
                textBox.BackColor = Color.Red;
                return false;
            }
        }
        private bool ColorParseInt32(TextBox textBox, out Int32 result)
        {
            try
            {
                result = Int32.Parse(textBox.Text, System.Globalization.CultureInfo.InvariantCulture);
                textBox.BackColor = Color.White;
                return true;
            }
            catch
            {
                result = 0;
                textBox.BackColor = Color.Red;
                return false;
            }
        }
        private bool ColorParseDateTime(TextBox textBox, out DateTime result)
        {
            try
            {
                result = DateTime.Parse(textBox.Text, System.Globalization.CultureInfo.InvariantCulture);
                textBox.BackColor = Color.White;
                return true;
            }
            catch
            {
                result = DateTime.Now;
                textBox.BackColor = Color.Red;
                return false;
            }
        }
        private bool ColorParseBool(TextBox textBox, out Boolean result)
        {
            try
            {
                if (textBox.Text.ToLower().Trim() == "false")
                {
                    result = false;
                }
                else if (textBox.Text.ToLower().Trim() == "true")
                {
                    result = true;
                } 
                else
                {
                    result = Int32.Parse(textBox.Text) != 0;
                }
                textBox.BackColor = Color.White;
                return true;
            }
            catch
            {
                result = false;
                textBox.BackColor = Color.Red;
                return false;
            }
        }
        private bool ColorParseInt64(TextBox textBox, out Int64 result)
        {
            try
            {
                result = Int64.Parse(textBox.Text, System.Globalization.CultureInfo.InvariantCulture);
                textBox.BackColor = Color.White;
                return true;
            }
            catch
            {
                result = 0;
                textBox.BackColor = Color.Red;
                return false;
            }
        }
        private bool ColorParseUInt64(TextBox textBox, out UInt64 result)
        {
            try
            {
                result = UInt64.Parse(textBox.Text, System.Globalization.CultureInfo.InvariantCulture);
                textBox.BackColor = Color.White;
                return true;
            }
            catch
            {
                result = 0;
                textBox.BackColor = Color.Red;
                return false;
            }
        }
        private bool ColorParseByte(TextBox textBox, out Byte result)
        {
            try
            {
                result = Byte.Parse(textBox.Text, System.Globalization.CultureInfo.InvariantCulture);
                textBox.BackColor = Color.White;
                return true;
            }
            catch
            {
                result = 0;
                textBox.BackColor = Color.Red;
                return false;
            }
        }
        private void editorFStringValue_TextChanged(object sender, EventArgs e)
        {
            var fsingle = (ITR_FSingle)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                fsingle.value = value;
            }
        }

        private void editorFStringValue_TextChanged_1(object sender, EventArgs e)
        {
            var fstring = (ITR_FString)selectedItem;
            fstring.str = ((TextBox)sender).Text;
        }

        private void editorFVectorXValue_TextChanged(object sender, EventArgs e)
        {
            var fvector = (ITR_FVector)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                fvector.x = value;
            }
        }

        private void editorFVectorYValue_TextChanged(object sender, EventArgs e)
        {
            var fvector = (ITR_FVector)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                fvector.y = value;
            }
        }

        private void editorFVectorZValue_TextChanged(object sender, EventArgs e)
        {
            var fvector = (ITR_FVector)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                fvector.z = value;
            }
        }

        private void editorFQuaternionXValue_TextChanged(object sender, EventArgs e)
        {
            var fquaternion = (ITR_FQuaternion)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                fquaternion.x = value;
            }
        }

        private void editorFQuaternionYValue_TextChanged(object sender, EventArgs e)
        {
            var fquaternion = (ITR_FQuaternion)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                fquaternion.y = value;
            }
        }

        private void editorFQuaternionZValue_TextChanged(object sender, EventArgs e)
        {
            var fquaternion = (ITR_FQuaternion)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                fquaternion.z = value;
            }
        }

        private void editorFQuaternionWValue_TextChanged(object sender, EventArgs e)
        {
            var fquaternion = (ITR_FQuaternion)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                fquaternion.w = value;
            }
        }

        private void editorFTransformRotX_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.rotation.x = value;
            }
        }

        private void editorFTransformRotY_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.rotation.y = value;
            }
        }

        private void editorFTransformRotZ_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.rotation.z = value;
            }
        }

        private void editorFTransformRotW_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.rotation.w = value;
            }
        }

        private void editorFTransformPosX_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.position.x = value;
            }
        }

        private void editorFTransformPosY_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.position.y = value;
            }
        }

        private void editorFTransformPosZ_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.position.z = value;
            }
        }

        private void editorFTransformScaleX_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.scale.x = value;
            }
        }

        private void editorFTransformScaleY_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.scale.y = value;
            }
        }

        private void editorFTransformScaleZ_TextChanged(object sender, EventArgs e)
        {
            var ftransform = (ITR_FTransform)selectedItem;
            Single value;
            if (ColorParseSingle((TextBox)sender, out value))
            {
                ftransform.scale.z = value;
            }
        }

        private void editorFInt32Value_TextChanged(object sender, EventArgs e)
        {
            var fint32 = (ITR_FInt32)selectedItem;
            Int32 value;
            if (ColorParseInt32((TextBox)sender, out value))
            {
                fint32.value = value;
            }
        }

        private void editorFDateTimeValue_TextChanged(object sender, EventArgs e)
        {
            var dateTime = (ITR_FDateTime)selectedItem;
            DateTime value;
            if (ColorParseDateTime((TextBox)sender, out value))
            {
                dateTime.value = value;
            }
        }

        private void editorFTagTupleValue1_TextChanged(object sender, EventArgs e)
        {
            var tuple = (ITR_FGameplayTagsTuple)selectedItem;
            tuple.key = ((TextBox)sender).Text;
        }

        private void editorFTagTupleValue2_TextChanged(object sender, EventArgs e)
        {
            var tuple = (ITR_FGameplayTagsTuple)selectedItem;
            tuple.value = ((TextBox)sender).Text;
        }

        private void editorFBoolValue_TextChanged(object sender, EventArgs e)
        {
            var fbool = (ITR_FBool)selectedItem;
            bool value;
            if (ColorParseBool((TextBox)sender, out value))
            {
                fbool.value = value;
            }
        }

        private void editorFEnumValue_TextChanged(object sender, EventArgs e)
        {
            var fenum = (ITR_FEnum)selectedItem;
            Byte value;
            if (ColorParseByte((TextBox)sender, out value))
            {
                fenum.value = value;
            }
        }

        private void editorFInt64_TextChanged(object sender, EventArgs e)
        {
            var fint64 = (ITR_FInt64)selectedItem;
            Int64 value;
            if (ColorParseInt64((TextBox)sender, out value))
            {
                fint64.value = value;
            }
        }

        private void editorFUInt64_TextChanged(object sender, EventArgs e)
        {
            var fuint64 = (ITR_FUInt64)selectedItem;
            UInt64 value;
            if (ColorParseUInt64((TextBox)sender, out value))
            {
                fuint64.value = value;
            }
        }

        private void editorFTagToTagValue_TextChanged(object sender, EventArgs e)
        {
            var tagToTags = (ITR_FTagToTagsList)selectedItem;
            tagToTags.tag = ((TextBox)sender).Text;
        }

        private void addAmount_TextChanged(object sender, EventArgs e)
        {
            Int32 value;
            if (ColorParseInt32((TextBox)sender, out value))
            {
                addAmountValue = value;
            }
        }

        private void addNewItem_Click(object sender, EventArgs e)
        {
            saveBrowser.BeginUpdate();
            var mapHack = selectedCollection is ITR_MAP;
            ITR_MAP map = null;
            if (mapHack)
            {
                map = (ITR_MAP)selectedCollection;
            }
            for (var i = 0; i < addAmountValue; ++i)
            {
                var propType = selectedCollection.GetItemPropertyType();
                if (propType == "StructProperty")
                {
                    var item = selectedCollection.Add();
                    var node = collectionNode.Nodes.Add(item.ToString());
                    node.Tag = item;
                    MapClassNameToUpdate(node, item, selectedCollection.GetItemClassName(), false);
                    if (mapHack)
                    {
                        MapHack(map, node);
                    }
                }
                else
                {
                    var item = selectedCollection.Add();
                    var node = collectionNode.Nodes.Add(item.ToString());
                    node.Tag = item;
                    MapPropertyTypeToUpdate(node, item, selectedCollection.GetItemPropertyType(), selectedCollection.GetItemClassName(), false);
                    if (mapHack)
                    {
                        MapHack(map, node);
                    }
                }
            }
            collectionNode.Text = collectionNode.Tag.ToString();
            saveBrowser.EndUpdate();
        }

        private void MapHack(ITR_MAP map, TreeNode keyNode)
        {
            var lastValue = map.values.Last();
            var valueNode = keyNode.Nodes.Add("-");
            valueNode.Tag = lastValue;
            if (map.valuePropertyType == "ObjectProperty")
            {
                MapPropertyTypeToUpdate(valueNode, lastValue, map.valuePropertyType, map.valueClass, false);
            }
            else
            {
                MapClassNameToUpdate(valueNode, lastValue, map.valueClass, false);
            }
        }

        private void cloneSelectedItem_Click(object sender, EventArgs e)
        {
            saveBrowser.BeginUpdate();
            var mapHack = selectedCollection is ITR_MAP;
            ITR_MAP map = null;
            if (mapHack)
            {
                map = (ITR_MAP)selectedCollection;
            }
            for (var i = 0; i < addAmountValue; ++i)
            {
                var propType = selectedCollection.GetItemPropertyType();
                var item = selectedCollection.AddCopy(selectedItem);
                var node = collectionNode.Nodes.Add(item.ToString());
                node.Tag = selectedItem;
                if (propType == "StructProperty")
                {
                    MapClassNameToUpdate(node, selectedItem, selectedCollection.GetItemClassName(), false);
                    if (mapHack)
                    {
                        MapHack(map, node);
                    }
                } 
                else
                {
                    MapPropertyTypeToUpdate(node, item, propType, selectedCollection.GetItemClassName(), false);
                    if (mapHack)
                    {
                        MapHack(map, node);
                    }
                }
            }
            collectionNode.Text = collectionNode.Tag.ToString();
            saveBrowser.EndUpdate();
        }

        private void removeSelectedItem_Click(object sender, EventArgs e)
        {
            saveBrowser.BeginUpdate();
            selectedCollection.Remove(selectedItem);
            var prevIndex = Math.Max(currentNode.Index - 1, 0);
            collectionNode.Nodes.Remove(currentNode);
            collectionNode.Text = collectionNode.Tag.ToString();
            saveBrowser.EndUpdate();
            if (collectionNode.Nodes.Count == 0)
            {
                saveBrowser.SelectedNode = collectionNode;
            }
            else
            {
                saveBrowser.SelectedNode = collectionNode.Nodes[prevIndex];
            }
            saveBrowser.Focus();
        }

        private void clearCollection_Click(object sender, EventArgs e)
        {
            selectedCollection.Clear();
            saveBrowser.BeginUpdate();
            collectionNode.Nodes.Clear();
            collectionNode.Text = collectionNode.Tag.ToString();
            saveBrowser.EndUpdate();
            saveBrowser.SelectedNode = collectionNode;
            saveBrowser.Focus();
        }

        private void UpdateMap()
        {
            if (listOfLevels.SelectedItem == null)
            {
                return;
            }
            var levelName = listOfLevels.SelectedItem.ToString();
            pictureBox1.Image = levelSurvey[levelName].GetMap(showEnemies.Checked, showItems.Checked, showArtifacts.Checked, showAnomalies.Checked);
        }

        private void listOfLevels_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }
        private void showEnemies_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }
        private void showAnomalies_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }
        private void showArtifacts_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }
        private void showItems_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }
    }
}
