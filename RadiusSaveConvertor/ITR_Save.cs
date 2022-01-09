using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace RadiusSaveConvertor
{
    public class ITR_SaveFile
    {
        public int version;
        [JsonProperty("saveRoot")]
        public ITR_OBJ mainObject;

        public ITR_SaveFile()
        {
        }

        public ITR_SaveFile(BinaryReader reader)
        {
            version = reader.ReadInt32();
            mainObject = new ITR_OBJ(reader);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(version);
            mainObject.Write(writer);
        }
    }

    public class ITR_ITEM
    {
        public virtual void Write(BinaryWriter writer)
        {

        }
        public virtual void Write(BinaryWriter writer, string className)
        {
            Write(writer);
        }
    }

    public interface ITR_ICollection
    {
        ITR_ITEM Add();
        ITR_ITEM AddCopy(ITR_ITEM item);
        void Remove(ITR_ITEM item);
        void Clear();
        string GetItemPropertyType();
        string GetItemClassName();
    }

    public class ITR_OBJ : ITR_ITEM
    {
        public const string OBJ_BEGIN = "OBJ-BEGIN";
        public const string OBJ_END = "OBJ-END";

        [JsonProperty("objectName")]
        public string name;
        [JsonProperty("className")]
        public string className;
        [JsonProperty("properties")]
        public List<ITR_PROP> props = new List<ITR_PROP>();

        public ITR_OBJ()
        {
        }

        public ITR_OBJ(BinaryReader reader)
        {
            var objBeginKeyword = CH.ReadITRString(reader);
            if (objBeginKeyword != OBJ_BEGIN)
            {
                throw new FormatException("OBJ-BEGIN keyword expected");
            }

            name = CH.ReadITRString(reader);
            className = CH.ReadITRString(reader);

            var term = "";
            while (term != OBJ_END)
            {
                term = CH.ReadITRString(reader);
                if (term != OBJ_END)
                {
                    reader.BaseStream.Position -= term.Length + 5; // I am sorry
                    var prop = new ITR_PROP(reader);
                    props.Add(prop);
                }
            }
        }
        public override void Write(BinaryWriter writer)
        {
            CH.WriteITRString(writer, OBJ_BEGIN);
            CH.WriteITRString(writer, name);
            CH.WriteITRString(writer, className);
            foreach (var prop in props)
            {
                prop.Write(writer);
            }
            CH.WriteITRString(writer, OBJ_END);
        }

        public override string ToString()
        {
            return $"{name}";
        }

        public ITR_PROP GetProp(string name)
        {
            return props.Where(p => p.name == name).FirstOrDefault();
        }
    }

    public class ITR_PROP : ITR_ITEM
    {
        public const string PROP_BEGIN = "PROP-BEGIN";
        public const string PROP_VALUE = "PROP-VALUE";
        public const string PROP_END = "PROP-END";

        [JsonProperty("propertyName")]
        public string name;
        [JsonProperty("propertyType")]
        public string propType;
        [JsonProperty("propertyClassName")]
        public string className;
        [JsonProperty("propertyValue")]
        public ITR_ITEM value;
        static int cnt = 0;
        public ITR_PROP()
        {
        }

        public ITR_PROP(BinaryReader reader)
        {
            var propBeginKeyword = CH.ReadITRString(reader);
            if (propBeginKeyword != PROP_BEGIN)
            {
                throw new FormatException("PROP_BEGIN keyword expected");
            }

            name = CH.ReadITRString(reader);
            propType = CH.ReadITRString(reader);
            className = CH.ReadITRString(reader);

            if (className == "AActor*")
            {

            }
            else
            {
                var propValueKeyword = CH.ReadITRString(reader);
                if (propValueKeyword != PROP_VALUE)
                {
                    throw new FormatException("PROP_VALUE keyword expected");
                }
                value = CH.MapPropertyName(reader, propType, className);
            }

            var propEndKeyword = CH.ReadITRString(reader);
            if (propEndKeyword != PROP_END)
            {
                throw new FormatException("PROP_END keyword expected");
            }
        }
        public override void Write(BinaryWriter writer)
        {
            CH.WriteITRString(writer, PROP_BEGIN);
            CH.WriteITRString(writer, name);
            CH.WriteITRString(writer, propType);
            CH.WriteITRString(writer, className);
            if (className != "AActor*")
            {
                CH.WriteITRString(writer, PROP_VALUE);
                CH.WriteProperty(writer, value, propType, className);
            }
            CH.WriteITRString(writer, PROP_END);
        }
        public override string ToString()
        {
            return $"{name} ({propType})";
        }
    }
    public class ITR_STRUCT : ITR_ITEM
    {
        public ITR_ITEM value;
        public ITR_STRUCT()
        {

        }
        public ITR_STRUCT(ITR_ITEM value)
        {
            this.value = value;
        }
        public ITR_STRUCT(string className)
        {
            value = CH.CreateClass(className);
        }

        public ITR_STRUCT(BinaryReader reader, string className)
        {
            value = CH.MapClassName(reader, className);
        }
        public override void Write(BinaryWriter writer, string className)
        {
            CH.WriteStructure(writer, value, className);
        }
        public override string ToString()
        {
            return value.ToString();
        }
    }
    public class ITR_ARRAY : ITR_ITEM, ITR_ICollection
    {
        [JsonProperty("elementType")]
        public string propertyType;
        [JsonProperty("elementClass")]
        public string className;

        [JsonProperty("elements")]
        public List<ITR_ITEM> items = new List<ITR_ITEM>();

        public ITR_ARRAY()
        {
        }

        public ITR_ARRAY(BinaryReader reader)
        {
            propertyType = CH.ReadITRString(reader);
            className = CH.ReadITRString(reader);

            var cnt = reader.ReadInt32();
            for (var i = 0; i < cnt; ++i)
            {
                items.Add(CH.MapPropertyName(reader, propertyType, className));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            CH.WriteITRString(writer, propertyType);
            CH.WriteITRString(writer, className);

            writer.Write((Int32)items.Count);
            foreach (var item in items)
            {
                CH.WriteProperty(writer, item, propertyType, className);
            }
        }
        public override string ToString()
        {
            return $"Array<{propertyType}> ({items.Count} entries)";
        }

        public ITR_ITEM Add()
        {
            var item = CH.CreateProp(propertyType, className);
            items.Add(item);
            if (item is ITR_STRUCT)
            {
                return ((ITR_STRUCT)item).value;
            } 
            else
            {
                return item;
            }
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            if (!(clonedItem is ITR_STRUCT) && (propertyType == "StructProperty"))
            {
                items.Add(new ITR_STRUCT(clonedItem));
            } 
            else
            {
                items.Add(clonedItem);
            }
            return clonedItem;
        }
        public void Remove(ITR_ITEM item)
        {
            if (!(item is ITR_STRUCT) && (propertyType == "StructProperty"))
            {
                var itemToRemove = items.Where(s => ((ITR_STRUCT)s).value == item).FirstOrDefault();
                items.Add(itemToRemove);
            }
            else
            {
                items.Remove(item);
            }
        }
        public void Clear()
        {
            items.Clear();
        }
        public string GetItemPropertyType()
        {
            return propertyType;
        }

        public string GetItemClassName()
        {
            return className;
        }
    }
    public class ITR_MAP : ITR_ITEM, ITR_ICollection
    {
        [JsonProperty("keyPropertyType")]
        public string keyPropertyType;
        [JsonProperty("keyClassName")]
        public string keyClass;
        [JsonProperty("valuePropertyType")]
        public string valuePropertyType;
        [JsonProperty("valueClassName")]
        public string valueClass;

        [JsonProperty("keys")]
        public List<ITR_ITEM> keys = new List<ITR_ITEM>();
        [JsonProperty("values")]
        public List<ITR_ITEM> values = new List<ITR_ITEM>();

        public ITR_MAP()
        {
        }

        public ITR_MAP(BinaryReader reader)
        {
            keyPropertyType = CH.ReadITRString(reader);
            keyClass = CH.ReadITRString(reader);
            valuePropertyType = CH.ReadITRString(reader);
            valueClass = CH.ReadITRString(reader);

            var cnt = reader.ReadInt32();
            for (var i = 0; i < cnt; ++i)
            {
                keys.Add(CH.MapClassName(reader, keyClass));
                if (valuePropertyType != "ObjectProperty")
                {
                    values.Add(CH.MapClassName(reader, valueClass));
                }
                else
                {
                    values.Add(CH.MapPropertyName(reader, valuePropertyType, valueClass));
                }
            }
        }
        public override void Write(BinaryWriter writer)
        {
            CH.WriteITRString(writer, keyPropertyType);
            CH.WriteITRString(writer, keyClass);
            CH.WriteITRString(writer, valuePropertyType);
            CH.WriteITRString(writer, valueClass);

            writer.Write((Int32)keys.Count);
            for (var i = 0; i < keys.Count; ++i)
            {
                CH.WriteStructure(writer, keys[i], keyClass);
                if (valuePropertyType != "ObjectProperty")
                {
                    CH.WriteStructure(writer, values[i], valueClass);
                }
                else
                {
                    CH.WriteProperty(writer, values[i], valuePropertyType, valueClass);
                }
            }
        }
        public ITR_ITEM Get(ITR_ITEM key)
        {
            if (!keys.Contains(key))
            {
                return null;
            }
            return values[keys.IndexOf(key)];
        }
        public override string ToString()
        {
            return $"Map<{keyPropertyType}, {valuePropertyType}> ({keys.Count} entries)";
        }
        public ITR_ITEM Add()
        {
            var key = ((ITR_STRUCT)CH.CreateProp(keyPropertyType, keyClass)).value;
            keys.Add(key);
            var value = CH.CreateProp(valuePropertyType, valueClass);
            if (valuePropertyType == "StructProperty")
            {
                value = ((ITR_STRUCT)value).value;
            }
            values.Add(value);
            return key;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedKey = CH.DeepClone(item);
            keys.Add(clonedKey);
            var value = values[keys.IndexOf(item)];
            var clonedValue = CH.DeepClone(value);
            values.Add(clonedValue);
            return clonedKey;
        }

        public void Remove(ITR_ITEM item)
        {
            if (keys.Contains(item))
            {
                var index = keys.IndexOf(item);
                keys.RemoveAt(index);
                values.RemoveAt(index);
            }
        }

        public void Clear()
        {
            keys.Clear();
            values.Clear();
        }

        public string GetItemPropertyType()
        {
            return keyPropertyType;
        }

        public string GetItemClassName()
        {
            return keyClass;
        }
    }
    public class ITR_FTransform : ITR_ITEM
    {
        public ITR_FQuaternion rotation;
        public ITR_FVector position;
        public ITR_FVector scale;

        public ITR_FTransform()
        {
        }

        public ITR_FTransform(BinaryReader reader)
        {
            rotation = new ITR_FQuaternion(reader);
            position = new ITR_FVector(reader);
            scale = new ITR_FVector(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            rotation.Write(writer);
            position.Write(writer);
            scale.Write(writer);
        }

        public override string ToString()
        {
            return $"Position({position}) Scale({scale}) Rotation({rotation})";
        }
    }
    public class ITR_FVector : ITR_ITEM
    {
        public float x;
        public float y;
        public float z;

        public ITR_FVector()
        {
        }

        public ITR_FVector(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
        public override string ToString()
        {
            return $"{x:0.00} {y:0.00} {z:0.00}";
        }
    }
    public class ITR_FQuaternion : ITR_ITEM
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public ITR_FQuaternion()
        {
        }

        public ITR_FQuaternion(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            w = reader.ReadSingle();
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
            writer.Write(w);
        }
        public override string ToString()
        {
            return $"{x:0.00} {y:0.00} {z:0.00} {w:0.00}";
        }
    }
    public class ITR_FTagToTagsList : ITR_ITEM
    {
        public string tag;
        public List<ITR_FString> tags = new List<ITR_FString>();

        public ITR_FTagToTagsList()
        {
        }

        public ITR_FTagToTagsList(BinaryReader reader)
        {
            tag = CH.ReadITRString(reader);
            var cnt = reader.ReadInt32();
            for (var i = 0; i < cnt; ++i)
            {
                tags.Add(CH.ReadITRString(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            CH.WriteITRString(writer, tag);
            writer.Write((Int32)tags.Count);
            foreach (var tag in tags)
            {
                CH.WriteITRString(writer, tag.ToString());
            }
        }

        public override string ToString()
        {
            return tag;
        }
    }
    public class ITR_FTagToItemConfigsList : ITR_ITEM
    {
        public ITR_FString tag;
        public List<ITR_ItemConfig> itemConfigs = new List<ITR_ItemConfig>();

        public ITR_FTagToItemConfigsList()
        {
            tag = new ITR_FString();
        }

        public ITR_FTagToItemConfigsList(BinaryReader reader)
        {
            tag = CH.ReadITRString(reader);
            var cnt = reader.ReadInt32();
            for (var i = 0; i < cnt; ++i)
            {
                itemConfigs.Add(new ITR_ItemConfig(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            tag.Write(writer);
            writer.Write((Int32)itemConfigs.Count);
            foreach (var itemConfig in itemConfigs)
            {
                itemConfig.Write(writer);
            }
        }
        public override string ToString()
        {
            return tag.ToString();
        }
    }
    public class ITR_FAmmoContainerData : ITR_ITEM
    {
        public ITR_PROP ammo;
        public ITR_PROP chamberAmmo;

        public ITR_FAmmoContainerData()
        {
        }

        public ITR_FAmmoContainerData(BinaryReader reader)
        {
            ammo = new ITR_PROP(reader);
            chamberAmmo = new ITR_PROP(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            ammo.Write(writer);
            chamberAmmo.Write(writer);
        }
        public override string ToString()
        {
            return $"Ammo Container";
        }
    }
    public class ITR_ItemConfig : ITR_ITEM
    {
        public ITR_FString itemName;
        public ITR_FInt32 someInt;
        public ITR_FString someString1;
        public ITR_FVector[] someVectors = new ITR_FVector[4];
        public ITR_FString someString2;
        public ITR_FVector someVector;

        public ITR_ItemConfig()
        {
            itemName = new ITR_FString();
            someString1 = new ITR_FString();
            someString2 = new ITR_FString();
        }

        public ITR_ItemConfig(BinaryReader reader)
        {
            itemName = CH.ReadITRString(reader);
            someInt = reader.ReadInt32();
            someString1 = CH.ReadITRString(reader);
            for (var i = 0; i < 4; ++i)
            {
                someVectors[i] = new ITR_FVector(reader);
            }
            someString2 = CH.ReadITRString(reader);
            someVector = new ITR_FVector(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            itemName.Write(writer);
            someInt.Write(writer);
            someString1.Write(writer);
            for (var i = 0; i < 4; ++i)
            {
                someVectors[i].Write(writer);
            }
            someString2.Write(writer);
            someVector.Write(writer);
        }
        public override string ToString()
        {
            return itemName.ToString();
        }
    }
    public class ITR_FNPCDataMap : ITR_ITEM, ITR_ICollection
    {
        public List<ITR_FNPCData> enemies = new List<ITR_FNPCData>();

        public ITR_FNPCDataMap()
        {
        }

        public ITR_FNPCDataMap(BinaryReader reader)
        {
            var amount = reader.ReadInt32();
            for (var i = 0; i < amount; ++i)
            {
                enemies.Add(new ITR_FNPCData(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write((Int32)enemies.Count);
            foreach (var enemy in enemies)
            {
                enemy.Write(writer);
            }
        }
        public override string ToString()
        {
            return $"NPC List ({enemies.Count} entries)";
        }
        public ITR_ITEM Add()
        {
            var item = new ITR_FNPCData();
            enemies.Add((ITR_FNPCData)item);
            return item;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            enemies.Add((ITR_FNPCData)clonedItem);
            return clonedItem;
        }
        public void Remove(ITR_ITEM item)
        {
            enemies.Remove((ITR_FNPCData)item);
        }
        public void Clear()
        {
            enemies.Clear();
        }
        public string GetItemPropertyType()
        {
            return "StructProperty";
        }

        public string GetItemClassName()
        {
            return "FNPCData";
        }
    }
    public class ITR_FNPCData : ITR_ITEM
    {
        public ITR_FString name1;
        public ITR_FString name2;
        public ITR_FString name3;
        public ITR_FTransform transform;
        public ITR_FSingle health;
        public ITR_FSingle[] floats = new ITR_FSingle[4];
        public ITR_FEnum govno;

        public ITR_FNPCData()
        {
            name1 = new ITR_FString();
            name2 = new ITR_FString();
            name3 = new ITR_FString();
        }

        public ITR_FNPCData(BinaryReader reader)
        {
            name1 = CH.ReadITRString(reader);
            name2 = CH.ReadITRString(reader);
            name3 = CH.ReadITRString(reader);
            transform = new ITR_FTransform(reader);
            health = reader.ReadSingle();
            for (var i = 0; i < 4; ++i)
            {
                floats[i] = reader.ReadSingle();
            }
            govno = reader.ReadByte();
        }
        public override void Write(BinaryWriter writer)
        {
            name1.Write(writer);
            name2.Write(writer);
            name3.Write(writer);
            transform.Write(writer);
            health.Write(writer);
            for (var i = 0; i < 4; ++i)
            {
                floats[i].Write(writer);
            }
            govno.Write(writer);
        }
        public override string ToString()
        {
            return name1.ToString();
        }
    }
    public class ITR_FAnomalyFieldData : ITR_ITEM
    {
        public ITR_FAnomanyFieldDataAnomalyList anomalies;
        public ITR_FAnomanyFieldDataArtifactList artifacts;
        public int value;

        public ITR_FAnomalyFieldData()
        {
        }

        public ITR_FAnomalyFieldData(BinaryReader reader)
        {
            anomalies = new ITR_FAnomanyFieldDataAnomalyList(reader);
            artifacts = new ITR_FAnomanyFieldDataArtifactList(reader);
            value = reader.ReadInt32();
            if (value != 0)
            {
                throw new FormatException($"FAnomalyFieldData, value excpected to be 0, got {value}. Possible unknown structure, please, report save to author");
            }
        }
        public override void Write(BinaryWriter writer)
        {
            anomalies.Write(writer);
            artifacts.Write(writer);
            writer.Write(value);
        }
        public override string ToString()
        {
            return $"Anomaly Field";
        }
    }
    public class ITR_FAnomanyFieldDataAnomalyList : ITR_ITEM, ITR_ICollection
    {
        public List<ITR_FAnomaly> anomalies = new List<ITR_FAnomaly>();
        public ITR_FAnomanyFieldDataAnomalyList(BinaryReader reader)
        {
            var anomalyCount = reader.ReadInt32();
            for (var j = 0; j < anomalyCount; ++j)
            {
                anomalies.Add(new ITR_FAnomaly(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write((Int32)anomalies.Count);
            foreach (var anomaly in anomalies)
            {
                anomaly.Write(writer);
            }
        }
        public override string ToString()
        {
            return $"Anomalies";
        }
        public ITR_ITEM Add()
        {
            var item = new ITR_FAnomaly();
            anomalies.Add(item);
            return item;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            anomalies.Add((ITR_FAnomaly)clonedItem);
            return clonedItem;
        }

        public void Remove(ITR_ITEM item)
        {
            anomalies.Remove((ITR_FAnomaly)item);
        }

        public void Clear()
        {
            anomalies.Clear();
        }

        public string GetItemPropertyType()
        {
            return "StructProperty";
        }

        public string GetItemClassName()
        {
            return "FAnomalyData";
        }
    }
    public class ITR_FAnomanyFieldDataArtifactList : ITR_ITEM, ITR_ICollection
    {
        public List<ITR_FArtifact> artifacts = new List<ITR_FArtifact>();
        public ITR_FAnomanyFieldDataArtifactList(BinaryReader reader)
        {
            var artifactCount = reader.ReadInt32();
            for (var j = 0; j < artifactCount; ++j)
            {
                artifacts.Add(new ITR_FArtifact(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write((Int32)artifacts.Count);
            foreach (var artifact in artifacts)
            {
                artifact.Write(writer);
            }
        }
        public override string ToString()
        {
            return $"Artifacts";
        }

        public ITR_ITEM Add()
        {
            var item = new ITR_FArtifact();
            artifacts.Add(item);
            return item;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            artifacts.Add((ITR_FArtifact)clonedItem);
            return clonedItem;
        }

        public void Remove(ITR_ITEM item)
        {
            artifacts.Remove((ITR_FArtifact)item);
        }

        public void Clear()
        {
            artifacts.Clear();
        }

        public string GetItemPropertyType()
        {
            return "StructProperty";
        }

        public string GetItemClassName()
        {
            return "FArtifactData";
        }
    }
    public class ITR_FAnomaly : ITR_ITEM
    {
        public ITR_FString anomalyId;
        public ITR_FString anomalyName;
        public ITR_FTransform transform;
        public ITR_FString anomalyBpName;
        public ITR_FSingle scale;
        public List<ITR_FTransform> transforms = new List<ITR_FTransform>();

        public ITR_FAnomaly()
        {
            anomalyId = new ITR_FString();
            anomalyName = new ITR_FString();
            anomalyBpName = new ITR_FString();
        }

        public ITR_FAnomaly(BinaryReader reader)
        {
            anomalyId = CH.ReadITRString(reader);
            anomalyName = CH.ReadITRString(reader);
            transform = new ITR_FTransform(reader);
            anomalyBpName = CH.ReadITRString(reader);
            scale = reader.ReadSingle();
            var transformCount = reader.ReadInt32();
            for (var i = 0; i < transformCount; ++i)
            {
                transforms.Add(new ITR_FTransform(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            anomalyId.Write(writer);
            anomalyName.Write(writer);
            transform.Write(writer);
            anomalyBpName.Write(writer);
            scale.Write(writer);
            writer.Write((Int32)transforms.Count);
            foreach (var transform in transforms)
            {
                transform.Write(writer);
            }
        }
        public override string ToString()
        {
            return anomalyName.ToString();
        }
    }
    public class ITR_FArtifact : ITR_ITEM
    {
        public ITR_FString artifactId;
        public ITR_FString artifactName;
        public ITR_FTransform transform;

        public ITR_FArtifact()
        {
            artifactId = new ITR_FString();
            artifactName = new ITR_FString();
        }

        public ITR_FArtifact(BinaryReader reader)
        {
            artifactId = CH.ReadITRString(reader);
            artifactName = CH.ReadITRString(reader);
            transform = new ITR_FTransform(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            artifactId.Write(writer);
            artifactName.Write(writer);
            transform.Write(writer);
        }
        public override string ToString()
        {
            return artifactName.ToString();
        }
    }
    public class ITR_FFurnitureData : ITR_ITEM, ITR_ICollection
    {
        public List<ITR_FFurniture> furnitureList = new List<ITR_FFurniture>();

        public ITR_FFurnitureData()
        {
        }

        public ITR_FFurnitureData(BinaryReader reader)
        {
            var amount = reader.ReadInt32();
            for (var i = 0; i < amount; ++i)
            {
                furnitureList.Add(new ITR_FFurniture(reader));
            }

        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write((Int32)furnitureList.Count);
            foreach (var furniture in furnitureList)
            {
                furniture.Write(writer);
            }
        }
        public override string ToString()
        {
            return $"Furniture List ({furnitureList.Count} entries)";
        }
        public ITR_ITEM Add()
        {
            var item = new ITR_FFurniture();
            furnitureList.Add(item);
            return item;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            furnitureList.Add((ITR_FFurniture)clonedItem);
            return clonedItem;
        }

        public void Remove(ITR_ITEM item)
        {
            furnitureList.Remove((ITR_FFurniture)item);
        }

        public void Clear()
        {
            furnitureList.Clear();
        }

        public string GetItemPropertyType()
        {
            return "StructProperty";
        }

        public string GetItemClassName()
        {
            return "FFurniture";
        }
    }
    public class ITR_FFurniture : ITR_ITEM
    {
        public ITR_FString furnitureName;
        public ITR_FTransform transform;
        public ITR_FFurniture()
        {
        }

        public ITR_FFurniture(BinaryReader reader)
        {
            furnitureName = CH.ReadITRString(reader);
            transform = new ITR_FTransform(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            furnitureName.Write(writer);
            transform.Write(writer);
        }
        public override string ToString()
        {
            return furnitureName.ToString();
        }
    }
    public class ITR_FNameArray : ITR_ITEM, ITR_ICollection
    {
        public List<ITR_FString> names = new List<ITR_FString>();

        public ITR_FNameArray()
        {
        }

        public ITR_FNameArray(BinaryReader reader)
        {
            var amount = reader.ReadInt32();
            for (var i = 0; i < amount; ++i)
            {
                names.Add(CH.ReadITRString(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write((Int32)names.Count);
            foreach (var name in names)
            {
                CH.WriteITRString(writer, name.ToString());
            }
        }
        public override string ToString()
        {
            return $"Name Array ({names.Count} entries)";
        }
        public ITR_ITEM Add()
        {
            var item = new ITR_FString();
            names.Add(item);
            return item;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            names.Add((ITR_FString)clonedItem);
            return clonedItem;
        }

        public void Remove(ITR_ITEM item)
        {
            names.Remove((ITR_FString)item);
        }

        public void Clear()
        {
            names.Clear();
        }

        public string GetItemPropertyType()
        {
            return "StructProperty";
        }

        public string GetItemClassName()
        {
            return "FString";
        }
    }
    public class ITR_FSpawnSlotData : ITR_ITEM, ITR_ICollection
    {
        public int value;
        public List<ITR_FSpawnSlot> slots = new List<ITR_FSpawnSlot>();

        public ITR_FSpawnSlotData()
        {
        }

        public ITR_FSpawnSlotData(BinaryReader reader)
        {
            value = reader.ReadInt32();
            var amount = reader.ReadInt32();
            for (var i = 0; i < amount; ++i)
            {
                slots.Add(new ITR_FSpawnSlot(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write(value);
            writer.Write((Int32)slots.Count);
            foreach (var slot in slots)
            {
                slot.Write(writer);
            }
        }
        public override string ToString()
        {
            return $"Spawn Slots ({slots.Count} entries)";
        }
        public ITR_ITEM Add()
        {
            var item = new ITR_FSpawnSlot();
            slots.Add(item);
            return item;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            slots.Add((ITR_FSpawnSlot)clonedItem);
            return clonedItem;
        }

        public void Remove(ITR_ITEM item)
        {
            slots.Remove((ITR_FSpawnSlot)item);
        }

        public void Clear()
        {
            slots.Clear();
        }

        public string GetItemPropertyType()
        {
            return "StructProperty";
        }

        public string GetItemClassName()
        {
            return "FSpawnSlot";
        }
    }
    public class ITR_FSpawnSlot : ITR_ITEM
    {
        public ITR_OBJ obj;
        public ITR_FTransform transform;

        public ITR_FSpawnSlot()
        {
        }

        public ITR_FSpawnSlot(BinaryReader reader)
        {
            obj = new ITR_OBJ(reader);
            transform = new ITR_FTransform(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            obj.Write(writer);
            transform.Write(writer);
        }
        public override string ToString()
        {
            return "Spawn Slot";
        }
    }
    public class ITR_FPlayerStats : ITR_ITEM
    {
        public ITR_PROP deaths;
        public ITR_PROP kills;

        public ITR_FPlayerStats()
        {
        }

        public ITR_FPlayerStats(BinaryReader reader)
        {
            deaths = new ITR_PROP(reader);
            kills = new ITR_PROP(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            deaths.Write(writer);
            kills.Write(writer);
        }
        public override string ToString()
        {
            return "Player Stats";
        }
    }
    public class ITR_FGameplayTagsTuple : ITR_ITEM
    {
        public ITR_FString key;
        public ITR_FString value;

        public ITR_FGameplayTagsTuple()
        {
            key = new ITR_FString();
            value = new ITR_FString();
        }

        public ITR_FGameplayTagsTuple(BinaryReader reader)
        {
            key = CH.ReadITRString(reader);
            value = CH.ReadITRString(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            key.Write(writer);
            value.Write(writer);
        }

        public override string ToString()
        {
            return $"{key} : {value}";
        }
    }
    public class ITR_FGameDifficulty : ITR_ITEM
    {
        public ITR_PROP sleepRestoreHealth;
        public ITR_PROP locationOnMap;
        public ITR_PROP enemySense;
        public ITR_PROP showTracers;
        public ITR_PROP showTips;
        public ITR_PROP itemsDropType;

        public ITR_FGameDifficulty()
        {
        }

        public ITR_FGameDifficulty(BinaryReader reader)
        {
            sleepRestoreHealth = new ITR_PROP(reader);
            locationOnMap = new ITR_PROP(reader);
            enemySense = new ITR_PROP(reader);
            showTracers = new ITR_PROP(reader);
            showTips = new ITR_PROP(reader);
            itemsDropType = new ITR_PROP(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            sleepRestoreHealth.Write(writer);
            locationOnMap.Write(writer);
            enemySense.Write(writer);
            showTracers.Write(writer);
            showTips.Write(writer);
            itemsDropType.Write(writer);
        }
        public override string ToString()
        {
            return "Game Difficulty";
        }
    }
    public class ITR_FMapData : ITR_ITEM
    {
        public byte[] stream;

        public ITR_FMapData()
        {
            stream = new byte[0];
        }

        public ITR_FMapData(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            stream = reader.ReadBytes(length);
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write((Int32)stream.Length);
            writer.Write(stream);
        }
        public override string ToString()
        {
            return "Map Image Data";
        }
    }
    public class ITR_FLevelDecorData : ITR_ITEM
    {
        public ITR_FLevelDecorDataLevelDecorList decorList;
        public ITR_FLevelDecorDataDecorIdList decorIdList;

        public ITR_FLevelDecorData()
        {
        }

        public ITR_FLevelDecorData(BinaryReader reader)
        {
            decorList = new ITR_FLevelDecorDataLevelDecorList(reader);
            decorIdList = new ITR_FLevelDecorDataDecorIdList(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            decorList.Write(writer);
            decorIdList.Write(writer);
        }
        public override string ToString()
        {
            return "Level Decorations";
        }
    }
    public class ITR_FLevelDecorDataLevelDecorList : ITR_ITEM, ITR_ICollection
    {
        public List<ITR_FLevelDecor> decorList = new List<ITR_FLevelDecor>();

        public ITR_FLevelDecorDataLevelDecorList()
        {
        }

        public ITR_FLevelDecorDataLevelDecorList(BinaryReader reader)
        {
            var amount = reader.ReadInt32();
            for (var i = 0; i < amount; ++i)
            {
                decorList.Add(new ITR_FLevelDecor(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write((Int32)decorList.Count);
            foreach (var decor in decorList)
            {
                decor.Write(writer);
            }
        }
        public override string ToString()
        {
            return "Decorations";
        }
        public ITR_ITEM Add()
        {
            var item = new ITR_FLevelDecor();
            decorList.Add(item);
            return item;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            decorList.Add((ITR_FLevelDecor)clonedItem);
            return clonedItem;
        }

        public void Remove(ITR_ITEM item)
        {
            decorList.Remove((ITR_FLevelDecor)item);
        }

        public void Clear()
        {
            decorList.Clear();
        }

        public string GetItemPropertyType()
        {
            return "StructProperty";
        }

        public string GetItemClassName()
        {
            return "FLevelDecor";
        }
    }
    public class ITR_FLevelDecorDataDecorIdList : ITR_ITEM, ITR_ICollection
    {
        public List<ITR_FString> decorIdList = new List<ITR_FString>();

        public ITR_FLevelDecorDataDecorIdList()
        {
        }

        public ITR_FLevelDecorDataDecorIdList(BinaryReader reader)
        {
            var additionalAmount = reader.ReadInt32();
            for (var i = 0; i < additionalAmount; ++i)
            {
                decorIdList.Add(CH.ReadITRString(reader));
            }
        }
        public override void Write(BinaryWriter writer)
        {
            writer.Write((Int32)decorIdList.Count);
            foreach (var id in decorIdList)
            {
                id.Write(writer);
            }
        }
        public override string ToString()
        {
            return "Decoration IDs";
        }
        public ITR_ITEM Add()
        {
            var item = new ITR_FString();
            decorIdList.Add(item);
            return item;
        }
        public ITR_ITEM AddCopy(ITR_ITEM item)
        {
            var clonedItem = CH.DeepClone(item);
            decorIdList.Add((ITR_FString)clonedItem);
            return clonedItem;
        }

        public void Remove(ITR_ITEM item)
        {
            decorIdList.Remove((ITR_FString)item);
        }

        public void Clear()
        {
            decorIdList.Clear();
        }

        public string GetItemPropertyType()
        {
            return "StructProperty";
        }

        public string GetItemClassName()
        {
            return "FString";
        }
    }
    public class ITR_FLevelDecor : ITR_ITEM
    {
        public ITR_FString name;
        public ITR_FTransform transform;
        public ITR_FLevelDecor()
        {
            name = new ITR_FString();
        }

        public ITR_FLevelDecor(BinaryReader reader)
        {
            name = CH.ReadITRString(reader);
            transform = new ITR_FTransform(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            name.Write(writer);
            transform.Write(writer);
        }
        public override string ToString()
        {
            return name.ToString();
        }
    }
    public class ITR_FString : ITR_ITEM
    {
        public string str = "default";

        public ITR_FString(BinaryReader reader)
        {
            str = CH.ReadITRString(reader);
        }
        public ITR_FString(string other)
        {
            str = other;
        }

        public ITR_FString()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            CH.WriteITRString(writer, str);
        }

        public override string ToString()
        {
            return str;
        }
        public static implicit operator ITR_FString(string input)
        {
            return new ITR_FString(input);
        }
    }
    public class ITR_FInt32 : ITR_ITEM
    {
        public Int32 value;

        public ITR_FInt32(BinaryReader reader)
        {
            value = reader.ReadInt32();
        }
        public ITR_FInt32(Int32 other)
        {
            value = other;
        }

        public ITR_FInt32()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public override string ToString()
        {
            return value.ToString();
        }
        public static implicit operator ITR_FInt32(Int32 input)
        {
            return new ITR_FInt32(input);
        }
    }
    public class ITR_FEnum : ITR_ITEM
    {
        public Byte value;

        public ITR_FEnum(BinaryReader reader)
        {
            value = reader.ReadByte();
        }
        public ITR_FEnum(Byte other)
        {
            value = other;
        }

        public ITR_FEnum()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public override string ToString()
        {
            return value.ToString();
        }
        public static implicit operator ITR_FEnum(Byte input)
        {
            return new ITR_FEnum(input);
        }
    }
    public class ITR_FUInt32 : ITR_ITEM
    {
        public UInt32 value;

        public ITR_FUInt32(BinaryReader reader)
        {
            value = reader.ReadUInt32();
        }
        public ITR_FUInt32(UInt32 other)
        {
            value = other;
        }

        public ITR_FUInt32()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public override string ToString()
        {
            return value.ToString();
        }
        public static implicit operator ITR_FUInt32(UInt32 input)
        {
            return new ITR_FUInt32(input);
        }
    }
    public class ITR_FInt64 : ITR_ITEM
    {
        public Int64 value;

        public ITR_FInt64(BinaryReader reader)
        {
            value = reader.ReadInt64();
        }
        public ITR_FInt64(Int64 other)
        {
            value = other;
        }

        public ITR_FInt64()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public override string ToString()
        {
            return value.ToString();
        }
        public static implicit operator ITR_FInt64(Int64 input)
        {
            return new ITR_FInt64(input);
        }
    }
    public class ITR_FUInt64 : ITR_ITEM
    {
        public UInt64 value;

        public ITR_FUInt64(BinaryReader reader)
        {
            value = reader.ReadUInt64();
        }
        public ITR_FUInt64(UInt64 other)
        {
            value = other;
        }

        public ITR_FUInt64()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public override string ToString()
        {
            return value.ToString();
        }
        public static implicit operator ITR_FUInt64(UInt64 input)
        {
            return new ITR_FUInt64(input);
        }
    }
    public class ITR_FBool : ITR_ITEM
    {
        public bool value;

        public ITR_FBool(BinaryReader reader)
        {
            value = (reader.ReadUInt32() != 0);
        }
        public ITR_FBool(bool other)
        {
            value = other;
        }

        public ITR_FBool()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            if (value)
            {
                writer.Write((Int32)1);
            }
            else
            {
                writer.Write((Int32)0);
            }
        }

        public override string ToString()
        {
            return value.ToString();
        }
        public static implicit operator ITR_FBool(bool input)
        {
            return new ITR_FBool(input);
        }
    }
    public class ITR_FSingle : ITR_ITEM
    {
        public Single value;

        public ITR_FSingle(BinaryReader reader)
        {
            value = reader.ReadSingle();
        }
        public ITR_FSingle(Single other)
        {
            value = other;
        }

        public ITR_FSingle()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public override string ToString()
        {
            return value.ToString();
        }
        public static implicit operator ITR_FSingle(Single input)
        {
            return new ITR_FSingle(input);
        }
    }
    public class ITR_FDateTime : ITR_ITEM
    {
        public DateTime value;

        public ITR_FDateTime(BinaryReader reader)
        {
            value = DateTime.FromBinary(reader.ReadInt64());
        }
        public ITR_FDateTime(DateTime other)
        {
            value = other;
        }
        public ITR_FDateTime(Int64 other)
        {
            value = DateTime.FromBinary(other);
        }

        public ITR_FDateTime()
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(value.ToBinary());
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    public static class CH //Crap Helper
    {
        public static Dictionary<String, Type> classToTypeMapping = new Dictionary<string, Type>()
        {
            {"FGameplayTag", typeof(ITR_FString)},
            {"FTransform", typeof(ITR_FTransform)},
            {"FVector", typeof(ITR_FVector)},
            {"FQuaternion", typeof(ITR_FQuaternion)},
            {"FTagToTagsList", typeof(ITR_FTagToTagsList)},
            {"FTagToItemConfigsList", typeof(ITR_FTagToItemConfigsList)},
            {"FAmmoContainerData", typeof(ITR_FAmmoContainerData)},
            {"FStringToTagsList", typeof(ITR_FTagToTagsList)},
            {"FString", typeof(ITR_FString)},
            {"FNPCDataMap", typeof(ITR_FNPCDataMap) },
            {"FNPCData", typeof(ITR_FNPCData) },
            {"FGamepFNPCDataMaplayTag", typeof(ITR_FNPCDataMap)},
            {"FAnomalyFieldData", typeof(ITR_FAnomalyFieldData)},
            {"FAnomalyData", typeof(ITR_FAnomaly)},
            {"FFurnitureData", typeof(ITR_FFurnitureData)},
            {"FNameArray", typeof(ITR_FNameArray)},
            {"FSpawnSlotData", typeof(ITR_FSpawnSlotData)},
            {"int32", typeof(ITR_FInt32)},
            {"float", typeof(ITR_FSingle)},
            {"FDateTime", typeof(ITR_FDateTime)},
            {"FPlayerStats", typeof(ITR_FPlayerStats)},
            {"FGameplayTagsTuple", typeof(ITR_FGameplayTagsTuple)},
            {"FGameDifficulty", typeof(ITR_FGameDifficulty)},
            {"FMapData", typeof(ITR_FMapData)},
            {"FLevelDecorData", typeof(ITR_FLevelDecorData)}
        };
        public static Dictionary<String, Type> propertyToTypeMapping = new Dictionary<string, Type>()
        {
            {"ObjectProperty", typeof(ITR_OBJ)},
            {"FloatProperty", typeof(ITR_FSingle)},
            {"IntProperty", typeof(ITR_FInt32)},
            {"StructProperty", typeof(ITR_STRUCT)},
            {"MapProperty", typeof(ITR_MAP)},
            {"ArrayProperty", typeof(ITR_ARRAY)},
            {"StrProperty", typeof(ITR_FString)},
            {"BoolProperty", typeof(ITR_FBool)},
            {"EnumProperty", typeof(ITR_FEnum)},
            {"Int64Property", typeof(ITR_FInt64)},
            {"UInt64Property", typeof(ITR_FUInt64)}
        };
        public static string ReadITRString(BinaryReader reader)
        {
            var isUnicode = false;
            var length = reader.ReadInt32();
            if (length == 0)
            {
                return "";
            } else if (length < 0)
            {
                isUnicode = true;
                length = -length;
            }
            string str;
            if (!isUnicode) {
                str = new string(reader.ReadChars(length - 1));
                reader.ReadByte();
            } else
            {
                byte[] stream = reader.ReadBytes(length * 2 - 2);
                str = System.Text.Encoding.Unicode.GetString(stream);
                reader.ReadByte();
                reader.ReadByte();
            }
            return str;
        }
        public static bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;

            return input.Any(c => c > MaxAnsiCode);
        }
        public static void WriteITRString(BinaryWriter writer, string str)
        {
            if (str.Length == 0)
            {
                writer.Write((Int32)0);
                return;
            }
            if (ContainsUnicodeCharacter(str))
            {
                Int32 length = -(str.Length + 1);
                writer.Write(length);
                var charArray = str.ToCharArray();
                var shortArray = charArray.Select(c => (Int16)c).ToArray();
                foreach (var shortChar in shortArray)
                {
                    writer.Write(shortChar);
                }
                writer.Write('\0');
                writer.Write('\0');
            } 
            else
            {
                Int32 length = str.Length + 1;
                writer.Write(length);
                writer.Write(str.ToCharArray());
                writer.Write('\0');
            }
            return;
        }
        public static ITR_ITEM CreateClass(string className)
        {
            if (classToTypeMapping.ContainsKey(className))
            {
                return (ITR_ITEM)Activator.CreateInstance(classToTypeMapping[className]);
            }
            else
            {
                throw new KeyNotFoundException($"Unknown class name {className}");
            }
        }
        public static ITR_ITEM CreateProp(string propType, string className)
        {
            if (propertyToTypeMapping.ContainsKey(propType))
            {
                if (className == "AActor*")
                {
                    return null;
                }
                else if (propType == "StructProperty")
                {
                    return new ITR_STRUCT(className);
                }
                else
                {
                    return (ITR_ITEM)Activator.CreateInstance(propertyToTypeMapping[propType]);
                }
            }
            else
            {
                throw new KeyNotFoundException($"Unknown class name {className}");
            }
        }
        public static ITR_ITEM MapClassName(BinaryReader reader, string className)
        {
            if (classToTypeMapping.ContainsKey(className))
            {
                return (ITR_ITEM)Activator.CreateInstance(classToTypeMapping[className], reader);
            }
            else
            {
                throw new KeyNotFoundException($"Unknown class name {className}");
            }   
        }
        public static ITR_ITEM MapPropertyName(BinaryReader reader, string propType, string className)
        {
            if (propertyToTypeMapping.ContainsKey(propType))
            {
                if (className == "AActor*")
                {
                    return null;
                } else if (propType == "StructProperty")
                {
                    return new ITR_STRUCT(reader, className);
                } else
                {
                    return (ITR_ITEM)Activator.CreateInstance(propertyToTypeMapping[propType], reader);
                }
            }
            else
            {
                throw new KeyNotFoundException($"Unknown class name {className}");
            }
        }
        public static void WriteStructure(BinaryWriter writer, object value, string className)
        {
            switch (className)
            {
                case "FGameplayTag":
                    ((ITR_FString)value).Write(writer);
                    break;
                case "FTransform":
                    ((ITR_FTransform)value).Write(writer);
                    break;
                case "FVector":
                    ((ITR_FVector)value).Write(writer);
                    break;
                case "FQuaternion":
                    ((ITR_FQuaternion)value).Write(writer);
                    break;
                case "FTagToTagsList":
                    ((ITR_FTagToTagsList)value).Write(writer);
                    break;
                case "FTagToItemConfigsList":
                    ((ITR_FTagToItemConfigsList)value).Write(writer);
                    break;
                case "FAmmoContainerData":
                    ((ITR_FAmmoContainerData)value).Write(writer);
                    break;
                case "FStringToTagsList":
                    ((ITR_FTagToTagsList)value).Write(writer);
                    break;
                case "FString":
                    ((ITR_FString)value).Write(writer);
                    break;
                case "FNPCDataMap":
                    ((ITR_FNPCDataMap)value).Write(writer);
                    break;
                case "FAnomalyFieldData":
                    ((ITR_FAnomalyFieldData)value).Write(writer);
                    break;
                case "FAnomalyData":
                    ((ITR_FAnomaly)value).Write(writer);
                    break;
                case "FFurnitureData":
                    ((ITR_FFurnitureData)value).Write(writer);
                    break;
                case "FNameArray":
                    ((ITR_FNameArray)value).Write(writer);
                    break;
                case "FSpawnSlotData":
                    ((ITR_FSpawnSlotData)value).Write(writer);
                    break;
                case "int32":
                    ((ITR_FInt32)value).Write(writer);
                    break;
                case "float":
                    ((ITR_FSingle)value).Write(writer);
                    break;
                case "FDateTime":
                    ((ITR_FDateTime)value).Write(writer);
                    break;
                case "FPlayerStats":
                    ((ITR_FPlayerStats)value).Write(writer);
                    break;
                case "FGameplayTagsTuple":
                    ((ITR_FGameplayTagsTuple)value).Write(writer);
                    break;
                case "FGameDifficulty":
                    ((ITR_FGameDifficulty)value).Write(writer);
                    break;
                case "FMapData":
                    ((ITR_FMapData)value).Write(writer);
                    break;
                case "FLevelDecorData":
                    ((ITR_FLevelDecorData)value).Write(writer);
                    break;
                default:
                    throw new KeyNotFoundException($"Unknown class name {className}");
            }
        }
        public static void WriteProperty(BinaryWriter writer, object value, string propType, string className)
        {
            switch (propType)
            {
                case "ObjectProperty":
                    if (className == "AActor*")
                    {
                        return;
                    }
                    ((ITR_OBJ)value).Write(writer);
                    break;
                case "FloatProperty":
                    ((ITR_FSingle)value).Write(writer);
                    break;
                case "IntProperty":
                    ((ITR_FInt32)value).Write(writer);
                    break;
                case "StructProperty":
                    ((ITR_STRUCT)value).Write(writer, className);
                    break;
                case "MapProperty":
                    ((ITR_MAP)value).Write(writer);
                    break;
                case "ArrayProperty":
                    ((ITR_ARRAY)value).Write(writer);
                    break;
                case "StrProperty":
                    ((ITR_FString)value).Write(writer);
                    break;
                case "BoolProperty":
                    ((ITR_FBool)value).Write(writer);
                    break;
                case "EnumProperty":
                    ((ITR_FEnum)value).Write(writer);
                    break;
                case "Int64Property":
                    ((ITR_FInt64)value).Write(writer);
                    break;
                case "UInt64Property":
                    ((ITR_FUInt64)value).Write(writer);
                    break;
                default:
                    throw new KeyNotFoundException($"Unknown property type {propType}");
            }
        }

        public static ITR_ITEM DeepClone(ITR_ITEM obj)
        {
            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace, TypeNameHandling = TypeNameHandling.All };
            return JsonConvert.DeserializeObject<ITR_ITEM>(JsonConvert.SerializeObject(obj, settings), settings);
        }
    }
}


