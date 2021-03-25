using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace IxMilia.Dxf.Generator
{
    [Generator]
    public class ObjectGenerator : GeneratorBase, ISourceGenerator
    {
        private XElement _xml;
        private string _xmlns;
        private IEnumerable<XElement> _objects;

        public const string ObjectNamespace = "IxMilia.Dxf.Objects";

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var specText = context.AdditionalFiles.Single(f => Path.GetFileName(f.Path) == "ObjectsSpec.xml").GetText().ToString();
            _xml = XDocument.Parse(specText).Root;
            _xmlns = _xml.Name.NamespaceName;
            _objects = _xml.Elements(XName.Get("Object", _xmlns)).Where(x => x.Attribute("Name").Value != "DxfObject");

            OutputDxfObjectType(context);
            OutputDxfObject(context);
            OutputDxfObjects(context);
        }

        private void OutputDxfObjectType(GeneratorExecutionContext context)
        {
            CreateNewFile(ObjectNamespace, "System", "System.Collections.Generic", "System.Linq", "IxMilia.Dxf.Collections");
            IncreaseIndent();
            AppendLine("public enum DxfObjectType");
            AppendLine("{");
            IncreaseIndent();
            var enumNames = _objects.Select(o => ObjectType(o)).Distinct().OrderBy(o => o);
            var enumStr = string.Join($",{Environment.NewLine}        ", enumNames);
            AppendLine(enumStr);
            DecreaseIndent();
            AppendLine("}");
            DecreaseIndent();
            FinishFile();
            WriteFile(context, "DxfObjectTypeGenerated.cs");
        }

        private void OutputDxfObject(GeneratorExecutionContext context)
        {
            var baseObject = _xml.Elements(XName.Get("Object", _xmlns)).Where(x => Name(x) == "DxfObject").Single();
            CreateNewFile(ObjectNamespace, "System", "System.Collections.Generic", "System.Linq", "IxMilia.Dxf.Collections");
            IncreaseIndent();
            AppendLine("/// <summary>");
            AppendLine("/// DxfObject class");
            AppendLine("/// </summary>");
            AppendLine("public partial class DxfObject : IDxfItemInternal");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("DxfHandle IDxfItemInternal.Handle { get; set; }");
            AppendLine("DxfHandle IDxfItemInternal.OwnerHandle { get; set; }");
            AppendLine("public IDxfItem Owner { get; private set;}");
            AppendLine();
            AppendLine("void IDxfItemInternal.SetOwner(IDxfItem owner)");
            AppendLine("{");
            AppendLine("    Owner = owner;");
            AppendLine("}");
            AppendLine();
            AppendLine("IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()");
            AppendLine("{");
            AppendLine("    yield break;");
            AppendLine("}");
            AppendLine();
            AppendLine("IEnumerable<IDxfItemInternal> IDxfItemInternal.GetChildItems()");
            AppendLine("{");
            AppendLine("    return ((IDxfItemInternal)this).GetPointers().Select(p => (IDxfItemInternal)p.Item);");
            AppendLine("}");

            //
            // ObjectTypeString
            //
            AppendLine();
            AppendLine("public string ObjectTypeString");
            AppendLine("{");
            AppendLine("    get");
            AppendLine("    {");
            AppendLine("        switch (ObjectType)");
            AppendLine("        {");
            foreach (var obj in _objects)
            {
                var typeString = TypeString(obj);
                var commaIndex = typeString.IndexOf(',');
                if (commaIndex >= 0)
                {
                    typeString = typeString.Substring(0, commaIndex);
                }

                if (!string.IsNullOrEmpty(typeString))
                {
                    AppendLine($"            case DxfObjectType.{ObjectType(obj)}:");
                    AppendLine($"                return \"{typeString}\";");
                }
            }

            AppendLine("            default:");
            AppendLine("                throw new NotImplementedException();");
            AppendLine("        }");
            AppendLine("    }");
            AppendLine("}");

            //
            // Copy constructor
            //
            AppendLine();
            AppendLine("protected DxfObject(DxfObject other)");
            AppendLine("    : this()");
            AppendLine("{");
            AppendLine("}");

            //
            // Initialize
            //
            AppendLine();
            AppendLine("protected virtual void Initialize()");
            AppendLine("{");
            AppendLine("}");

            //
            // AddValuePairs
            //
            AppendLine();
            AppendLine("protected virtual void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, bool writeXData)");
            AppendLine("{");
            AppendLine("    pairs.Add(new DxfCodePair(0, ObjectTypeString));");
            foreach (var line in GetWriteCommands(baseObject))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    AppendLine();
                }
                else
                {
                    AppendLine("    " + line);
                }
            }

            AppendLine("}");

            //
            // TrySetPair
            //
            AppendLine();
            AppendLine("internal virtual bool TrySetPair(DxfCodePair pair)");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("switch (pair.Code)");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("case 5:");
            AppendLine("    ((IDxfItemInternal)this).Handle = HandleString(pair.StringValue);");
            AppendLine("    break;");
            AppendLine("case 330:");
            AppendLine("    ((IDxfItemInternal)this).OwnerHandle = HandleString(pair.StringValue);");
            AppendLine("    break;");
            foreach (var propertyGroup in GetProperties(baseObject).Where(p => !ProtectedSet(p)).GroupBy(p => Code(p)).OrderBy(p => p.Key))
            {
                var code = propertyGroup.Key;
                var property = propertyGroup.Single();
                var name = Name(property);
                var codeType = DxfCodePair.ExpectedType(code);
                var codeTypeValue = TypeToString(codeType);
                var assignCode = AllowMultiples(property)
                    ? string.Format("this.{0}.Add(", Name(property))
                    : string.Format("this.{0} = ", Name(property));
                var assignSuffix = AllowMultiples(property)
                    ? ")"
                    : "";
                var readConverter = ReadConverter(property);
                AppendLine($"case {code}:");
                AppendLine($"    {assignCode}{string.Format(readConverter, $"pair.{codeTypeValue}")}{assignSuffix};");
                AppendLine("    break;");
            }

            AppendLine("default:");
            AppendLine("    return false;");
            DecreaseIndent();
            AppendLine("}"); // end switch
            AppendLine();
            AppendLine("return true;");
            DecreaseIndent();
            AppendLine("}"); // end method

            //
            // FromBuffer
            //
            AppendLine();
            AppendLine("internal static DxfObject FromBuffer(DxfCodePairBufferReader buffer)");
            AppendLine("{");
            IncreaseIndent();
            AppendLine("var first = buffer.Peek();");
            AppendLine("buffer.Advance();");
            AppendLine("DxfObject obj;");
            AppendLine("switch (first.StringValue)");
            AppendLine("{");
            IncreaseIndent();
            foreach (var obj in _objects)
            {
                var typeString = TypeString(obj);
                if (!string.IsNullOrEmpty(typeString))
                {
                    AppendLine($"case \"{typeString}\":");
                    AppendLine($"    obj = new {Name(obj)}();");
                    AppendLine("    break;");
                }
            }

            AppendLine("default:");
            AppendLine("    SwallowObject(buffer);");
            AppendLine("    obj = null;");
            AppendLine("    break;");
            DecreaseIndent();
            AppendLine("}"); // end switch
            AppendLine();
            AppendLine("if (obj != null)");
            AppendLine("{");
            AppendLine("    obj = obj.PopulateFromBuffer(buffer);");
            AppendLine("}");
            AppendLine();
            AppendLine("return obj;");
            DecreaseIndent();
            AppendLine("}"); // end method

            DecreaseIndent();
            AppendLine("}"); // end class
            DecreaseIndent();
            FinishFile();
            WriteFile(context, "DxfObjectGenerated.cs");
        }

        private void OutputDxfObjects(GeneratorExecutionContext context)
        {
            foreach (var obj in _objects)
            {
                var className = Name(obj);
                CreateNewFile(ObjectNamespace, "System", "System.Collections.Generic", "System.Diagnostics", "System.Linq", "IxMilia.Dxf.Collections", "IxMilia.Dxf.Entities");
                IncreaseIndent();
                OutputSingleDxfObject(obj);
                DecreaseIndent();
                FinishFile();
                WriteFile(context, className + "Generated.cs");
            }
        }

        private void OutputSingleDxfObject(XElement obj)
        {
            AppendLine("/// <summary>");
            AppendLine($"/// {Name(obj)} class");
            AppendLine("/// </summary>");
            var baseClass = BaseClass(obj, "DxfObject");
            if (GetPointers(obj).Any())
            {
                baseClass += ", IDxfItemInternal";
            }

            AppendLine($"{Accessibility(obj)} partial class {Name(obj)} : {baseClass}");
            AppendLine("{");
            IncreaseIndent();
            AppendLine($"public override DxfObjectType ObjectType {{ get {{ return DxfObjectType.{ObjectType(obj)}; }} }}");
            AppendMinAndMaxVersions(obj);
            AppendPointers(obj);
            AppendProperties(obj);
            AppendFlags(obj);
            AppendDefaultConstructor(obj);
            AppendParameterizedConstructors(obj);
            AppendCopyConstructor(obj);
            AppendInitializeMethod(obj);
            AppendAddValuePairsMethod(obj);

            //
            // TrySetPair
            //
            if (GetPropertiesAndPointers(obj).Any() && GenerateReaderFunction(obj))
            {
                // handle codes with multiple values (but only positive codes (negative codes mean special handling))
                var multiCodeProperties = GetProperties(obj)
                    .Where(p => !ProtectedSet(p))
                    .GroupBy(p => Code(p))
                    .Where(p => p.Key > 0 && p.Count() > 1)
                    .OrderBy(p => p.Key);
                if (multiCodeProperties.Any())
                {
                    AppendLine();
                    AppendLine("// This object has vales that share codes between properties and these counters are used to know which property to");
                    AppendLine("// assign to in TrySetPair() below.");
                }

                foreach (var propertyGroup in multiCodeProperties)
                {
                    AppendLine($"private int _code_{propertyGroup.Key}_index = 0; // shared by properties {string.Join(", ", propertyGroup.Select(p => Name(p)))}");
                }
            }

            AppendTrySetPairMethod(obj);

            DecreaseIndent();
            AppendLine("}"); // end class
        }
    }
}
