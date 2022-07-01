using System.Collections.Immutable;
using Libplanet.Crypto;

namespace PoCPlanet.Tests;

public class FixtureBase
{
    protected static readonly Block GenesisBlock = new Block(
        Index: 0,
        Difficulty: 0,
        Nonce: new Nonce(Array.Empty<byte>()),
        RewardBeneficiary: null,
        PreviousHash: null,
        Timestamp: new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Transactions: ImmutableArray<Transaction>.Empty
        );

    protected static readonly PrivateKey PrivateKey = new PrivateKey(
        "15EE8825EEE52F7DC87740F1D1DDD18585FD554EC2B6006491B8CC90DDD35C9B"
    );

    protected static readonly PublicKey PublicKey = PrivateKey.PublicKey;

    protected static readonly Address Address = new Address(PublicKey);

    protected static readonly PrivateKey RecipientPrivateKey = new PrivateKey(
        "0E5A41D3E614602AB5CD954D45F11C111D40BF07B12F41419D4F40F976704D6F"
    );

    protected static readonly PublicKey RecipientPublicKey = RecipientPrivateKey.PublicKey;

    protected static readonly Address RecipientAddress = new Address(RecipientPublicKey);

    protected static Blockchain Blockchain
    {
        get
        {
            IStore memoryStore = new MemoryStore();
            var bc = new Blockchain(ref memoryStore);
            bc.Append(GenesisBlock);
            bc.Append(
                Block.Mine(
                    index: 1,
                    difficulty: 1,
                    rewardBeneficiary: Address,
                    previousHash: GenesisBlock.Hash,
                    timestamp: new DateTime(2018, 1, 1, 0, 0, 1, DateTimeKind.Utc),
                    transactions: ImmutableArray<Transaction>.Empty
                )
            );
            bc.Append(
                Block.Mine(
                    index: 2,
                    difficulty: 2,
                    rewardBeneficiary: Address,
                    previousHash: bc.Last().Hash,
                    timestamp: new DateTime(2018, 1, 1, 0, 0, 2, DateTimeKind.Utc),
                    transactions: ImmutableArray<Transaction>.Empty
                )
            );
            bc.Append(
                Block.Mine(
                    index: 3,
                    difficulty: 3,
                    rewardBeneficiary: Address,
                    previousHash: bc.Last().Hash,
                    timestamp: new DateTime(2018, 1, 1, 0, 0, 3, DateTimeKind.Utc),
                    transactions: ImmutableArray<Transaction>.Empty
                )
            );
            bc.Append(
                Block.Mine(
                    index: 4,
                    difficulty: 4,
                    rewardBeneficiary: Address,
                    previousHash: bc.Last().Hash,
                    timestamp: new DateTime(2018, 1, 1, 0, 0, 10, DateTimeKind.Utc),
                    transactions: ImmutableArray<Transaction>.Empty
                        .Add(
                            new Transaction(
                                Sender: Address,
                                PublicKey: PublicKey,
                                Signature: new Signature(
                                    new byte[]
                                    {
                                        0x30, 0x44, 0x02, 0x20, 0x2C, 0x04, 0x14, 0x74, 0x44, 0xA0, 0xB7, 0xB0, 0x91,
                                        0x42,
                                        0x1E, 0x31, 0x9F, 0x78, 0xCE, 0x77, 0x10, 0xF9, 0xDD, 0xAA, 0x85, 0x81, 0xC1,
                                        0xE2,
                                        0x4E, 0xB2, 0x47, 0x5E, 0x56, 0xF0, 0x39, 0xA3, 0x02, 0x20, 0x25, 0xF8, 0x8C,
                                        0x9A,
                                        0x9D, 0x20, 0x7E, 0x7E, 0x42, 0xB1, 0x2A, 0xC2, 0x11, 0x24, 0x82, 0x64, 0xDD,
                                        0xC6,
                                        0xDA, 0xB4, 0x47, 0xD7, 0x57, 0x51, 0xBF, 0xB9, 0x87, 0xC0, 0x06, 0xE7, 0x62,
                                        0x3A
                                    }
                                ),
                                Recipient: RecipientAddress,
                                Actions: ImmutableArray<IAction>.Empty,
                                Timestamp:
                                new DateTime(2018, 1, 1, 0, 0, 5, DateTimeKind.Utc)
                            )
                        )
                        .Add(
                            new Transaction(
                                Sender: RecipientAddress,
                                PublicKey: RecipientPublicKey,
                                Signature: new Signature(
                                    new byte[]
                                    {
                                        0x30, 0x44, 0x02, 0x20, 0x6C, 0xEF, 0x51, 0x17, 0x3F, 0x3A, 0x37, 0xF7, 0xBD,
                                        0x02,
                                        0xE8, 0x2D, 0xBF, 0xBE, 0x88, 0xB5, 0x55, 0xC4, 0xE8, 0xD1, 0x7B, 0x8B, 0xDF,
                                        0x5A,
                                        0x7C, 0xEE, 0x47, 0x42, 0xDD, 0x79, 0xD6, 0x7C, 0x02, 0x20, 0x7E, 0x91, 0x73,
                                        0x98,
                                        0xDD, 0xF0, 0x6E, 0x8E, 0x38, 0x3C, 0x2C, 0x46, 0xAD, 0xD9, 0x72, 0xC7, 0x07,
                                        0x40,
                                        0x94, 0x20, 0xA8, 0x44, 0xC4, 0x99, 0xA2, 0x96, 0xD3, 0x67, 0x2D, 0xF4, 0x59,
                                        0x31
                                    }
                                ),
                                Recipient: Address,
                                Actions: ImmutableArray<IAction>.Empty,
                                Timestamp:
                                new DateTime(2018, 1, 1, 0, 0, 6, DateTimeKind.Utc)
                            )
                        )
                )
            );
            return bc;
        }
    }

    protected static Transaction Transaction => Transaction.Make(
        PrivateKey,
        recipient: RecipientAddress,
        timestamp: new DateTime(2018, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc),
        actions: ImmutableArray<IAction>.Empty
    );

    private static readonly PrivateKey Transaction2PrivateKey =
        PrivateKey.FromString("1C5BE49B7E30A1757BCB2C1C147C8D955E0F114D8E89C3391AF7EC21747BEF39");

    protected static Transaction Transaction2 => Transaction.Make(
        privateKey: Transaction2PrivateKey,
        recipient: new Address(Transaction2PrivateKey.PublicKey),
        timestamp: new DateTime(2018, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc),
        actions: ImmutableArray<IAction>.Empty
    );
}