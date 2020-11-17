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

        public void AppendAddValuePairsMethod(XElement item)
        {
            if (GenerateWriterFunction(item))
            {
                AppendLine();
                AppendLine("protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, bool writeXData)");
                AppendLine("{");
                IncreaseIndent();
                AppendLine("base.AddValuePairs(pairs, version, outputHandles, writeXData: false);");
                foreach (var line in GetWriteCommands(item))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        AppendLine();
                    }
                    else
                    {
                        AppendLine(line);
                    }
                }

                AppendLine("if (writeXData)");
                AppendLine("{");
                AppendLine("    DxfXData.AddValuePairs(XData, pairs, version, outputHandles);");
                AppendLine("}");

                DecreaseIndent();
                AppendLine("}");
            }
        }

        public void AppendCopyConstructor(XElement item)
        {
            var copyConstructorAccessibility = CopyConstructor(item);
            if (copyConstructorAccessibility != null)
            {
                AppendLine();
                if (copyConstructorAccessibility == "inherited")
                {
                    AppendLine($"internal {Name(item)}({BaseClass(item, "baseClass")} other)");
                    AppendLine("    : base(other)");
                    AppendLine("{");
                }
                else
                {
                    AppendLine($"{copyConstructorAccessibility} {Name(item)}({Name(item)} other)");
                    AppendLine("    : base(other)");
                    AppendLine("{");
                    IncreaseIndent();
                    AppendLine("CopyManualValues(other);");
                    AppendLine("other.XData.CopyItemsTo(this.XData);");
                    foreach (var property in GetPropertiesAndPointers(item))
                    {
                        var name = Name(property);
                        if (IsPointer(property))
                        {
                            name += "Pointer";
                            AppendLine($"this.{name}.Handle = other.{name}.Handle;");
                            AppendLine($"this.{name}.Item = other.{name}.Item;");
                        }
                        else
                        {
                            AppendLine($"this.{name} = other.{name};");
                        }
                    }

                    DecreaseIndent();
                }

                AppendLine("}"); // end method
            }
        }

        public void AppendDefaultConstructor(XElement item)
        {
            var defaultConstructorType = DefaultConstructor(item);
            if (defaultConstructorType != null)
            {
                AppendLine();
                AppendLine($"{defaultConstructorType} {Name(item)}()");
                AppendLine("    : base()");
                AppendLine("{");
                AppendLine("}");
            }
        }

        public void AppendFlags(XElement item)
        {
            foreach (var property in GetProperties(item))
            {
                var flags = property.Elements(XName.Get("Flag", item.Name.NamespaceName));
                if (flags.Any())
                {
                    AppendLine();
                    AppendLine($"// {Name(property)} flags");
                    foreach (var flag in flags)
                    {
                        AppendLine();
                        AppendLine($"public bool {Name(flag)}");
                        AppendLine("{");
                        AppendLine($"    get {{ return DxfHelpers.GetFlag({Name(property)}, {Mask(flag)}); }}");
                        AppendLine("    set");
                        AppendLine("    {");
                        AppendLine($"        var flags = {Name(property)};");
                        AppendLine($"        DxfHelpers.SetFlag(value, ref flags, {Mask(flag)});");
                        AppendLine($"        {Name(property)} = flags;");
                        AppendLine("    }");
                        AppendLine("}");
                    }
                }
            }
        }

        public void AppendInitializeMethod(XElement item, string customInitializeLine = null)
        {
            AppendLine();
            AppendLine("protected override void Initialize()");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("base.Initialize();");
            if (customInitializeLine != null)
            {
                AppendLine(customInitializeLine);
            }

            var seenProperties = new HashSet<string>();
            foreach (var property in GetProperties(item))
            {
                var propertyName = Name(property);
                if (seenProperties.Contains(propertyName))
                {
                    continue;
                }

                seenProperties.Add(propertyName);
                var defaultValue = AllowMultiples(property)
                    ? string.Format("new ListNonNull<{0}>()", Type(property))
                    : DefaultValue(property);
                AppendLine($"this.{propertyName} = {defaultValue};");
            }

            DecreaseIndent();
            AppendLine("}");
        }

        public void AppendMinAndMaxVersions(XElement item)
        {
            // min and max supported versions
            var minVersion = MinVersion(item);
            if (minVersion != null)
            {
                AppendLine($"protected override DxfAcadVersion MinVersion {{ get {{ return DxfAcadVersion.{minVersion}; }} }}");
            }

            var maxVersion = MaxVersion(item);
            if (maxVersion != null)
            {
                AppendLine($"protected override DxfAcadVersion MaxVersion {{ get {{ return DxfAcadVersion.{maxVersion}; }} }}");
            }
        }

        public void AppendParameterizedConstructors(XElement item)
        {
            var constructors = item.Elements(XName.Get("Constructor", item.Name.NamespaceName));
            if (constructors.Any())
            {
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.Elements(XName.Get("ConstructorParameter", item.Name.NamespaceName));
                    var argList = new List<string>();
                    foreach (var parameter in parameters)
                    {
                        var paramName = CamlCase(Property(parameter));
                        var paramType = Type(parameter);
                        argList.Add(paramType + " " + paramName);
                    }

                    var sig = string.Join(", ", argList);
                    AppendLine();
                    AppendLine($"public {Name(item)}({sig})");
                    AppendLine("    : this()");
                    AppendLine("{");
                    IncreaseIndent();

                    foreach (var parameter in parameters)
                    {
                        AppendLine($"this.{Property(parameter)} = {CamlCase(Property(parameter))};");
                    }

                    DecreaseIndent();
                    AppendLine("}"); // end constructor
                }
            }
        }

        public void AppendPointers(XElement item)
        {
            var pointers = GetPointers(item);
            if (pointers.Any())
            {
                AppendLine();
                AppendLine("IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()");
                AppendLine("{");
                foreach (var pointer in pointers)
                {
                    if (AllowMultiples(pointer))
                    {
                        AppendLine($"    foreach (var pointer in {Name(pointer)}Pointers.Pointers)");
                        AppendLine("    {");
                        AppendLine("        yield return pointer;");
                        AppendLine("    }");
                    }
                    else
                    {
                        AppendLine($"    yield return {Name(pointer)}Pointer;");
                    }
                }

                AppendLine("}"); // end method

                AppendLine();
                AppendLine("IEnumerable<IDxfItemInternal> IDxfItemInternal.GetChildItems()");
                AppendLine("{");
                AppendLine("    return ((IDxfItemInternal)this).GetPointers().Select(p => (IDxfItemInternal)p.Item);");
                AppendLine("}");
                AppendLine();

                foreach (var pointer in pointers)
                {
                    var defaultValue = "new DxfPointer()";
                    var typeString = "DxfPointer";
                    var suffix = "Pointer";
                    if (AllowMultiples(pointer))
                    {
                        var type = Type(pointer);
                        defaultValue = string.Format("new DxfPointerList<{0}>()", type);
                        typeString = string.Format("DxfPointerList<{0}>", type);
                        suffix += "s";
                    }
                    
                    AppendLine($"internal {typeString} {Name(pointer)}{suffix} {{ get; }} = {defaultValue};");
                }
            }
        }

        public void AppendProperties(XElement item)
        {
            foreach (var property in GetPropertiesAndPointers(item))
            {
                var propertyType = Type(property);
                var getset = $"{{ get; {SetterAccessibility(property)}set; }}";
                if (IsPointer(property))
                {
                    if (AllowMultiples(property))
                    {
                        getset = $"{{ get {{ return {Name(property)}Pointers; }} }}";
                    }
                    else
                    {
                        getset = $"{{ get {{ return {Name(property)}Pointer.Item as {propertyType}; }} set {{ {Name(property)}Pointer.Item = value; }} }}";
                    }
                }

                if (AllowMultiples(property))
                {
                    propertyType = string.Format("IList<{0}>", propertyType);
                }

                var comment = Comment(property);
                if (comment != null)
                {
                    AppendLine("/// <summary>");
                    AppendLine($"/// {comment}");
                    AppendLine("/// </summary>");
                }

                AppendLine($"{Accessibility(property)} {propertyType} {Name(property)} {getset}");
            }
        }

        public void AppendTrySetPairMethod(XElement item)
        {
            if (GetPropertiesAndPointers(item).Any() && GenerateReaderFunction(item))
            {
                AppendLine();
                AppendLine("internal override bool TrySetPair(DxfCodePair pair)");
                AppendLine("{");
                IncreaseIndent();
                AppendLine("switch (pair.Code)");
                AppendLine("{");
                IncreaseIndent();
                foreach (var propertyGroup in GetPropertiesAndPointers(item).Where(p => !ProtectedSet(p)).GroupBy(p => Code(p)).OrderBy(p => p.Key))
                {
                    var code = propertyGroup.Key;
                    if (propertyGroup.Count() == 1)
                    {
                        var property = propertyGroup.Single();
                        var name = Name(property);
                        var codes = GetCodeOverrides(property);
                        if (codes != null)
                        {
                            var suffix = 'X';
                            for (int i = 0; i < codes.Length; i++, suffix++)
                            {
                                AppendLine($"case {codes[i]}:");
                                AppendLine($"    this.{name} = this.{name}.WithUpdated{suffix}(pair.DoubleValue);");
                                AppendLine("    break;");
                            }
                        }
                        else
                        {
                            var codeType = DxfCodePair.ExpectedType(code);
                            var codeTypeValue = TypeToString(codeType);
                            if (IsPointer(property))
                            {
                                name += AllowMultiples(property) ? "Pointers.Pointers" : "Pointer.Handle";
                            }

                            var assignCode = AllowMultiples(property)
                                ? string.Format("this.{0}.Add(", name)
                                : string.Format("this.{0} = ", name);
                            var assignSuffix = AllowMultiples(property)
                                ? ")"
                                : "";
                            var readConverter = ReadConverter(property);
                            var value = string.Format(readConverter, $"pair.{codeTypeValue}");
                            if (IsPointer(property) && AllowMultiples(property))
                            {
                                value = "new DxfPointer(" + value + ")";
                            }

                            AppendLine($"case {code}:");
                            AppendLine($"    {assignCode}{value}{assignSuffix};");
                            AppendLine("    break;");
                        }
                    }
                    else
                    {
                        AppendLine($"case {code}:");
                        if (item.Name.LocalName == "Object" && code > 0)
                        {
                            IncreaseIndent();
                            AppendLine($"switch (_code_{code}_index)");
                            AppendLine("{");
                            IncreaseIndent();
                            for (int i = 0; i < propertyGroup.Count(); i++)
                            {
                                var property = propertyGroup.ElementAt(i);
                                var readConverter = ReadConverter(property);
                                var value = string.Format(readConverter, $"pair.{TypeToString(DxfCodePair.ExpectedType(code))}");
                                AppendLine($"case {i}:");
                                AppendLine($"    this.{Name(property)} = {value};");
                                AppendLine($"    _code_{code}_index++;");
                                AppendLine("    break;");
                            }

                            AppendLine("default:");
                            AppendLine($"    Debug.Assert(false, \"Unexpected extra values for code {code}\");");
                            AppendLine("    break;");
                            DecreaseIndent();
                            AppendLine("}");
                            DecreaseIndent();
                        }
                        else
                        {
                            AppendLine($"    // code is custom-handled and shared by properties {string.Join(", ", propertyGroup.Select(p => Name(p)))}");
                        }

                        AppendLine("    break;");
                    }
                }

                AppendLine("default:");
                AppendLine("    return base.TrySetPair(pair);");
                DecreaseIndent();
                AppendLine("}"); // end switch
                AppendLine();
                AppendLine("return true;");
                DecreaseIndent();
                AppendLine("}"); // end method
            }
        }

        public string AttributeOrDefault(XElement xml, string attributeName, string defaultValue = null)
        {
            var att = xml?.Attribute(attributeName);
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

        public string ClassName(XElement xml)
        {
            return AttributeOrDefault(xml, "ClassName");
        }

        public int Code(XElement property)
        {
            return int.Parse(property.Attribute("Code").Value);
        }

        public int[] CodeOverrides(XElement xml)
        {
            var text = AttributeOrDefault(xml, "CodeOverrides");
            if (text == null)
            {
                return null;
            }

            return text.Split(",".ToCharArray()).Select(i => int.Parse(i)).ToArray();
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

        public string ExpandCommentOrNull(string baseComment, string template)
        {
            if (baseComment == null)
            {
                return null;
            }

            return string.Format(template, baseComment);
        }

        public bool GenerateReaderFunction(XElement entity)
        {
            return bool.Parse(AttributeOrDefault(entity, "GenerateReaderFunction", "true"));
        }

        public bool GenerateWriterFunction(XElement entity)
        {
            return bool.Parse(AttributeOrDefault(entity, "GenerateWriterFunction", "true"));
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

            if (IsPointer(property) && !AllowMultiples(property))
            {
                writePredicates.Add(string.Format("this.{0}Pointer.Handle != 0u", name));
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
                    lines.Add(string.Format("{0}pairs.Add(new DxfCodePair({1}, {2}.{3}));", indentPrefix, codes[i], name, suffix));
                }
            }
            else
            {
                var writeConverter = WriteConverter(property);
                if (AllowMultiples(property))
                {
                    var value = string.Format(writeConverter, "p");
                    var infixPredicate = string.Empty;
                    if (IsPointer(property))
                    {
                        name += "Pointers.Pointers";
                        value = "DxfCommonConverters.UIntHandle(p.Handle)";
                        infixPredicate = string.Format(".Where(p => p.Handle != 0u)");
                    }

                    lines.Add(string.Format("{0}pairs.AddRange(this.{1}{2}.Select(p => new DxfCodePair({3}, {4})));", indentPrefix, name, infixPredicate, code, value));
                }
                else
                {
                    if (IsPointer(property))
                    {
                        name += "Pointer.Handle";
                    }
                    var value = string.Format(writeConverter, $"this.{name}");
                    lines.Add(string.Format("{0}pairs.Add(new DxfCodePair({1}, {2}));", indentPrefix, code, value));
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

        public bool HasFlags(XElement xml)
        {
            return bool.Parse(AttributeOrDefault(xml, "HasFlags", "true"));
        }

        public string HeaderVariable(XElement xml)
        {
            return AttributeOrDefault(xml, "HeaderVariable");
        }

        public string Identifier(string value)
        {
            return value[0] >= '0' && value[0] <= '9' ? "_" + value : value;
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

        public string ObjectType(XElement obj)
        {
            return obj.Attribute("ObjectType").Value;
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
                return "DxfCommonConverters.UIntHandle({0})";
            }
            else
            {
                var converter = property.Attribute("ReadConverter")?.Value ?? "{0}";
                if (!converter.Contains("{0"))
                {
                    throw new InvalidOperationException($"Read converter for '{Name(property)}' must contain a string format hole.");
                }

                return converter;
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

        public bool SuppressWriting(XElement property)
        {
            var att = property.Attribute("SuppressWriting");
            return att != null && bool.Parse(att.Value);
        }

        public string TableClassName(XElement table)
        {
            return AttributeOrDefault(table, "TableClassName");
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
            return entity.Attribute("TypeString").Value;
        }

        public string TypeStringVariable(XElement entity)
        {
            return entity.Attribute("TypeStringVariable").Value;
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
            else if (type == typeof(byte[])) expected = "Binary";
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
                return "DxfCommonConverters.UIntHandle({0})";
            }
            else
            {
                var converter = property.Attribute("WriteConverter")?.Value ?? "{0}";
                if (!converter.Contains("{0"))
                {
                    throw new InvalidOperationException($"Write converter for '{Name(property)}' must contain a string format hole.");
                }

                return converter;
            }
        }

        public IEnumerable<string> WriteCustomCode(XElement spec)
        {
            var code = AttributeOrDefault(spec, "Code");
            return new[] { code };
        }

        public IEnumerable<string> WriteProperty(XElement spec, XElement entity)
        {
            var property = GetPropertiesAndPointers(entity).Single(p => Name(p) == spec.Attribute("Property").Value);
            return GetPropertyWriteLines(property);
        }

        public IEnumerable<string> WriteSpecificValue(XElement spec)
        {
            var code = spec.Attribute("Code").Value;
            var writeConverter = WriteConverter(spec);
            var value = string.Format(writeConverter, spec.Attribute("Value").Value);

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
                predicate.Add(string.Format("{0} != {1}", spec.Attribute("Value").Value, defaultValue.Value));
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

        private int _foreachLevel = 0;

        public IEnumerable<string> WriteValue(XElement spec, XElement entity)
        {
            switch (spec.Name.LocalName)
            {
                case "WriteSpecificValue":
                    return WriteSpecificValue(spec);
                case "WriteProperty":
                    return WriteProperty(spec, entity);
                case "Foreach":
                    {
                        var property = spec.Attribute("Property").Value;
                        var condition = spec.Attribute("Condition")?.Value;

                        var lines = new List<string>();
                        var indent = "";
                        if (condition != null)
                        {
                            indent = "    ";
                            lines.Add($"if ({condition})");
                            lines.Add("{");
                        }

                        var itemSuffix = _foreachLevel == 0 ? "" : _foreachLevel.ToString();
                        lines.Add($"{indent}foreach (var item{itemSuffix} in {property})");
                        lines.Add($"{indent}{{");
                        _foreachLevel++;
                        lines.AddRange(spec.Elements().SelectMany(e => WriteValue(e, entity)).Select(l => $"{indent}    {l}"));
                        _foreachLevel--;
                        lines.Add($"{indent}}}");
                        if (condition != null)
                        {
                            lines.Add("}");
                        }

                        lines.Add(Environment.NewLine);
                        return lines;
                    }
                case "WriteExtensionData":
                    return new[] { "AddExtensionValuePairs(pairs, version, outputHandles);" };
                case "WriteCustomCode":
                    return WriteCustomCode(spec);
                case "WriteBinaryData":
                    {
                        var value = spec.Attribute("Value").Value;
                        var countCode = spec.Attribute("CountCode").Value;
                        var chunkCode = spec.Attribute("ChunkCode").Value;
                        var predicates = new List<string>();
                        predicates.Add($"({value}?.Length ?? 0) > 0");
                        if (MinVersion(spec) != null)
                        {
                            predicates.Add($"version >= DxfAcadVersion.{MinVersion(spec)}");
                        }

                        if (MaxVersion(spec) != null)
                        {
                            predicates.Add($"version <= DxfAcadVersion.{MaxVersion(spec)}");
                        }

                        var lines = new List<string>();
                        lines.Add($"if ({string.Join(" && ", predicates)})");
                        lines.Add("{");
                        lines.Add($"    pairs.Add(new DxfCodePair({countCode}, {value}.Length));");
                        lines.Add($"    foreach (var chunk in BinaryHelpers.ChunkBytes({value}))");
                        lines.Add("    {");
                        lines.Add($"        pairs.Add(new DxfCodePair({chunkCode}, chunk));");
                        lines.Add("    }");
                        lines.Add("}");
                        lines.Add(Environment.NewLine);

                        return lines;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public void CreateNewFile(string ns, params string[] usings)
        {
            if (_sb != null)
            {
                throw new Exception($"File is still being written.  Please call `{nameof(FinishFile)}`() first.");
            }

            _sb = new StringBuilder();
            _indentionLevel = 0;
            AppendLine("// The contents of this file are automatically generated by a tool, and should not be directly modified.");
            AppendLine();
            foreach (var u in usings)
            {
                AppendLine($"using {u};");
            }

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
