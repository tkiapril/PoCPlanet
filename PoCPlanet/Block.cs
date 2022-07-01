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
    public static readonly byte[] IndexKey = { Convert.ToByte('i') };
    public static readonly byte[] DifficultyKey = { Convert.ToByte('d') };
    public static readonly byte[] TimestampKey = { Convert.ToByte('t') };
    public static readonly byte[] TransactionsKey = { Convert.ToByte('T') };
    public static readonly byte[] NonceKey = { Convert.ToByte('n') };
    public static readonly byte[] RewardBeneficiaryKey = { Convert.ToByte('r') };
    public static readonly byte[] PreviousHashKey = { Convert.ToByte('p') };
    public static readonly byte[] HashKey = { Convert.ToByte('h') };

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
                throw new BlockIndexError($"Index must be 0 or above, but the index is {Index}");
            case < 1 when Difficulty != 0:
                throw new BlockDifficultyError(
                    $"Difficulty must be 0 for the genesis block but the difficulty is {Difficulty}"
                );
            case < 1 when PreviousHash is not null:
                throw new BlockPreviousHashError("Previous hash must be empty for the genesis block");
            case < 1:
                break;
            default:
            {
                if (Difficulty < 1)
                {
                    throw new BlockDifficultyError(
                        $"Difficulty must be above 0 except the genesis block but the difficulty is {Difficulty}"
                    );
                }

                if (PreviousHash is null)
                {
                    throw new BlockPreviousHashError("Previous hash must be present except for the genesis block");
                }

                break;
            }
        }

        if (!Hashcash.HasLeadingZeroBits(Hash, Difficulty))
        {
            throw new BlockNonceError(
                $"Hash {Hash} with the nonce {Nonce} does not satisfy the difficulty level {Difficulty}"
                );
        }
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => Hash.ToString();
}

public class BlockError : ArgumentException
{
    public BlockError(string? message) : base(message)
    {
    }
}

public class BlockHashError : BlockError
{
    public BlockHashError(string? message) : base(message)
    {
    }
}

public class BlockIndexError : BlockError
{
    public BlockIndexError(string? message) : base(message)
    {
    }
}

public class BlockDifficultyError : BlockError
{
    public BlockDifficultyError(string? message) : base(message)
    {
    }
}

public class BlockPreviousHashError : BlockError
{
    public BlockPreviousHashError(string? message) : base(message)
    {
    }
}

public class BlockNonceError : BlockError
{
    public BlockNonceError(string? message) : base(message)
    {
    }
}

public class BlockTimestampError : BlockError
{
    public BlockTimestampError(string? message) : base(message)
    {
    }
}
