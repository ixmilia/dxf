// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using IxMilia.Dxf;

namespace IxMilia.Dxf.Generator
{
    public class EntityGenerator : GeneratorBase
    {
        private string _outputDir;

        public const string EntityNamespace = "IxMilia.Dxf.Entities";

        public EntityGenerator(string outputDir)
        {
            _outputDir = outputDir;
            Directory.CreateDirectory(_outputDir);
        }

        public void Run()
        {
            var xml = XDocument.Load("EntitiesSpec.xml").Root;
            var xmlns = xml.Name.NamespaceName;
            var entities = xml.Elements(XName.Get("Entity", xmlns)).Where(x => x.Attribute("Name").Value != "DxfEntity");

            OutputDxfEntityType(entities);
            OutputDxfEntity(entities, xml, xmlns);
            OutputDxfEntities(entities, xmlns);
        }

        private void OutputDxfEntityType(IEnumerable<XElement> entities)
        {
            CreateNewFile(EntityNamespace);
            IncreaseIndent();
            AppendLine("public enum DxfEntityType");
            AppendLine("{");
            IncreaseIndent();
            var enumNames = entities.Select(e => EntityType(e)).Distinct().OrderBy(e => e);
            var enumStr = string.Join(",\r\n        ", enumNames);
            AppendLine(enumStr);
            DecreaseIndent();
            AppendLine("}");
            DecreaseIndent();
            FinishFile();
            WriteFile(Path.Combine(_outputDir, "DxfEntityTypeGenerated.cs"));
        }

        private void OutputDxfEntity(IEnumerable<XElement> entities, XElement xml, string xmlns)
        {
            var baseEntity = xml.Elements(XName.Get("Entity", xmlns)).Where(x => Name(x) == "DxfEntity").Single();
            CreateNewFile(EntityNamespace);
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
            IncreaseIndent();
            AppendLine("Owner = owner;");
            DecreaseIndent();
            AppendLine("}");

            //
            // Pointers
            //
            var pointers = GetPointers(baseEntity);
            if (pointers.Any())
            {
                AppendLine();
                AppendLine("IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()");
                AppendLine("{");
                IncreaseIndent();
                foreach (var pointer in pointers)
                {
                    AppendLine($"yield return {Name(pointer)}Pointer;");
                }

                DecreaseIndent();
                AppendLine("}");
                AppendLine();

                AppendLine("IEnumerable<IDxfItemInternal> IDxfItemInternal.GetChildItems()");
                AppendLine("{");
                AppendLine("    return ((IDxfItemInternal)this).GetPointers().Select(p => (IDxfItemInternal)p.Item);");
                AppendLine("}");
                AppendLine();

                foreach (var pointer in pointers)
                {
                    AppendLine($"internal DxfPointer {Name(pointer)}Pointer {{ get; }} = new DxfPointer();");
                }
            }

            //
            // Properties
            //
            foreach (var property in GetPropertiesAndPointers(baseEntity))
            {
                var typeString = Type(property);
                if (AllowMultiples(property))
                {
                    typeString = $"List<{typeString}>";
                }

                var getset = $"{{ get; {SetterAccessibility(property)}set; }}";
                if (IsPointer(property))
                {
                    getset = $"{{ get {{ return {Name(property)}Pointer.Item as {typeString}; }} set {{ {Name(property)}Pointer.Item = value; }} }}";
                }

                var comment = Comment(property);
                if (comment != null)
                {
                    AppendLine("/// <summary>");
                    AppendLine($"/// {comment}>");
                    AppendLine("/// </summary>");
                }

                AppendLine($"public {typeString} {Name(property)} {getset}");
            }

            AppendLine();
            AppendLine("public string EntityTypeString");
            AppendLine("{");
            AppendLine("    get");
            AppendLine("    {");
            AppendLine("        switch (EntityType)");
            AppendLine("        {");
            foreach (var entity in entities)
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
                    ? string.Format("new List<{0}>()", Type(property))
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
                            AppendLine($"            this.{name}.{suffix} = pair.DoubleValue;");
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
                        AppendLine($"        case {code}:");
                        AppendLine($"            {assignCode}{ReadConverter(property)}(pair.{codeTypeValue}){assignSuffix};");
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
            foreach (var entity in entities)
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

        private void OutputDxfEntities(IEnumerable<XElement> entities, string xmlns)
        {
            foreach (var entity in entities)
            {
                var className = Name(entity);
                var baseClass = BaseClass(entity, "DxfEntity");
                if (GetPointers(entity).Any())
                {
                    baseClass += ", IDxfItemInternal";
                }

                CreateNewFile(EntityNamespace);
                IncreaseIndent();
                OutputSingleDxfEntity(entities, entity, xmlns);
                DecreaseIndent();
                FinishFile();
                WriteFile(Path.Combine(_outputDir, className + "Generated.cs"));
            }
        }

        private void OutputSingleDxfEntity(IEnumerable<XElement> entities, XElement entity, string xmlns)
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

            // min and max entity supported versions
            var minVersion = MinVersion(entity);
            if (minVersion != null)
            {
                AppendLine($"protected override DxfAcadVersion MinVersion {{ get {{ return DxfAcadVersion.{minVersion}; }} }}");
            }

            var maxVersion = MaxVersion(entity);
            if (maxVersion != null)
            {
                AppendLine($"protected override DxfAcadVersion MaxVersion {{ get {{ return DxfAcadVersion.{maxVersion}; }} }}");
            }

            //
            // Pointers
            //
            var pointers = GetPointers(entity);
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

            //
            // Properties
            //
            foreach (var property in GetPropertiesAndPointers(entity))
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

            //
            // Flags
            //
            foreach (var property in GetProperties(entity))
            {
                var flags = property.Elements(XName.Get("Flag", xmlns));
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

            //
            // XData
            //
            if (HasXData(entity))
            {
                AppendLine();
                AppendLine("public DxfXData XData { get { return ((IDxfHasXDataHidden)this).XDataHidden; } set { ((IDxfHasXDataHidden)this).XDataHidden = value; } }");
            }

            //
            // Default constructor
            //
            var defaultConstructorType = DefaultConstructor(entity);
            if (defaultConstructorType != null)
            {
                AppendLine();
                AppendLine($"{defaultConstructorType} {Name(entity)}()");
                AppendLine("    : base()");
                AppendLine("{");
                AppendLine("}");
            }

            //
            // Parameterized constructors
            //
            var constructors = entity.Elements(XName.Get("Constructor", xmlns));
            if (constructors.Any())
            {
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.Elements(XName.Get("ConstructorParameter", xmlns));
                    var argList = new List<string>();
                    foreach (var parameter in parameters)
                    {
                        var paramName = CamlCase(Property(parameter));
                        var paramType = Type(parameter);
                        argList.Add(paramType + " " + paramName);
                    }

                    var sig = string.Join(", ", argList);
                    AppendLine();
                    AppendLine($"public {Name(entity)}({sig})");
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

            //
            // Copy constructor
            //
            var copyConstructorAccessibility = CopyConstructor(entity);
            if (copyConstructorAccessibility != null)
            {
                AppendLine();
                if (copyConstructorAccessibility == "inherited")
                {
                    AppendLine($"internal {Name(entity)}({BaseClass(entity, "DxfEntity")} other)");
                    AppendLine("    : base(other)");
                    AppendLine("{");
                }
                else
                {
                    AppendLine($"{copyConstructorAccessibility} {Name(entity)}({Name(entity)} other)");
                    AppendLine("    : base(other)");
                    AppendLine("{");
                    IncreaseIndent();
                    foreach (var property in GetPropertiesAndPointers(entity))
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

            //
            // Initialize
            //
            AppendLine();
            AppendLine("protected override void Initialize()");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("base.Initialize();");
            if (BaseClass(entity, "") == "DxfDimensionBase")
            {
                AppendLine($"this.DimensionType = DxfDimensionType.{Tag(entity)};");
            }

            foreach (var property in GetProperties(entity))
            {
                var defaultValue = AllowMultiples(property)
                    ? string.Format("new List<{0}>()", Type(property))
                    : DefaultValue(property);
                AppendLine($"this.{Name(property)} = {defaultValue};");
            }

            DecreaseIndent();
            AppendLine("}"); // end method

            //
            // AddValuePairs
            //
            AppendLine();
            AppendLine("protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("base.AddValuePairs(pairs, version, outputHandles);");
            foreach (var line in GetWriteCommands(entity))
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

            if (HasXData(entity))
            {
                AppendLine("if (XData != null)");
                AppendLine("{");
                AppendLine("    XData.AddValuePairs(pairs, version, outputHandles);");
                AppendLine("}");
            }

            DecreaseIndent();
            AppendLine("}"); // end method

            //
            // TrySetPair
            //
            if (GetPropertiesAndPointers(entity).Any() && GenerateReaderFunction(entity))
            {
                AppendLine();
                AppendLine("internal override bool TrySetPair(DxfCodePair pair)");
                AppendLine("{");
                IncreaseIndent();
                AppendLine("switch (pair.Code)");
                AppendLine("{");
                IncreaseIndent();
                foreach (var propertyGroup in GetPropertiesAndPointers(entity).Where(p => !ProtectedSet(p)).GroupBy(p => Code(p)).OrderBy(p => p.Key))
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
                                AppendLine($"    this.{name}.{suffix} = pair.DoubleValue;");
                                AppendLine("    break;");
                            }
                        }
                        else
                        {
                            var codeType = DxfCodePair.ExpectedType(code);
                            var codeTypeValue = TypeToString(codeType);
                            if (IsPointer(property))
                            {
                                name += "Pointer.Handle";
                            }

                            var assignCode = AllowMultiples(property)
                                ? string.Format("this.{0}.Add(", name)
                                : string.Format("this.{0} = ", name);
                            var assignSuffix = AllowMultiples(property)
                                ? ")"
                                : "";
                            AppendLine($"case {code}:");
                            AppendLine($"    {assignCode}{ReadConverter(property)}(pair.{codeTypeValue}){assignSuffix};");
                            AppendLine("    break;");
                        }
                    }
                    else
                    {
                        AppendLine($"case {code}:");
                        AppendLine($"    // TODO: code is shared by properties {string.Join(", ", propertyGroup.Select(p => Name(p)))}");
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
                foreach (var ent in entities.OrderBy(e => EntityType(e)).Where(e => BaseClass(e, "DxfEntity") == "DxfDimensionBase"))
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
