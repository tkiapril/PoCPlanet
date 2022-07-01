using System.Collections.Immutable;
using System.Reflection;
using Bencodex.Types;

namespace PoCPlanet;

public interface IAction : IEquatable<IAction>
{
    public static readonly byte[] ActionTypeIdKey = { Convert.ToByte('@') };
    public static readonly byte[] ValuesKey = { Convert.ToByte('#') };
    public static string ActionTypeId => throw new NotImplementedException();

    public static IAction Deserialize(Dictionary data)
    {
        var subclasses = from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where type != typeof(IAction) && typeof(IAction).IsAssignableFrom(type)
            select type;
        foreach (var subclass in subclasses)
        {
            try
            {
                return (IAction)subclass.GetMethod("Deserialize", new[] { typeof(Dictionary) })!
                    .Invoke(null, new object[] { data })! ?? throw new InvalidOperationException();
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException!;
            }
            catch (ArgumentException)
            {
            }
        }

        throw new ArgumentException("No suitable class found for deserialization");
    }

    public Dictionary Serialize();

    public ImmutableHashSet<Address> RequestStates(Address from, Address to) =>
        ImmutableHashSet.Create(from, to);

    public ImmutableDictionary<Address, Dictionary> Execute(
        Address from,
        Address to,
        ImmutableDictionary<Address, Dictionary> states
        );
}

public class ActionError : ArgumentException
{
    public ActionError(string? message) : base(message)
    {
    }
}

public static class StateUtil
{
    public static string ToString(ImmutableDictionary<Address, Dictionary> state) =>
        string.Join(Environment.NewLine, state);
}