using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IxMilia.Dxf.Generator
{
    public class SectionGenerator : GeneratorBase
    {
        private XElement _xml;
        private string _xmlns;
        private IEnumerable<XElement> _variables;

        public const string SectionNamespace = "IxMilia.Dxf";

        public SectionGenerator(string outputDir)
            : base(outputDir)
        {
        }

        public void Run()
        {
            _xml = XDocument.Load(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "Specs", "HeaderVariablesSpec.xml")).Root;
            _xmlns = _xml.Name.NamespaceName;
            _variables = _xml.Elements(XName.Get("Variable", _xmlns));

            OutputHeader();
        }

        private void OutputHeader()
        {
            CreateNewFile(SectionNamespace, "System", "System.Collections.Generic", "System.Diagnostics", "IxMilia.Dxf.Entities");

            IncreaseIndent();
            AppendLine("public partial class DxfHeader");
            AppendLine("{");
            IncreaseIndent();

            //
            // Key names
            //
            var seenKeys = new HashSet<string>();
            foreach (var property in _variables)
            {
                var name = Name(property).ToUpper();
                if (!seenKeys.Contains(name))
                {
                    seenKeys.Add(name);
                    AppendLine($"private const string {Identifier(name)} = \"${name}\";");
                }
            }

            //
            // Properties
            //
            var seenProperties = new HashSet<string>();
            foreach (var property in _variables)
            {
                var propertyName = Property(property);
                if (!seenProperties.Contains(propertyName))
                {
                    seenProperties.Add(propertyName); // don't write duplicate properties
                    var comment = $"/// {$"The ${Name(property)} header variable.  {Comment(property)}"}";
                    var minVersion = MinVersion(property);
                    if (minVersion != null)
                    {
                        comment += $"  Minimum AutoCAD version: {minVersion}.";
                    }

                    var maxVersion = MaxVersion(property);
                    if (maxVersion != null)
                    {
                        comment += $"  Maximum AutoCAD version: {maxVersion}.";
                    }

                    AppendLine();
                    AppendLine("/// <summary>");
                    AppendLine(comment);
                    AppendLine("/// </summary>");
                    AppendLine($"public {Type(property)} {Property(property)} {{ get; set; }}");
                }
            }

            //
            // SetDefaults
            //
            AppendLine();
            AppendLine("public void SetDefaults()");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("SetManualDefaults();");
            seenProperties.Clear();
            foreach (var property in _variables)
            {
                var propertyName = Property(property);
                if (!seenProperties.Contains(propertyName))
                {
                    seenProperties.Add(propertyName);
                    var defaultValue = DefaultValue(property);
                    if (Type(property) == "string" && defaultValue != "null" && (!defaultValue.StartsWith("\"") && !defaultValue.EndsWith("\"")))
                    {
                        defaultValue = string.Format("\"{0}\"", defaultValue);
                    }
                    else if (Type(property) == "char" && defaultValue.Length == 1)
                    {
                        if (defaultValue == "\"")
                        {
                            defaultValue = "\\\"";
                        }

                        defaultValue = string.Format("'{0}'", defaultValue);
                    }

                    AppendLine($"this.{propertyName} = {defaultValue}; // {Name(property)}");
                }
            }

            DecreaseIndent();
            AppendLine("}"); // end method

            //
            // AddValueToList
            //
            AppendLine();
            AppendLine("internal void AddValueToList(List<DxfCodePair> list)");
            AppendLine("{");
            IncreaseIndent();
            foreach (var property in _variables.Where(p => !SuppressWriting(p)))
            {
                var converter = WriteConverter(property);
                var type = Type(property);
                var dontWriteDefaultAtt = property.Attribute("DontWriteDefault");
                var minVersionAtt = property.Attribute("MinVersion");
                var maxVersionAtt = property.Attribute("MaxVersion");
                var dontWriteDefault = dontWriteDefaultAtt == null ? false : bool.Parse(dontWriteDefaultAtt.Value);
                var usingIf = dontWriteDefault || minVersionAtt != null || maxVersionAtt != null;
                AppendLine();
                AppendLine($"// {Name(property)}");
                if (usingIf)
                {
                    var ifParts = new List<string>();
                    if (minVersionAtt != null && maxVersionAtt != null && minVersionAtt.Value == maxVersionAtt.Value)
                    {
                        ifParts.Add(string.Format("Version == DxfAcadVersion.{0}", minVersionAtt.Value));
                    }
                    else
                    {
                        if (minVersionAtt != null)
                        {
                            ifParts.Add(string.Format("Version >= DxfAcadVersion.{0}", minVersionAtt.Value));
                        }
                        if (maxVersionAtt != null)
                        {
                            ifParts.Add(string.Format("Version <= DxfAcadVersion.{0}", maxVersionAtt.Value));
                        }
                    }
                    if (dontWriteDefault)
                    {
                        ifParts.Add(string.Format("this.{0} != {1}", Property(property), DefaultValue(property)));
                    }

                    var allIfText = string.Join(" && ", ifParts);

                    AppendLine($"if ({allIfText})");
                    AppendLine("{");
                    IncreaseIndent();
                }

                AppendLine($"list.Add(new DxfCodePair(9, {Identifier(Name(property))}));");
                if (type == "DxfPoint" || type == "DxfVector")
                {
                    var prop = Property(property);
                    AppendLine($"list.Add(new DxfCodePair(10, this.{prop}.X));");
                    AppendLine($"list.Add(new DxfCodePair(20, this.{prop}.Y));");
                    if (Math.Abs(Code(property)) >= 3)
                    {
                        AppendLine($"list.Add(new DxfCodePair(30, this.{prop}.Z));");
                    }
                }
                else
                {
                    AppendLine($"list.Add(new DxfCodePair({Code(property)}, {string.Format(converter, $"this.{Property(property)}")}));");
                }

                if (usingIf)
                {
                    DecreaseIndent();
                    AppendLine("}");
                }
            }

            DecreaseIndent();
            AppendLine("}"); // end method

            //
            // SetHeaderVariable
            //
            AppendLine();
            AppendLine("internal void SetHeaderVariable(string keyName, DxfCodePair pair)");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("switch (keyName)");
            AppendLine("{");
            IncreaseIndent();
            foreach (var propertyGroup in _variables.GroupBy(v => Name(v)))
            {
                AppendLine($"case {Identifier(propertyGroup.Key)}:");
                IncreaseIndent();
                var type = Type(propertyGroup.First());
                var prop = Property(propertyGroup.First());
                if (type == "DxfPoint" || type == "DxfVector")
                {
                    AppendLine($"this.{prop} = UpdatePoint(pair, this.{prop});");
                    AppendLine("break;");
                }
                else
                {
                    if (propertyGroup.Count() > 1)
                    {
                        AppendLine("switch (pair.Code)");
                        AppendLine("{");
                        IncreaseIndent();
                        foreach (var property in propertyGroup)
                        {
                            var code = Code(property);
                            var codeType = DxfCodePair.ExpectedType(code);
                            var codeTypeValue = TypeToString(codeType);
                            var converter = ReadConverter(property);
                            AppendLine($"case {code}:");
                            AppendLine($"    this.{prop} = {string.Format(converter, $"pair.{codeTypeValue}")};");
                            AppendLine("    break;");
                        }

                        AppendLine("default:");
                        AppendLine($"    Debug.Assert(false, $\"Expected code [{string.Join(", ", propertyGroup.Select(p => Code(p)))}], got {{pair.Code}}\");");
                        AppendLine("    break;");
                        DecreaseIndent();
                        AppendLine("}"); // end switch
                    }
                    else
                    {
                        var code = Code(propertyGroup.First());
                        var codeType = DxfCodePair.ExpectedType(code);
                        var codeTypeValue = TypeToString(codeType);
                        var converter = ReadConverter(propertyGroup.First());
                        AppendLine($"EnsureCode(pair, {code});");
                        AppendLine($"this.{prop} = {string.Format(converter, $"pair.{codeTypeValue}")};");
                    }

                    AppendLine("break;");
                }

                DecreaseIndent();
            }

            AppendLine("default:");
            AppendLine("    // unsupported variable");
            AppendLine("    break;");
            DecreaseIndent();
            AppendLine("}"); // end switch
            DecreaseIndent();
            AppendLine("}"); // end method

            //
            // Flags
            //
            var flagElement = XName.Get("Flag", _xmlns);
            foreach (var property in _variables)
            {
                var flags = property.Elements(flagElement);
                if (flags.Any())
                {
                    AppendLine();
                    AppendLine($"// {Name(property)} flags");
                    foreach (var flag in flags)
                    {
                        AppendLine();
                        var comment = Comment(flag);
                        var minVersion = MinVersion(property);
                        if (minVersion != null)
                        {
                            comment += $"  Minimum AutoCAD version: {minVersion}.";
                        }

                        var maxVersion = MaxVersion(property);
                        if (maxVersion != null)
                        {
                            comment += $"  Maximum AutoCAD version: {maxVersion}.";
                        }

                        AppendLine("/// <summary>");
                        AppendLine($"/// {comment}");
                        AppendLine("/// </summary>");
                        AppendLine($"public bool {Name(flag)}");
                        AppendLine("{");
                        AppendLine($"    get {{ return DxfHelpers.GetFlag({Property(property)}, {Mask(flag)}); }}");
                        AppendLine("    set");
                        AppendLine("    {");
                        AppendLine($"        var flags = {Property(property)};");
                        AppendLine($"        DxfHelpers.SetFlag(value, ref flags, {Mask(flag)});");
                        AppendLine($"        {Property(property)} = flags;");
                        AppendLine("    }");
                        AppendLine("}");
                    }
                }
            }

            //
            // GetValue
            //
            AppendLine();
            AppendLine("private object GetValue(string variableName)");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("switch (variableName.ToUpper())");
            AppendLine("{");
            IncreaseIndent();
            seenProperties.Clear();
            foreach (var property in _variables)
            {
                var propertyName = Name(property);
                if (!seenProperties.Contains(propertyName))
                {
                    seenProperties.Add(propertyName);
                    AppendLine($"case {Identifier(propertyName)}:");
                    AppendLine($"    return this.{Property(property)};");
                }
            }

            AppendLine("default:");
            AppendLine("    throw new ArgumentException(\"Unrecognized variable\", nameof(variableName));");
            DecreaseIndent();
            AppendLine("}"); // end switch
            DecreaseIndent();
            AppendLine("}"); // end method

            //
            // SetValue
            //
            AppendLine();
            AppendLine("private void SetValue(string variableName, object value)");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("switch (variableName.ToUpper())");
            AppendLine("{");
            IncreaseIndent();
            seenProperties.Clear();
            foreach (var property in _variables)
            {
                var propertyName = Name(property);
                if (!seenProperties.Contains(propertyName))
                {
                    seenProperties.Add(propertyName);
                    AppendLine($"case {Identifier(propertyName)}:");
                    AppendLine($"    this.{Property(property)} = ({Type(property)})value;");
                    AppendLine("    break;");
                }
            }

            AppendLine("default:");
            AppendLine("    throw new ArgumentException(\"Unrecognized variable\", nameof(variableName));");
            DecreaseIndent();
            AppendLine("}"); // end switch
            DecreaseIndent();
            AppendLine("}"); // end method

            DecreaseIndent();
            AppendLine("}"); // end class
            DecreaseIndent();

            FinishFile();
            WriteFile("DxfHeaderGenerated.cs");
        }
    }
}
