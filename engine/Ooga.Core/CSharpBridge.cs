using System.Collections;
using System.Reflection;

namespace Ooga;

// The door from ooga into C#: any .NET type, method, property or field.
//
//   me csharp "System.Math" "Sqrt" 16              -> 4          (static method)
//   me csharp "System.Math" "PI"                   -> 3.14159... (static property or field)
//   me has sb me csharp_new "System.Text.StringBuilder"
//   me csharp_call sb "Append" "hi"                             (method on a thing)
//   me csharp_get "hello" "Length"                 -> 5          (property or field on a thing)
//   me csharp_set thing "Name" "grok"                           (change a property or field)
//
// Ooga numbers, texts, yes/no, nothing and lists are turned into what C# wants, and back again.
public static class CSharpBridge
{
    const BindingFlags Static = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
    const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;
    const int MaxListSize = 1_000_000;

    static readonly Dictionary<string, Type> typeCache = new();

    public static IEnumerable<OogaAction> Actions()
    {
        yield return new("csharp", 2, -1, c =>
        {
            var type = TypeOf(c, c.Text(0));
            string member = c.Text(1);
            var args = c.Things.Skip(2).ToArray();
            if (args.Length == 0 && GetMember(type, member, Static) is MemberInfo m)
                return Read(c, m, null);
            return Invoke(c, type, null, member, args, Static);
        });
        yield return new("csharp_new", 1, -1, c =>
        {
            var type = TypeOf(c, c.Text(0));
            var args = c.Things.Skip(1).ToArray();
            var (ctor, converted) = Choose(c, type.GetConstructors(Instance), args);
            if (ctor == null)
            {
                if (args.Length == 0 && type.IsValueType) return FromCSharp(c, Activator.CreateInstance(type), keepCollections: true);
                throw c.Problem($"csharp_new no find a way to make {type.FullName} from {Things(args.Length)}. {Choices(type.GetConstructors(Instance), type.Name)}");
            }
            // A made thing stays a C# thing (even a C# List), so its methods can be called later.
            return FromCSharp(c, Catch(c, () => ((ConstructorInfo)ctor).Invoke(converted)), keepCollections: true);
        });
        yield return new("csharp_call", 2, -1, c =>
        {
            object target = ToTarget(c, c[0]);
            return Invoke(c, target.GetType(), target, c.Text(1), c.Things.Skip(2).ToArray(), Instance);
        });
        yield return new("csharp_get", 2, c =>
        {
            object target = ToTarget(c, c[0]);
            string member = c.Text(1);
            var m = GetMember(target.GetType(), member, Instance)
                    ?? throw c.Problem($"{target.GetType().Name} no have property or field \"{member}\".");
            return Read(c, m, target);
        });
        yield return new("csharp_set", 3, c =>
        {
            object target;
            Type type;
            BindingFlags flags;
            if (c[0] is string typeName && FindType(typeName) is Type t && GetMember(t, c.Text(1), Static) != null)
            {
                (target, type, flags) = (null, t, Static);
            }
            else
            {
                target = ToTarget(c, c[0]);
                (type, flags) = (target.GetType(), Instance);
            }
            string member = c.Text(1);
            switch (GetMember(type, member, flags))
            {
                case PropertyInfo p when p.CanWrite:
                    Catch(c, () => { p.SetValue(target, Convert(c, c[2], p.PropertyType)); return null; });
                    return null;
                case FieldInfo f when !f.IsInitOnly && !f.IsLiteral:
                    Catch(c, () => { f.SetValue(target, Convert(c, c[2], f.FieldType)); return null; });
                    return null;
                case null:
                    throw c.Problem($"{type.Name} no have property or field \"{member}\".");
                default:
                    throw c.Problem($"{type.Name}.{member} can only be read, not changed.");
            }
        });
    }

    // How "say" shows a C# thing: its own ToString.
    public static string ShowThing(object v)
    {
        try
        {
            return v.ToString() ?? v.GetType().FullName;
        }
        catch (Exception)
        {
            return v.GetType().FullName;
        }
    }

    // ---------- finding types and members ----------

    public static Type FindType(string name)
    {
        lock (typeCache)
        {
            if (typeCache.TryGetValue(name, out var cached)) return cached;
            var found = Search(name) ?? (name.Contains('.') ? null : Search("System." + name));
            if (found != null) typeCache[name] = found;
            return found;
        }
    }

    static Type Search(string name)
    {
        var t = Type.GetType(name, throwOnError: false);
        if (t != null) return t;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(name, throwOnError: false);
            if (t != null) return t;
        }
        // Not loaded yet: try assemblies named after the namespace, like System.Net.Http for System.Net.Http.HttpClient.
        var parts = name.Split('.');
        for (int i = parts.Length - 1; i >= 1; i--)
        {
            try
            {
                var asm = Assembly.Load(new AssemblyName(string.Join('.', parts, 0, i)));
                t = asm.GetType(name, throwOnError: false);
                if (t != null) return t;
            }
            catch (Exception e) when (e is IOException or BadImageFormatException or ArgumentException)
            {
            }
        }
        return null;
    }

    static Type TypeOf(ActionCall c, string name) =>
        FindType(name) ?? throw c.Problem($"ooga no find C# type \"{name}\". write the full name, like \"System.Math\".");

    static MemberInfo GetMember(Type type, string name, BindingFlags flags) =>
        (MemberInfo)type.GetProperty(name, flags | BindingFlags.IgnoreCase) is PropertyInfo p && p.GetIndexParameters().Length == 0
            ? p
            : type.GetField(name, flags | BindingFlags.IgnoreCase);

    static object Read(ActionCall c, MemberInfo m, object target) => m switch
    {
        PropertyInfo p => FromCSharp(c, Catch(c, () => p.GetValue(target))),
        FieldInfo f => FromCSharp(c, Catch(c, () => f.GetValue(target))),
        _ => throw c.Problem("ooga no can read that."),
    };

    static object ToTarget(ActionCall c, object v) =>
        v ?? throw c.Problem($"{c.Name} need a thing to work on, but got nothing.");

    // ---------- calling ----------

    static object Invoke(ActionCall c, Type type, object target, string name, object[] args, BindingFlags flags)
    {
        var methods = type.GetMethods(flags).Where(m => m.Name == name).ToArray();
        if (methods.Length == 0)
            methods = type.GetMethods(flags).Where(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        // Generic methods (like Array.Find<T>) get their T guessed from the things handed in.
        methods = methods.Select(m => m.IsGenericMethodDefinition ? CloseGeneric(m, args) : m).Where(m => m != null).ToArray();
        if (methods.Length == 0)
            throw c.Problem($"{type.Name} no have {(target == null ? "static " : "")}method \"{name}\".");

        var (method, converted) = Choose(c, methods, args);
        if (method == null)
            throw c.Problem($"{type.Name}.{name} no take {Things(args.Length)} like that. {Choices(methods, name)}");
        return FromCSharp(c, Catch(c, () => method.Invoke(target, converted)));
    }

    // Guesses the T in a generic method from the ooga things: numbers -> double, texts -> string, a list of numbers -> double, ...
    static MethodInfo CloseGeneric(MethodInfo m, object[] args)
    {
        var generic = m.GetGenericArguments();
        var chosen = new Type[generic.Length];
        var ps = m.GetParameters();
        for (int g = 0; g < generic.Length; g++)
        {
            for (int i = 0; i < ps.Length && i < args.Length && chosen[g] == null; i++)
            {
                var pt = ps[i].ParameterType;
                if (pt == generic[g])
                    chosen[g] = ClrTypeOf(args[i]);
                else if (args[i] is OogaList list
                         && ((pt.IsArray && pt.GetElementType() == generic[g])
                             || (pt.IsGenericType && pt.GetGenericArguments().Length == 1 && pt.GetGenericArguments()[0] == generic[g])))
                    chosen[g] = CommonType(list.Items);
            }
            chosen[g] ??= typeof(object);
        }
        try
        {
            return m.MakeGenericMethod(chosen);
        }
        catch (ArgumentException)
        {
            return null;   // the guess breaks a rule of the method (a "where T : ..." rule)
        }
    }

    static Type ClrTypeOf(object v) => v switch
    {
        null => typeof(object),
        double => typeof(double),
        string => typeof(string),
        bool => typeof(bool),
        OogaList or OogaBox or ActionValue => typeof(object),
        _ => v.GetType(),
    };

    static Type CommonType(List<object> items)
    {
        if (items.Count == 0) return typeof(object);
        var first = ClrTypeOf(items[0]);
        return items.All(x => ClrTypeOf(x) == first) ? first : typeof(object);
    }

    // Picks the method (or constructor) whose things fit best.
    static (MethodBase, object[]) Choose(ActionCall c, IEnumerable<MethodBase> candidates, object[] args)
    {
        MethodBase best = null;
        object[] bestArgs = null;
        int bestScore = int.MaxValue;
        foreach (var m in candidates)
        {
            var ps = m.GetParameters();
            if (args.Length > ps.Length || args.Length < ps.Count(p => !p.IsOptional)) continue;
            var converted = new object[ps.Length];
            int score = 0;
            bool ok = true;
            for (int i = 0; i < ps.Length && ok; i++)
            {
                if (i >= args.Length) { converted[i] = ps[i].DefaultValue; score += 1; continue; }
                ok = TryConvert(c, args[i], ps[i].ParameterType, out converted[i], out int s);
                score += s;
            }
            if (ok && score < bestScore) (best, bestArgs, bestScore) = (m, converted, score);
        }
        return (best, bestArgs);
    }

    static string Choices(IEnumerable<MethodBase> methods, string name)
    {
        var shown = methods.Take(6).Select(m => name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name)) + ")");
        return "it can take: " + string.Join("  ", shown);
    }

    static string Things(int n) => n == 1 ? "1 thing" : $"{n} things";

    static object Catch(ActionCall c, Func<object> work)
    {
        try
        {
            return work();
        }
        catch (TargetInvocationException e) when (e.InnerException is OogaError or DieSignal or OperationCanceledException)
        {
            // Something from ooga itself (an ooga action called by C#): let it keep going as it is.
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            throw c.Problem($"C# say problem: {e.InnerException.Message}");
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or NotSupportedException
                                       or MemberAccessException or TargetException)
        {
            throw c.Problem($"C# say problem: {e.Message}");
        }
    }

    // ---------- ooga -> C# ----------

    static object Convert(ActionCall c, object v, Type target) =>
        TryConvert(c, v, target, out var result, out _)
            ? result
            : throw c.Problem($"no can turn {Values.Describe(v)} into C# {target.Name}.");

    // score: 0 = perfect fit, bigger = worse fit.
    static bool TryConvert(ActionCall c, object v, Type target, out object result, out int score)
    {
        result = null;
        score = 0;
        var under = Nullable.GetUnderlyingType(target);
        if (v == null)
            return !target.IsValueType || under != null;
        if (under != null) target = under;
        if (target.IsByRef) return false;

        if (target.IsInstanceOfType(v) && target != typeof(object))
        {
            result = v;
            return true;
        }

        switch (v)
        {
            case double d:
                if (target == typeof(double)) { result = d; return true; }
                if (target == typeof(float)) { result = (float)d; score = 1; return true; }
                if (target == typeof(decimal)) { result = (decimal)d; score = 1; return true; }
                if (target.IsEnum && d == Math.Floor(d)) { result = Enum.ToObject(target, (long)d); score = 2; return true; }
                if (IsWholeType(target))
                {
                    if (d != Math.Floor(d)) return false;
                    try
                    {
                        result = System.Convert.ChangeType(d, target, System.Globalization.CultureInfo.InvariantCulture);
                        score = target == typeof(int) ? 1 : 2;
                        return true;
                    }
                    catch (OverflowException) { return false; }
                }
                break;
            case string s:
                if (target == typeof(char) && s.Length == 1) { result = s[0]; score = 1; return true; }
                if (target.IsEnum && Enum.TryParse(target, s, ignoreCase: true, out var e)) { result = e; score = 1; return true; }
                if (target == typeof(Type) && FindType(s) is Type t) { result = t; score = 2; return true; }
                break;
            case ActionValue action when typeof(Delegate).IsAssignableFrom(target) && target != typeof(Delegate)
                                         && target != typeof(MulticastDelegate):
                result = MakeDelegate(c, action, target);
                score = 1;
                return result != null;
            case OogaList list:
            {
                Type item = target.IsArray ? target.GetElementType()
                    : target.IsGenericType && target.GetGenericArguments().Length == 1
                        && target.IsAssignableFrom(typeof(List<>).MakeGenericType(target.GetGenericArguments()[0]))
                        ? target.GetGenericArguments()[0]
                    : target == typeof(object) || target == typeof(IEnumerable) ? typeof(object)
                    : null;
                if (item == null) break;
                var array = Array.CreateInstance(item, list.Items.Count);
                for (int i = 0; i < list.Items.Count; i++)
                {
                    if (!TryConvert(c, list.Items[i], item, out var x, out int s)) return false;
                    array.SetValue(x, i);
                    score += s;
                }
                if (target.IsArray || target == typeof(object) || target == typeof(IEnumerable))
                    result = array;
                else
                {
                    var typed = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(item));
                    foreach (var x in array) typed.Add(x);
                    result = typed;
                }
                score += 2;
                return true;
            }
        }

        if (target == typeof(object))
        {
            result = v;
            score = 5;
            return true;
        }
        return false;
    }

    // Wraps an ooga action as a C# delegate (Func, Action, Predicate, Comparison, ...).
    // When C# calls it, the things are turned into ooga values, the action runs, and its answer goes back to C#.
    static Delegate MakeDelegate(ActionCall c, ActionValue action, Type delegateType)
    {
        var invokeMethod = delegateType.GetMethod("Invoke");
        if (invokeMethod == null || invokeMethod.GetParameters().Any(p => p.ParameterType.IsByRef)) return null;
        var ps = invokeMethod.GetParameters()
            .Select(p => System.Linq.Expressions.Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        var bridge = new DelegateBridge(c, action, invokeMethod.ReturnType);
        var args = System.Linq.Expressions.Expression.NewArrayInit(typeof(object),
            ps.Select(p => System.Linq.Expressions.Expression.Convert(p, typeof(object))));
        System.Linq.Expressions.Expression body = System.Linq.Expressions.Expression.Call(
            System.Linq.Expressions.Expression.Constant(bridge), typeof(DelegateBridge).GetMethod(nameof(DelegateBridge.Run)), args);
        if (invokeMethod.ReturnType != typeof(void))
            body = System.Linq.Expressions.Expression.Convert(body, invokeMethod.ReturnType);
        return System.Linq.Expressions.Expression.Lambda(delegateType, body, ps).Compile();
    }

    sealed class DelegateBridge
    {
        readonly ActionCall call;
        readonly ActionValue action;
        readonly Type returns;

        public DelegateBridge(ActionCall call, ActionValue action, Type returns)
        {
            this.call = call;
            this.action = action;
            this.returns = returns;
        }

        public object Run(object[] things)
        {
            var oogaThings = things.Select(t => FromCSharp(call, t)).ToArray();
            object answer = call.Invoke(action, oogaThings);
            if (returns == typeof(void)) return null;
            if (!TryConvert(call, answer, returns, out var result, out _))
                throw call.Problem($"action {action.Name} gave {Values.Describe(answer)}, but C# want {returns.Name}.");
            return result;
        }
    }

    static bool IsWholeType(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
        || t == typeof(sbyte) || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort);

    // ---------- C# -> ooga ----------

    // keepCollections: leave C# lists and dictionaries as C# things instead of turning them into ooga lists and boxes.
    public static object FromCSharp(ActionCall c, object r, bool keepCollections = false)
    {
        if (keepCollections && r is IEnumerable and not string) return r;
        switch (r)
        {
            case null: return null;
            case double d: return Finite(c, d);
            case float f: return Finite(c, f);
            case int or long or short or byte or sbyte or uint or ulong or ushort or decimal:
                return System.Convert.ToDouble(r, System.Globalization.CultureInfo.InvariantCulture);
            case bool or string: return r;
            case char ch: return ch.ToString();
            case Enum e: return e.ToString();
            case Task task:
            {
                Catch(c, () => { task.GetAwaiter().GetResult(); return null; });
                var t = task.GetType();
                if (t.IsGenericType && t.GetProperty("Result") is PropertyInfo p && p.PropertyType.Name != "VoidTaskResult")
                    return FromCSharp(c, p.GetValue(task));
                return null;
            }
            case IDictionary dict:
            {
                var box = new OogaBox();
                foreach (DictionaryEntry entry in dict)
                {
                    var key = FromCSharp(c, entry.Key);
                    if (key is not (double or string or bool)) key = Values.Show(key);
                    box.Set(key, FromCSharp(c, entry.Value));
                }
                return box;
            }
            case IEnumerable:   // arrays, lists, and LINQ answers become ooga lists
            {
                var list = new OogaList();
                foreach (var x in (IEnumerable)r)
                {
                    if (list.Items.Count >= MaxListSize) throw c.Problem($"C# list too big (more than {MaxListSize} things).");
                    list.Items.Add(FromCSharp(c, x));
                }
                return list;
            }
            default:
                return r;   // a C# thing: keep it as it is, so it can be handed back to csharp_call / csharp_get
        }
    }

    static object Finite(ActionCall c, double d) =>
        double.IsFinite(d) ? d : throw c.Problem("C# gave a number ooga no can hold (too big, or not a number).");
}
