using System.Collections.Immutable;
using System.Reflection;
using Bencodex.Types;
using Libplanet.Crypto;

namespace PoCPlanet;

[Serializable()]
public record InitializeAction(Address Address) : IAction
{
    public static string ActionTypeId => "init";
    
    public static readonly byte[] AddressKey = { Convert.ToByte('p') };
    
    public ImmutableHashSet<Address> RequestStates(Address from, Address to) =>
        ImmutableHashSet.Create(Address);
    
    public bool Equals(IAction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return other is InitializeAction action
            && action.Address.Equals(Address);
    }

    public Dictionary Serialize() =>
        Dictionary.Empty
            .Add(IAction.ActionTypeIdKey, ActionTypeId)
            .Add(
                IAction.ValuesKey, 
                Dictionary.Empty
                    .Add(AddressKey, Address)
            );

    public static InitializeAction Deserialize(Dictionary data)
    {
        if (data.GetValue<Text>(IAction.ActionTypeIdKey) != ActionTypeId)
        {
            throw new ArgumentException(
                $"Input data does not match the type {MethodBase.GetCurrentMethod()!.DeclaringType}"
            );
        }
        
        data = data.GetValue<Dictionary>(IAction.ValuesKey);

        return new InitializeAction(new Address(data.GetValue<Binary>(AddressKey).ToByteArray()));
    }
    
    public ImmutableDictionary<Address, Dictionary> Execute(
        Address from,
        Address to,
        ImmutableDictionary<Address, Dictionary> states
        )
    {
        if (!states.ContainsKey(Address) || states[Address].Equals(Dictionary.Empty))
        {
            states = states.Remove(Address).Add(Address, new Balance(1000000, Address).Serialize());
        }

        return states;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine("init", Address);
    }
}