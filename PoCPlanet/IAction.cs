using Bencodex.Types;

namespace PoCPlanet;

public interface IAction
{
    public Dictionary Serialize();
}