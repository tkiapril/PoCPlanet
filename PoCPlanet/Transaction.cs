using Libplanet.Crypto;

namespace PoCPlanet;

[Serializable()]
public record Transaction(
    Address Sender,
    PublicKey PublicKey,
    Signature Signature,
    Address Recipient,
    List<Action> Actions,
    DateTime Timestamp
)
{
}

public record Signature(byte[] Data);