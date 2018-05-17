// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IxMilia.Dxf.Generator
{
    public class TableGenerator : GeneratorBase
    {
        private string _outputDir;
        private XElement _xml;
        private string _xmlns;
        private IEnumerable<XElement> _tables;

        public const string TableNamespace = "IxMilia.Dxf.Tables";

        public TableGenerator(string outputDir)
        {
            _outputDir = outputDir;
            Directory.CreateDirectory(_outputDir);
        }

        public void Run()
        {
            _xml = XDocument.Load(Path.Combine("Specs", "TableSpec.xml")).Root;
            _xmlns = _xml.Name.NamespaceName;
            _tables = _xml.Elements(XName.Get("Table", _xmlns));

            OutputTables();
            OutputTableItems();
        }

        private void OutputTables()
        {
            foreach (var table in _tables)
            {
                var tableItem = Name(table.Element(XName.Get("TableItem", _xmlns)));
                var className = "Dxf" + Type(table) + "Table";
                CreateNewFile(TableNamespace, "System.Linq", "System.Collections.Generic", "IxMilia.Dxf.Collections", "IxMilia.Dxf.Sections");

                IncreaseIndent();
                AppendLine($"public partial class {className} : DxfTable");
                AppendLine("{");
                IncreaseIndent();
                AppendLine($"internal override DxfTableType TableType {{ get {{ return DxfTableType.{Type(table)}; }} }}");

                var tableClassName = TableClassName(table);
                if (tableClassName != null)
                {
                    AppendLine($"internal override string TableClassName {{ get {{ return \"{tableClassName}\"; }} }}");
                }

                AppendLine();
                AppendLine($"public IList<{tableItem}> Items {{ get; private set; }}");
                AppendLine();
                AppendLine("protected override IEnumerable<DxfSymbolTableFlags> GetSymbolItems()");
                AppendLine("{");
                AppendLine("#if NET35");
                AppendLine("    return Items.Cast<DxfSymbolTableFlags>();");
                AppendLine("#else");
                AppendLine("    return Items;");
                this.AppendLine("#endif");
                AppendLine("}");

                //
                // Constructor
                //
                AppendLine();
                AppendLine($"public {className}()");
                AppendLine("{");
                AppendLine($"    Items = new ListNonNull<{tableItem}>();");
                AppendLine("    Normalize();");
                AppendLine("}");

                //
                // ReadFromBuffer
                //
                AppendLine();
                AppendLine("internal static DxfTable ReadFromBuffer(DxfCodePairBufferReader buffer)");
                AppendLine("{");
                AppendLine($"    var table = new {className}();");
                AppendLine("    table.Items.Clear();");
                AppendLine("    while (buffer.ItemsRemain)");
                AppendLine("    {");
                AppendLine("        var pair = buffer.Peek();");
                AppendLine("        buffer.Advance();");
                AppendLine("        if (DxfTablesSection.IsTableEnd(pair))");
                AppendLine("        {");
                AppendLine("            break;");
                AppendLine("        }");
                AppendLine();
                AppendLine($"        if (pair.Code == 0 && pair.StringValue == DxfTable.{TypeStringVariable(table)})");
                AppendLine("        {");
                AppendLine($"            var item = {tableItem}.FromBuffer(buffer);");
                AppendLine("            table.Items.Add(item);");
                AppendLine("        }");
                AppendLine("    }"); // end while
                AppendLine();
                AppendLine("    return table;");
                AppendLine("}"); // end method

                DecreaseIndent();
                AppendLine("}"); // end class
                DecreaseIndent();

                FinishFile();
                WriteFile(Path.Combine(_outputDir, $"{className}Generated.cs"));
            }
        }

        private void OutputTableItems()
        {
            foreach (var table in _tables)
            {
                var tableItem = table.Element(XName.Get("TableItem", _xmlns));
                var properties = tableItem.Elements(XName.Get("Property", _xmlns));
                CreateNewFile("IxMilia.Dxf", "System.Linq", "System.Collections.Generic", "IxMilia.Dxf.Collections", "IxMilia.Dxf.Sections", "IxMilia.Dxf.Tables");

                IncreaseIndent();
                AppendLine($"public partial class {Name(tableItem)} : DxfSymbolTableFlags");
                AppendLine("{");
                IncreaseIndent();

                AppendLine($"internal const string AcDbText = \"{ClassName(tableItem)}\";");
                AppendLine();
                AppendLine($"protected override DxfTableType TableType {{ get {{ return DxfTableType.{Type(table)}; }} }}");

                //
                // Properties
                //
                if (properties.Any())
                {
                    AppendLine();
                }

                var seenProperties = new HashSet<string>();
                foreach (var property in properties)
                {
                    var name = Name(property);
                    if (!seenProperties.Contains(name))
                    {
                        seenProperties.Add(name);
                        var propertyType = Type(property);
                        if (AllowMultiples(property))
                        {
                            propertyType = $"IList<{propertyType}>";
                        }

                        var getset = $"{{ get; {SetterAccessibility(property)}set; }}";
                        AppendLine($"{Accessibility(property)} {propertyType} {name} {getset}");
                    }
                }

                AppendLine();
                AppendLine("public DxfXData XData { get; set; }");

                //
                // Constructor
                //
                AppendLine();
                AppendLine($"public {Name(tableItem)}()");
                AppendLine("    : base()");
                AppendLine("{");
                IncreaseIndent();
                foreach (var property in properties)
                {
                    var defaultValue = DefaultValue(property);
                    if (AllowMultiples(property))
                    {
                        defaultValue = $"new ListNonNull<{Type(property)}>()";
                    }

                    AppendLine($"{Name(property)} = {defaultValue};");
                }

                DecreaseIndent();
                AppendLine("}"); // end constructor

                //
                // AddValuePairs
                //
                if (GenerateWriterFunction(tableItem))
                {
                    AppendLine();
                    AppendLine("internal override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)");
                    AppendLine("{");
                    IncreaseIndent();
                    AppendLine("if (version >= DxfAcadVersion.R13)");
                    AppendLine("{");
                    AppendLine("    pairs.Add(new DxfCodePair(100, AcDbText));");
                    AppendLine("}");
                    AppendLine();
                    AppendLine("pairs.Add(new DxfCodePair(2, Name));");
                    if (HasFlags(tableItem))
                    {
                        AppendLine("pairs.Add(new DxfCodePair(70, (short)StandardFlags));");
                    }

                    foreach (var property in properties)
                    {
                        var disableWritingDefault = DisableWritingDefault(property);
                        var writeCondition = WriteCondition(property);
                        var minVersion = MinVersion(property);
                        var maxVersion = MaxVersion(property);
                        var hasPredicate = disableWritingDefault || writeCondition != null || minVersion != null || maxVersion != null;
                        string predicate = null;
                        if (hasPredicate)
                        {
                            var parts = new List<string>();
                            if (disableWritingDefault)
                            {
                                parts.Add(string.Format("{0} != {1}", Name(property), DefaultValue(property)));
                            }

                            if (writeCondition != null)
                            {
                                parts.Add(writeCondition);
                            }

                            if ((minVersion != null || maxVersion != null) && minVersion == maxVersion)
                            {
                                parts.Add("version == DxfAcadVersion." + minVersion);
                            }
                            else
                            {
                                if (minVersion != null)
                                {
                                    parts.Add("version >= DxfAcadVersion." + minVersion);
                                }

                                if (maxVersion != null)
                                {
                                    parts.Add("version <= DxfAcadVersion." + maxVersion);
                                }
                            }

                            predicate = string.Join(" && ", parts);
                        }

                        if (AllowMultiples(property))
                        {
                            if (hasPredicate)
                            {
                                AppendLine($"if ({predicate})");
                                AppendLine("{");
                                IncreaseIndent();
                            }

                            AppendLine($"pairs.AddRange({Name(property)}.Select(value => new DxfCodePair({Code(property)}, value)));");

                            if (hasPredicate)
                            {
                                DecreaseIndent();
                                AppendLine("}");
                                AppendLine();
                            }
                        }
                        else
                        {
                            var codeOverrides = CodeOverrides(property);
                            if (Code(property) < 0 && codeOverrides != null)
                            {
                                char prop = 'X';
                                for (int i = 0; i < codeOverrides.Length; i++, prop++)
                                {
                                    if (hasPredicate)
                                    {
                                        AppendLine($"if ({predicate})");
                                        AppendLine("{");
                                        IncreaseIndent();
                                    }

                                    AppendLine($"pairs.Add(new DxfCodePair({codeOverrides[i]}, {WriteConverter(property)}({Name(property)}.{prop})));");

                                    if (hasPredicate)
                                    {
                                        DecreaseIndent();
                                        AppendLine("}");
                                        AppendLine();
                                    }
                                }
                            }
                            else
                            {
                                if (hasPredicate)
                                {
                                    AppendLine($"if ({predicate})");
                                    AppendLine("{");
                                    IncreaseIndent();
                                }

                                AppendLine($"pairs.Add(new DxfCodePair({Code(property)}, {WriteConverter(property)}({Name(property)})));");

                                if (hasPredicate)
                                {
                                    DecreaseIndent();
                                    AppendLine("}");
                                    AppendLine();
                                }
                            }
                        }
                    }

                    AppendLine("if (XData != null)");
                    AppendLine("{");
                    AppendLine("    XData.AddValuePairs(pairs, version, outputHandles);");
                    AppendLine("}");

                    DecreaseIndent();
                    AppendLine("}"); // end method
                }

                //
                // FromBuffer
                //
                if (GenerateReaderFunction(tableItem))
                {
                    AppendLine();
                    AppendLine($"internal static {Name(tableItem)} FromBuffer(DxfCodePairBufferReader buffer)");
                    AppendLine("{");
                    IncreaseIndent();
                    AppendLine($"var item = new {Name(tableItem)}();");
                    AppendLine("while (buffer.ItemsRemain)");
                    AppendLine("{");
                    IncreaseIndent();
                    AppendLine("var pair = buffer.Peek();");
                    AppendLine("if (pair.Code == 0)");
                    AppendLine("{");
                    AppendLine("    break;");
                    AppendLine("}");
                    AppendLine();
                    AppendLine("buffer.Advance();");
                    AppendLine("switch (pair.Code)");
                    AppendLine("{");
                    IncreaseIndent();
                    if (HasFlags(tableItem))
                    {
                        AppendLine("case 70:");
                        AppendLine("    item.StandardFlags = (int)pair.ShortValue;");
                        AppendLine("    break;");
                    }

                    AppendLine("case DxfCodePairGroup.GroupCodeNumber:");
                    AppendLine("    var groupName = DxfCodePairGroup.GetGroupName(pair.StringValue);");
                    AppendLine("    item.ExtensionDataGroups.Add(DxfCodePairGroup.FromBuffer(buffer, groupName));");
                    AppendLine("    break;");

                    foreach (var property in properties)
                    {
                        var codeOverrides = CodeOverrides(property);
                        if (Code(property) < 0 && codeOverrides != null)
                        {
                            char prop = 'X';
                            for (int i = 0; i < codeOverrides.Length; i++, prop++)
                            {
                                var codeType = DxfCodePair.ExpectedType(codeOverrides[i]);
                                var codeTypeValue = TypeToString(codeType);
                                AppendLine($"case {codeOverrides[i]}:");
                                AppendLine($"    item.{Name(property)} = item.{Name(property)}.WithUpdated{prop}(pair.{codeTypeValue});");
                                AppendLine("    break;");
                            }
                        }
                        else
                        {
                            var code = Code(property);
                            var codeType = DxfCodePair.ExpectedType(code);
                            var codeTypeValue = TypeToString(codeType);
                            AppendLine($"case {Code(property)}:");
                            if (AllowMultiples(property))
                            {
                                AppendLine($"    item.{Name(property)}.Add({ReadConverter(property)}(pair.{codeTypeValue}));");
                            }
                            else
                            {
                                AppendLine($"    item.{Name(property)} = {ReadConverter(property)}(pair.{codeTypeValue});");
                            }

                            AppendLine("    break;");
                        }
                    }

                    AppendLine("case (int)DxfXDataType.ApplicationName:");
                    AppendLine("    item.XData = DxfXData.FromBuffer(buffer, pair.StringValue);");
                    AppendLine("    break;");
                    AppendLine("default:");
                    AppendLine("    item.TrySetPair(pair);");
                    AppendLine("    break;");

                    DecreaseIndent();
                    AppendLine("}"); // end switch
                    DecreaseIndent();
                    AppendLine("}"); // end while
                    AppendLine();
                    AppendLine("return item;");
                    DecreaseIndent();
                    AppendLine("}"); // end method
                }

                DecreaseIndent();
                AppendLine("}"); // end class
                DecreaseIndent();

                FinishFile();
                WriteFile(Path.Combine(_outputDir, $"{Name(tableItem)}Generated.cs"));
            }
        }
    }
}
