using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;

namespace PoCPlanet;

[Serializable()]
public record Block(
    int Index,
    int Difficulty,
    Nonce Nonce,
    Address? RewardBeneficiary,
    Hash? PreviousHash,
    DateTime Timestamp,
    IEnumerable<Transaction> Transactions
)
{
    private static readonly byte[] IndexKey = { Convert.ToByte('i') };
    private static readonly byte[] DifficultyKey = { Convert.ToByte('d') };
    private static readonly byte[] TimestampKey = { Convert.ToByte('t') };
    private static readonly byte[] TransactionsKey = { Convert.ToByte('T') };
    private static readonly byte[] NonceKey = { Convert.ToByte('n') };
    private static readonly byte[] RewardBeneficiaryKey = { Convert.ToByte('r') };
    private static readonly byte[] PreviousHashKey = { Convert.ToByte('p') };
    private static readonly byte[] HashKey = { Convert.ToByte('h') };

    public static Block Mine(
        int index,
        int difficulty,
        Address? rewardBeneficiary,
        Hash? previousHash,
        DateTime timestamp,
        IEnumerable<Transaction> transactions
        )
    {
        Block MakeBlock(Nonce nonce)
        {
            return new Block(
                Index: index,
                Difficulty: difficulty,
                Nonce: nonce,
                RewardBeneficiary: rewardBeneficiary,
                PreviousHash: previousHash,
                Timestamp: timestamp,
                Transactions: transactions
            );
        }

        var nonce = Hashcash.Answer(
            (nonce) => MakeBlock(nonce).Bencode(hash: false, transactionData: true),
            difficulty
            );
        return MakeBlock(nonce);
    }

    public Dictionary Serialize(bool hash, bool transactionData)
    {
        var dict = Dictionary.Empty
            .Add(IndexKey, Index)
            .Add(TimestampKey, Timestamp.ToRfc3339())
            .Add(NonceKey, Nonce);
        dict = RewardBeneficiary is not null
            ? dict.Add(RewardBeneficiaryKey, RewardBeneficiary)
            : dict.Add(RewardBeneficiaryKey, Null.Value);

        dict = dict.Add(DifficultyKey, Difficulty);

        dict = PreviousHash is not null
            ? dict.Add(PreviousHashKey, PreviousHash)
            : dict.Add(PreviousHashKey, Null.Value);

        if (hash)
        {
            dict = dict.Add(HashKey, Hash);
        }

        if (transactionData)
        {
            dict = dict.Add(TransactionsKey, from tx in Transactions select tx.Serialize(sign: true));
        }
        else
        {
            dict = dict.Add(
                TransactionsKey,
                (IEnumerable<IValue>)(from tx in Transactions select tx.Id)
                .Aggregate(List.Empty, (current, id) => current.Add(id))
                );
        }

        return dict;
    }

    public Hash Hash =>
        new (SHA256.Create().ComputeHash(Bencode(hash: false, transactionData: true)));

    public byte[] Bencode(bool hash, bool transactionData) => new Codec().Encode(Serialize(hash, transactionData));

    public void Validate()
    {
        switch (Index)
        {
            case < 0:
                throw new Exception($"Index must be 0 or above, but the index is {Index}");
            case < 1 when Difficulty != 0:
                throw new Exception(
                    $"Difficulty must be 0 for the genesis block but the difficulty is {Difficulty}"
                );
            case < 1 when PreviousHash is not null:
                throw new Exception("Previous hash must be empty for the genesis block");
            case < 1:
                break;
            default:
            {
                if (Difficulty < 1)
                {
                    throw new Exception(
                        $"Difficulty must be above 0 except the genesis block but the difficulty is {Difficulty}"
                    );
                }

                if (PreviousHash is null)
                {
                    throw new Exception("Previous hash must be present except for the genesis block");
                }

                break;
            }
        }

        if (!Hashcash.HasLeadingZeroBits(Hash, Difficulty))
        {
            throw new Exception(
                $"Hash {Hash} with the nonce {Nonce} does not satisfy the difficulty level {Difficulty}"
                );
        }
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => Hash.ToString();
}