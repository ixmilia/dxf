// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace IxMilia.Dxf.Generator
{
    public abstract class GeneratorBase
    {
        public const string Copyright = "Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.";

        private StringBuilder _sb;
        private int _indentionLevel;

        public string Accessibility(XElement property)
        {
            var att = property.Attribute("Accessibility");
            var accessibility = att == null ? "public" : att.Value;
            if (accessibility == "private" && !Name(property).StartsWith("_"))
            {
                throw new Exception(string.Format("Improperly named private property '{0}'.  Missing leading underscore.", Name(property)));
            }

            return accessibility;
        }

        public bool AllowMultiples(XElement property)
        {
            var att = property.Attribute("AllowMultiples");
            return att != null && bool.Parse(att.Value);
        }

        public string AttributeOrDefault(XElement xml, string attributeName, string defaultValue = null)
        {
            var att = xml.Attribute(attributeName);
            return att == null ? defaultValue : att.Value;
        }

        public string BaseClass(XElement entity, string defaultValue)
        {
            var att = entity.Attribute("BaseClass");
            return att == null ? defaultValue : att.Value;
        }

        public string CamlCase(string value)
        {
            return char.ToLower(value[0]) + value.Substring(1);
        }

        public int Code(XElement property)
        {
            return int.Parse(property.Attribute("Code").Value);
        }

        public string Comment(XElement xml)
        {
            return AttributeOrDefault(xml, "Comment");
        }

        public string CopyConstructor(XElement entity)
        {
            var att = entity.Attribute("CopyConstructor");
            return att == null || att.Value == "None" ? null : CamlCase(att.Value);
        }

        public string DefaultConstructor(XElement entity)
        {
            var att = entity.Attribute("DefaultConstructor");
            if (att == null)
            {
                return "public";
            }

            return att == null || att.Value == "None" ? null : CamlCase(att.Value);
        }

        public string DefaultValue(XElement property)
        {
            var value = property.Attribute("DefaultValue").Value;
            if (property.Attribute("Type").Value == "string" && value != "null")
            {
                value = string.Format("\"{0}\"", value);
            }

            return value;
        }

        public bool DisableWritingDefault(XElement property)
        {
            var att = property.Attribute("DisableWritingDefault");
            return att != null && bool.Parse(att.Value);
        }

        public string EntityType(XElement entity)
        {
            return entity.Attribute("EntityType").Value;
        }

        public bool GenerateReaderFunction(XElement entity)
        {
            return bool.Parse(AttributeOrDefault(entity, "GenerateReaderFunction", "true"));
        }

        public int[] GetCodeOverrides(XElement property)
        {
            var codesAtt = property.Attribute("CodeOverrides");
            return codesAtt == null ? null : codesAtt.Value.Split(',').Select(c => int.Parse(c)).ToArray();
        }

        public IEnumerable<XElement> GetPointers(XElement entity)
        {
            return entity.Elements(XName.Get("Pointer", entity.Name.NamespaceName));
        }

        public IEnumerable<XElement> GetProperties(XElement entity)
        {
            return entity.Elements(XName.Get("Property", entity.Name.NamespaceName));
        }

        public IEnumerable<XElement> GetPropertiesAndPointers(XElement entity)
        {
            return entity.Elements().Where(e => e.Name.LocalName == "Property" || e.Name.LocalName == "Pointer");
        }

        public IEnumerable<string> GetPropertyWriteLines(XElement property)
        {
            var lines = new List<string>();
            var code = Code(property);
            var codes = GetCodeOverrides(property);
            var name = Name(property);
            var writeCondition = WriteCondition(property);
            var minVersion = MinVersion(property);
            var maxVersion = MaxVersion(property);
            var writePredicates = new List<string>();
            if (writeCondition != null)
            {
                writePredicates.Add(writeCondition);
            }

            if (minVersion != null)
            {
                writePredicates.Add(string.Format("version >= DxfAcadVersion.{0}", minVersion));
            }

            if (maxVersion != null)
            {
                writePredicates.Add(string.Format("version <= DxfAcadVersion.{0}", maxVersion));
            }

            if (DisableWritingDefault(property))
            {
                writePredicates.Add(string.Format("this.{0} != {1}", name, DefaultValue(property)));
            }

            var indentPrefix = string.Empty;
            if (writePredicates.Any())
            {
                lines.Add(string.Format("if ({0})", string.Join(" && ", writePredicates)));
                lines.Add("{");
                indentPrefix = "    ";
            }

            if (codes != null)
            {
                var suffix = 'X';
                for (int i = 0; i < codes.Length; i++, suffix++)
                {
                    lines.Add(string.Format("{0}pairs.Add(new DxfCodePair({1}, {2}?.{3} ?? default(double)));", indentPrefix, codes[i], name, suffix));
                    // currently all multi-value codes are doubles, so this is ok:         ^^^^^^^^^^^^^^^
                }
            }
            else
            {
                if (AllowMultiples(property))
                {
                    var writeConverter = WriteConverter(property);
                    var value = string.IsNullOrEmpty(writeConverter)
                        ? "p"
                        : string.Format("{0}(p)", writeConverter);
                    if (IsPointer(property))
                    {
                        name += "Pointers.Pointers";
                        value = "DxfCommonConverters.UIntHandle(p.Handle)";
                    }
                    lines.Add(string.Format("{0}pairs.AddRange(this.{1}.Select(p => new DxfCodePair({2}, {3})));", indentPrefix, name, code, value));
                }
                else
                {
                    if (IsPointer(property))
                    {
                        name += "Pointer.Handle";
                    }
                    lines.Add(string.Format("{0}pairs.Add(new DxfCodePair({1}, {2}(this.{3})));", indentPrefix, code, WriteConverter(property), name));
                }
            }

            if (writePredicates.Any())
            {
                lines.Add("}");
                lines.Add(string.Empty);
            }

            return lines;
        }

        public List<string> GetWriteCommands(XElement entity)
        {
            var att = entity.Element(XName.Get("WriteOrder", entity.Name.NamespaceName));
            var lines = new List<string>();
            if (att == null)
            {
                // default order
                if (!string.IsNullOrEmpty(SubclassMarker(entity)))
                {
                    lines.Add(string.Format("pairs.Add(new DxfCodePair(100, \"{0}\"));", SubclassMarker(entity)));
                }

                foreach (var property in GetPropertiesAndPointers(entity))
                {
                    lines.AddRange(GetPropertyWriteLines(property));
                }
            }
            else
            {
                // specific order
                foreach (var spec in att.Elements())
                {
                    lines.AddRange(WriteValue(spec, entity));
                }
            }

            return lines;
        }

        public bool HasXData(XElement xml)
        {
            return bool.Parse(AttributeOrDefault(xml, "HasXData", "false"));
        }

        public bool IsPointer(XElement entity)
        {
            return entity.Name.LocalName == "Pointer";
        }

        public int Mask(XElement flag)
        {
            return int.Parse(flag.Attribute("Mask").Value);
        }

        public string MaxVersion(XElement property)
        {
            var att = property.Attribute("MaxVersion");
            return att == null ? null : att.Value;
        }

        public string MinVersion(XElement property)
        {
            var att = property.Attribute("MinVersion");
            return att == null ? null : att.Value;
        }

        public string Name(XElement property)
        {
            return property.Attribute("Name").Value;
        }

        public string Property(XElement flag)
        {
            return flag.Attribute("Property").Value;
        }

        public bool ProtectedSet(XElement property)
        {
            var att = property.Attribute("ProtectedSet");
            return att == null ? false : bool.Parse(att.Value);
        }

        public string ReadConverter(XElement property)
        {
            if (IsPointer(property))
            {
                return "DxfCommonConverters.UIntHandle";
            }
            else
            {
                var att = property.Attribute("ReadConverter");
                return att == null ? string.Empty : att.Value;
            }
        }

        public string SetterAccessibility(XElement property)
        {
            if (AllowMultiples(property) && Accessibility(property) != "private")
                return "private ";
            return ProtectedSet(property) ? "protected " : "";
        }

        public string SubclassMarker(XElement entity)
        {
            var value = entity.Attribute("SubclassMarker").Value;
            return value == "null" ? null : value;
        }

        public string Tag(XElement entity)
        {
            var att = entity.Attribute("Tag");
            return att == null ? null : att.Value;
        }

        public string Type(XElement property)
        {
            return property.Attribute("Type").Value;
        }

        public string TypeString(XElement entity)
        {
            var typeString = entity.Attribute("TypeString").Value;
            if (typeString.Any(c => char.IsLower(c)))
            {
                throw new InvalidOperationException(string.Format("TypeString value '{0}' must be all upper case.", typeString));
            }

            return typeString;
        }

        public string TypeToString(Type type)
        {
            string expected;
            if (type == typeof(string)) expected = "String";
            else if (type == typeof(double)) expected = "Double";
            else if (type == typeof(short)) expected = "Short";
            else if (type == typeof(int)) expected = "Integer";
            else if (type == typeof(long)) expected = "Long";
            else if (type == typeof(bool)) expected = "Bool";
            else throw new Exception("Unsupported code pair type");
            // TODO: handle

            return expected + "Value";
        }

        public string WriteCondition(XElement property)
        {
            var att = property.Attribute("WriteCondition");
            return att == null ? null : att.Value;
        }

        public string WriteConverter(XElement property)
        {
            if (IsPointer(property))
            {
                return "DxfCommonConverters.UIntHandle";
            }
            else
            {
                var att = property.Attribute("WriteConverter");
                return att == null ? string.Empty : att.Value;
            }
        }

        public IEnumerable<string> WriteProperty(XElement spec, XElement entity)
        {
            var property = GetPropertiesAndPointers(entity).Single(p => Name(p) == spec.Attribute("Property").Value);
            return GetPropertyWriteLines(property);
        }

        public IEnumerable<string> WriteSpecificValue(XElement spec)
        {
            var code = spec.Attribute("Code").Value;
            var value = spec.Attribute("Value").Value;
            var originalValue = value;
            var writeConverter = WriteConverter(spec);
            if (!string.IsNullOrEmpty(writeConverter))
            {
                value = string.Format("{0}({1})", writeConverter, value);
            }

            var line = string.Format("pairs.Add(new DxfCodePair({0}, {1}));", code, value);
            var minVersion = MinVersion(spec);
            var maxVersion = MaxVersion(spec);
            var defaultValue = spec.Attribute("DontWriteIfValueIs");
            var condition = WriteCondition(spec);
            var predicate = new List<string>();
            if (minVersion != null || maxVersion != null)
            {
                if (minVersion == maxVersion)
                {
                    predicate.Add("version == DxfAcadVersion." + minVersion);
                }
                else
                {
                    if (minVersion != null)
                    {
                        predicate.Add("version >= DxfAcadVersion." + minVersion);
                    }
                    if (maxVersion != null)
                    {
                        predicate.Add("version <= DxfAcadVersion." + maxVersion);
                    }
                }
            }

            if (defaultValue != null)
            {
                predicate.Add(string.Format("{0} != {1}", originalValue, defaultValue.Value));
            }

            if (condition != null)
            {
                predicate.Add(condition);
            }

            if (predicate.Any())
            {
                yield return string.Format("if ({0})", string.Join(" && ", predicate));
                yield return "{";
                yield return "    " + line;
                yield return "}";
            }
            else
            {
                yield return line;
            }
        }

        public IEnumerable<string> WriteValue(XElement spec, XElement entity)
        {
            switch (spec.Name.LocalName)
            {
                case "WriteSpecificValue":
                    return WriteSpecificValue(spec);
                case "WriteProperty":
                    return WriteProperty(spec, entity);
                case "Foreach":
                    var property = spec.Attribute("Property").Value;
                    var lines = new List<string>();
                    lines.Add(string.Format("foreach (var item in {0})", property));
                    lines.Add("{");
                    lines.AddRange(spec.Elements().SelectMany(e => WriteValue(e, entity)).Select(l => "    " + l));
                    lines.Add("}");
                    lines.Add("\n");
                    return lines;
                case "WriteExtensionData":
                    return new[] { "AddExtensionValuePairs(pairs, version, outputHandles);" };
                default:
                    throw new NotSupportedException();
            }
        }

        public void CreateNewFile(string ns)
        {
            if (_sb != null)
            {
                throw new Exception($"File is still being written.  Please call `{nameof(FinishFile)}`() first.");
            }

            _sb = new StringBuilder();
            _indentionLevel = 0;
            AppendLine($"// {Copyright}");
            AppendLine();
            AppendLine("// The contents of this file are automatically generated by a tool, and should not be directly modified.");
            AppendLine();
            AppendLine("using System;");
            AppendLine("using System.Collections.Generic;");
            AppendLine("using System.Linq;");
            AppendLine("using IxMilia.Dxf.Collections;");
            AppendLine();
            AppendLine($"namespace {ns}");
            AppendLine("{");
        }

        public void IncreaseIndent()
        {
            _indentionLevel++;
        }

        public void DecreaseIndent()
        {
            _indentionLevel--;
        }

        public void AppendLine()
        {
            _sb.AppendLine();
        }

        public void AppendLine(string line)
        {
            var indention = new string(' ', _indentionLevel * 4);
            _sb.Append(indention);
            _sb.AppendLine(line);
        }

        public void FinishFile()
        {
            AppendLine("}");
        }

        public void WriteFile(string path)
        {
            File.WriteAllText(path, _sb.ToString());
            _sb = null;
        }
    }
}
