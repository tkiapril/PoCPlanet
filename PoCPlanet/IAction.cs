using System.Collections.Immutable;
using Bencodex.Types;

namespace PoCPlanet;

public interface IAction : IEquatable<IAction>
{
    public static string ActionTypeId => throw new NotImplementedException();
    public static IAction Deserialize(Dictionary data) => throw new NotImplementedException();
    public Dictionary Serialize();

    public ImmutableHashSet<Address> RequestStates(Address from, Address to) =>
        ImmutableHashSet.Create(from, to);

    public ImmutableDictionary<Address, Dictionary> Execute(
        Address from,
        Address to,
        ImmutableDictionary<Address, Dictionary> states
        );
}

public static class StateUtil
{
    public static string ToString(ImmutableDictionary<Address, Dictionary> state) =>
        string.Join(Environment.NewLine, state);
}