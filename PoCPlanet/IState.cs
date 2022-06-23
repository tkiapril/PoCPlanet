using Bencodex.Types;

namespace PoCPlanet;

public interface IState
{
    public static IState Deserialize(Dictionary data) => throw new NotImplementedException();
    public Dictionary Serialize();
}