using System.Collections.Immutable;
using Bencodex.Types;

namespace PoCPlanet;

public interface IAction
{
    public static string ActionTypeId => throw new NotImplementedException();
    public static IAction Deserialize(Dictionary data) => throw new NotImplementedException();
    public Dictionary Serialize();

    public ImmutableHashSet<Address> RequestStates(Address from, Address to) =>
        ImmutableHashSet.Create(from, to);

    public Dictionary<Address, Dictionary> Execute(
        Address from,
        Address to,
        Dictionary<Address, Dictionary> states
        );
}