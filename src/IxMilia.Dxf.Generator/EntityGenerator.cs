using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IxMilia.Dxf.Generator
{
    public class EntityGenerator : GeneratorBase
    {
        private string _outputDir;
        private XElement _xml;
        private string _xmlns;
        private IEnumerable<XElement> _entities;

        public const string EntityNamespace = "IxMilia.Dxf.Entities";

        public EntityGenerator(string outputDir)
        {
            _outputDir = outputDir;
            Directory.CreateDirectory(_outputDir);
        }

        public void Run()
        {
            _xml = XDocument.Load(Path.Combine("Specs", "EntitiesSpec.xml")).Root;
            _xmlns = _xml.Name.NamespaceName;
            _entities = _xml.Elements(XName.Get("Entity", _xmlns)).Where(x => x.Attribute("Name").Value != "DxfEntity");

            OutputDxfEntityType();
            OutputDxfEntity();
            OutputDxfEntities();
        }

        private void OutputDxfEntityType()
        {
            CreateNewFile(EntityNamespace, "System", "System.Collections.Generic", "System.Linq", "IxMilia.Dxf.Collections");
            IncreaseIndent();
            AppendLine("public enum DxfEntityType");
            AppendLine("{");
            IncreaseIndent();
            var enumNames = _entities.Select(e => EntityType(e)).Distinct().OrderBy(e => e);
            var enumStr = string.Join($",{Environment.NewLine}        ", enumNames);
            AppendLine(enumStr);
            DecreaseIndent();
            AppendLine("}");
            DecreaseIndent();
            FinishFile();
            WriteFile(Path.Combine(_outputDir, "DxfEntityTypeGenerated.cs"));
        }

        private void OutputDxfEntity()
        {
            var baseEntity = _xml.Elements(XName.Get("Entity", _xmlns)).Where(x => Name(x) == "DxfEntity").Single();
            CreateNewFile(EntityNamespace, "System", "System.Collections.Generic", "System.Linq", "IxMilia.Dxf.Collections");
            IncreaseIndent();
            AppendLine("/// <summary>");
            AppendLine("/// DxfEntity class");
            AppendLine("/// </summary>");
            AppendLine("public partial class DxfEntity : IDxfItemInternal");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("uint IDxfItemInternal.Handle { get; set; }");
            AppendLine("uint IDxfItemInternal.OwnerHandle { get; set; }");
            AppendLine("public IDxfItem Owner { get; private set;}");
            AppendLine();
            AppendLine("void IDxfItemInternal.SetOwner(IDxfItem owner)");
            AppendLine("{");
            AppendLine("    Owner = owner;");
            AppendLine("}");
            AppendLine();
            AppendLine("protected void SetOwner(IDxfItem owner)");
            AppendLine("{");
            AppendLine("    ((IDxfItemInternal)this).SetOwner(owner);");
            AppendLine("}");

            AppendPointers(baseEntity);
            AppendProperties(baseEntity);

            AppendLine();
            AppendLine("public string EntityTypeString");
            AppendLine("{");
            AppendLine("    get");
            AppendLine("    {");
            AppendLine("        switch (EntityType)");
            AppendLine("        {");
            foreach (var entity in _entities)
            {
                var typeString = TypeString(entity);
                var commaIndex = typeString.IndexOf(',');
                if (commaIndex >= 0)
                {
                    typeString = typeString.Substring(0, commaIndex);
                }

                if (!string.IsNullOrEmpty(typeString))
                {
                    AppendLine($"            case DxfEntityType.{EntityType(entity)}:");
                    AppendLine($"                return \"{typeString}\";");
                }
            }

            AppendLine("            default:");
            AppendLine("                throw new NotImplementedException();");
            AppendLine("        }"); // end switch
            AppendLine("    }"); // end getter
            AppendLine("}"); // end method
            AppendLine();

            //
            // Constructors
            //
            AppendLine("protected DxfEntity(DxfEntity other)");
            AppendLine("    : this()");
            AppendLine("{");
            AppendLine("    ((IDxfItemInternal)this).Handle = ((IDxfItemInternal)other).Handle;");
            AppendLine("    ((IDxfItemInternal)this).OwnerHandle = ((IDxfItemInternal)other).OwnerHandle;");
            AppendLine("    ((IDxfItemInternal)this).SetOwner(((IDxfItemInternal)other).Owner);");
            foreach (var property in GetPropertiesAndPointers(baseEntity))
            {
                var name = Name(property);
                if (IsPointer(property))
                {
                    name += "Pointer";
                    AppendLine($"    this.{name}.Handle = other.{name}.Handle;");
                    AppendLine($"    this.{name}.Item = other.{name}.Item;");
                }
                else
                {
                    AppendLine($"    this.{name} = other.{name};");
                }
            }

            AppendLine("}"); // end method

            //
            // Initialize
            //
            AppendLine();
            AppendLine("protected virtual void Initialize()");
            AppendLine("{");
            foreach (var property in GetProperties(baseEntity))
            {
                var defaultValue = AllowMultiples(property)
                    ? string.Format("new ListNonNull<{0}>()", Type(property))
                    : DefaultValue(property);
                AppendLine($"    this.{Name(property)} = {defaultValue};");
            }

            AppendLine("}"); // end method

            //
            // AddValuePairs
            //
            AppendLine();
            AppendLine("protected virtual void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)");
            AppendLine("{");
            AppendLine("    pairs.Add(new DxfCodePair(0, EntityTypeString));");
            IncreaseIndent();
            foreach (var line in GetWriteCommands(baseEntity))
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

            DecreaseIndent();
            AppendLine("}"); // end method

            //
            // TrySetPair
            //
            AppendLine();
            AppendLine("internal virtual bool TrySetPair(DxfCodePair pair)");
            AppendLine("{");
            AppendLine("    switch (pair.Code)");
            AppendLine("    {");
            AppendLine("        case 5:");
            AppendLine("            ((IDxfItemInternal)this).Handle = UIntHandle(pair.StringValue);");
            AppendLine("            break;");
            AppendLine("        case 330:");
            AppendLine("            ((IDxfItemInternal)this).OwnerHandle = UIntHandle(pair.StringValue);");
            AppendLine("            break;");
            foreach (var propertyGroup in GetPropertiesAndPointers(baseEntity).Where(p => !ProtectedSet(p)).GroupBy(p => Code(p)).OrderBy(p => p.Key))
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
                            AppendLine($"        case {codes[i]}:");
                            AppendLine($"            this.{name} = this.{name}.WithUpdated{suffix}(pair.DoubleValue);");
                            AppendLine("            break;");
                        }
                    }
                    else
                    {
                        if (IsPointer(property))
                        {
                            name += "Pointer.Handle";
                        }

                        var codeType = DxfCodePair.ExpectedType(code);
                        var codeTypeValue = TypeToString(codeType);
                        var assignCode = AllowMultiples(property)
                            ? string.Format("this.{0}.Add(", name)
                            : string.Format("this.{0} = ", name);
                        var assignSuffix = AllowMultiples(property)
                            ? ")"
                            : "";
                        var readConverter = ReadConverter(property);
                        AppendLine($"        case {code}:");
                        AppendLine($"            {assignCode}{string.Format(readConverter, $"pair.{codeTypeValue}")}{assignSuffix};");
                        AppendLine($"            break;");
                    }
                }
                else
                {
                    AppendLine($"        case {code}:");
                    AppendLine($"            // TODO: code is shared by properties {string.Join(", ", propertyGroup.Select(p => Name(p)))}");
                    AppendLine("            break;");
                }

            }

            AppendLine("        default:");
            AppendLine("            return false;");
            AppendLine("    }"); // end switch
            AppendLine();
            AppendLine("    return true;");
            AppendLine("}"); // end method

            //
            // FromBuffer
            //
            AppendLine();
            AppendLine("internal static DxfEntity FromBuffer(DxfCodePairBufferReader buffer)");
            AppendLine("{");
            AppendLine("    var first = buffer.Peek();");
            AppendLine("    buffer.Advance();");
            AppendLine("    DxfEntity entity;");
            AppendLine("    switch (first.StringValue)");
            AppendLine("    {");
            foreach (var entity in _entities)
            {
                var typeString = TypeString(entity);
                if (!string.IsNullOrEmpty(typeString))
                {
                    var typeStrings = typeString.Split(',');
                    foreach (var singleTypeString in typeStrings)
                    {
                        AppendLine($"        case \"{singleTypeString}\":");
                    }

                    AppendLine($"            entity = new {Name(entity)}();");
                    AppendLine("            break;");
                }
            }

            AppendLine("        default:");
            AppendLine("            SwallowEntity(buffer);");
            AppendLine("            entity = null;");
            AppendLine("            break;");
            AppendLine("    }"); // end switch
            AppendLine();
            AppendLine("    if (entity != null)");
            AppendLine("    {");
            AppendLine("        entity = entity.PopulateFromBuffer(buffer);");
            AppendLine("    }");
            AppendLine();
            AppendLine("    return entity;");
            AppendLine("}"); // end method

            DecreaseIndent();
            AppendLine("}"); // end class
            DecreaseIndent();
            FinishFile();
            WriteFile(Path.Combine(_outputDir, "DxfEntityGenerated.cs"));
        }

        private void OutputDxfEntities()
        {
            foreach (var entity in _entities)
            {
                var className = Name(entity);
                CreateNewFile(EntityNamespace, "System", "System.Collections.Generic", "System.Linq", "IxMilia.Dxf.Collections", "IxMilia.Dxf.Objects");
                IncreaseIndent();
                OutputSingleDxfEntity(entity);
                DecreaseIndent();
                FinishFile();
                WriteFile(Path.Combine(_outputDir, className + "Generated.cs"));
            }
        }

        private void OutputSingleDxfEntity(XElement entity)
        {
            AppendLine("/// <summary>");
            AppendLine($"/// {Name(entity)} class");
            AppendLine("/// </summary>");
            var baseClass = BaseClass(entity, "DxfEntity");
            if (GetPointers(entity).Any())
            {
                baseClass += ", IDxfItemInternal";
            }

            AppendLine($"public partial class {Name(entity)} : {baseClass}");
            AppendLine("{");
            IncreaseIndent();
            AppendLine($"public override DxfEntityType EntityType {{ get {{ return DxfEntityType.{EntityType(entity)}; }} }}");
            AppendMinAndMaxVersions(entity);
            AppendPointers(entity);
            AppendProperties(entity);
            AppendFlags(entity);
            AppendDefaultConstructor(entity);
            AppendParameterizedConstructors(entity);
            AppendCopyConstructor(entity);
            AppendInitializeMethod(entity, BaseClass(entity, "") == "DxfDimensionBase" ? $"this.DimensionType = DxfDimensionType.{Tag(entity)};" : null);
            AppendAddValuePairsMethod(entity);
            AppendTrySetPairMethod(entity);

            //
            // Extents
            //
            var extentsElement = entity.Element(XName.Get("Extents", entity.Name.NamespaceName));
            if (AttributeOrDefault(extentsElement, "Custom", "false") != "true")
            {
                var extents = extentsElement?.Elements(XName.Get("Value", entity.Name.NamespaceName));
                AppendLine();
                AppendLine("protected override IEnumerable<DxfPoint> GetExtentsPoints()");
                AppendLine("{");
                IncreaseIndent();
                if (extents == null)
                {
                    AppendLine("return null;");
                }
                else
                {
                    foreach (var value in extents)
                    {
                        var cond = value.Attribute("Condition");
                        if (cond != null)
                        {
                            AppendLine($"if ({cond.Value})");
                            AppendLine("{");
                            IncreaseIndent();
                        }

                        AppendLine($"yield return {value.Value};");
                        if (cond != null)
                        {
                            DecreaseIndent();
                            AppendLine("}");
                        }
                    }
                }

                DecreaseIndent();
                AppendLine("}");
            }

            //
            // PostParse
            //
            if (Name(entity) == "DxfDimensionBase")
            {
                AppendLine();
                AppendLine("protected override DxfEntity PostParse()");
                AppendLine("{");
                AppendLine("    DxfDimensionBase newDimension = null;");
                AppendLine("    switch (DimensionType)");
                AppendLine("    {");
                foreach (var ent in _entities.OrderBy(e => EntityType(e)).Where(e => BaseClass(e, "DxfEntity") == "DxfDimensionBase"))
                {
                    AppendLine($"        case DxfDimensionType.{Tag(ent)}:");
                    AppendLine($"            newDimension = new {Name(ent)}(this);");
                    AppendLine("            break;");
                }

                AppendLine("    }");
                AppendLine();
                AppendLine("    if (newDimension != null)");
                AppendLine("    {");
                AppendLine("        foreach (var pair in ExcessCodePairs)");
                AppendLine("        {");
                AppendLine("            newDimension.TrySetPair(pair);");
                AppendLine("        }");
                AppendLine("    }");
                AppendLine();
                AppendLine("    return newDimension;");
                AppendLine("}"); // end method
            }

            DecreaseIndent();
            AppendLine("}"); // end class
        }
    }
}
