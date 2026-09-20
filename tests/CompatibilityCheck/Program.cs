using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: CompatibilityCheck <Valheim Managed directory> <BepInEx directory> <PlayerTradingReforged.dll>");
    return 2;
}

string[] searchPaths = [Path.GetFullPath(args[0]), Path.Combine(Path.GetFullPath(args[1]), "core")];
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    foreach (string directory in searchPaths)
    {
        string path = Path.Combine(directory, $"{name.Name}.dll");
        if (File.Exists(path)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }
    return null;
};

Assembly plugin = Assembly.LoadFrom(Path.GetFullPath(args[2]));
List<string> failures = [];
int checkedPatches = 0;
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
foreach (Type type in plugin.GetTypes())
{
    HarmonyPatch[] attributes = type.GetCustomAttributes<HarmonyPatch>().ToArray();
    if (attributes.Length == 0) continue;
    foreach (MethodInfo patch in type.GetMethods(flags).Where(method => method.Name is "Prefix" or "Postfix" or "Finalizer"))
    {
        HarmonyMethod[] descriptions = attributes.Concat(patch.GetCustomAttributes<HarmonyPatch>()).Select(attribute => attribute.info).ToArray();
        Type? targetType = descriptions.LastOrDefault(info => info.declaringType != null)?.declaringType;
        string? targetName = descriptions.LastOrDefault(info => info.methodName != null)?.methodName;
        Type[]? arguments = descriptions.LastOrDefault(info => info.argumentTypes != null)?.argumentTypes;
        MethodInfo[] candidates = targetType?.GetMethods(flags)
            .Where(method => method.Name == targetName && (arguments == null || method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(arguments))).ToArray() ?? [];
        string label = $"{type.FullName}.{patch.Name}";
        if (candidates.Length != 1)
        {
            failures.Add($"{label}: expected one matching target {targetType?.FullName}.{targetName}, found {candidates.Length}.");
            continue;
        }
        MethodInfo original = candidates[0];
        checkedPatches++;
        foreach (ParameterInfo parameter in patch.GetParameters())
        {
            if (parameter.Name == "__state") continue;
            Type? expected = parameter.Name switch
            {
                "__instance" => original.IsStatic ? null : original.DeclaringType,
                "__result" => original.ReturnType,
                "__runOriginal" => typeof(bool),
                string name when name.StartsWith("___") => targetType?.GetField(name[3..], flags)?.FieldType,
                _ => original.GetParameters().FirstOrDefault(value => value.Name == parameter.Name)?.ParameterType
            };
            Type actual = Unwrap(parameter.ParameterType);
            if (expected == null || !actual.IsAssignableFrom(Unwrap(expected)))
                failures.Add($"{label}: injected parameter {parameter.Name} ({actual}) does not match {original}.");
        }
    }
}
if (checkedPatches == 0) failures.Add("No Harmony patches were checked.");

// Check compiled calls as well as Harmony attributes. A stale embedded library can
// compile successfully and still refer to methods removed from the current game.
using var resolver = new DefaultAssemblyResolver();
foreach (string directory in searchPaths) resolver.AddSearchDirectory(directory);
using AssemblyDefinition compiled = AssemblyDefinition.ReadAssembly(args[2], new ReaderParameters { AssemblyResolver = resolver });
int checkedReferences = 0;
foreach (TypeDefinition type in compiled.MainModule.GetTypes())
foreach (MethodDefinition method in type.Methods.Where(method => method.HasBody))
foreach (Instruction instruction in method.Body.Instructions)
{
    if (instruction.Operand is not MemberReference member || member.DeclaringType?.Scope?.Name.StartsWith("assembly_") != true) continue;
    checkedReferences++;
    if (member is MethodReference call && call.Resolve() == null)
        failures.Add($"{method.FullName}: missing game method {member.FullName}.");
    if (member is FieldReference field)
    {
        FieldDefinition? definition = field.Resolve();
        if (definition == null || definition.IsLiteral && instruction.OpCode == OpCodes.Ldsfld)
            failures.Add($"{method.FullName}: missing or no longer loadable game field {member.FullName}.");
    }
}

foreach (string failure in failures) Console.Error.WriteLine(failure);
Console.WriteLine($"Checked {checkedPatches} Harmony patches and {checkedReferences} game references; {failures.Count} failures.");
return failures.Count == 0 ? 0 : 1;

static Type Unwrap(Type type) => type.IsByRef && type.GetElementType() is Type element ? element : type;
