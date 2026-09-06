using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Program
{
    private static int Main(string[] args)
    {
        var path = args[0];
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(path));
        var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
        {
            ReadWrite = true,
            AssemblyResolver = resolver
        });
        var module = asm.MainModule;
        var existing = module.Types.FirstOrDefault(t => t.FullName == "UnityEngine.XR.XRSettings");
        if (existing != null)
        {
            Console.WriteLine("XRSettings already present");
            asm.Dispose();
            return 0;
        }

        var type = new TypeDefinition(
            "UnityEngine.XR",
            "XRSettings",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            module.TypeSystem.Object);
        module.Types.Add(type);

        AddAutoProp(module, type, "enabled", module.TypeSystem.Boolean, true);
        AddAutoProp(module, type, "isDeviceActive", module.TypeSystem.Boolean, false);
        AddAutoProp(module, type, "eyeTextureResolutionScale", module.TypeSystem.Single, true);
        AddAutoProp(module, type, "renderViewportScale", module.TypeSystem.Single, true);
        AddAutoProp(module, type, "loadedDeviceName", module.TypeSystem.String, false);

        asm.Write();
        Console.WriteLine("Injected UnityEngine.XR.XRSettings");
        return 0;
    }

    private static void AddAutoProp(ModuleDefinition module, TypeDefinition type, string name, TypeReference propType, bool hasSet)
    {
        var field = new FieldDefinition("<" + name + ">k__BackingField", FieldAttributes.Private | FieldAttributes.Static, propType);
        type.Fields.Add(field);

        var get = new MethodDefinition("get_" + name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName, propType);
        var gil = get.Body.GetILProcessor();
        gil.Emit(OpCodes.Ldsfld, field);
        gil.Emit(OpCodes.Ret);
        type.Methods.Add(get);

        var prop = new PropertyDefinition(name, PropertyAttributes.None, propType) { GetMethod = get };
        if (hasSet)
        {
            var set = new MethodDefinition("set_" + name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName, module.TypeSystem.Void);
            set.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, propType));
            var sil = set.Body.GetILProcessor();
            sil.Emit(OpCodes.Ldarg_0);
            sil.Emit(OpCodes.Stsfld, field);
            sil.Emit(OpCodes.Ret);
            type.Methods.Add(set);
            prop.SetMethod = set;
        }

        type.Properties.Add(prop);
    }
}
