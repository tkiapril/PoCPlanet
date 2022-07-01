using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Crypto;
using static NUnit.Framework.Assert;

namespace PoCPlanet.Tests;

public class BlockchainTest : FixtureBase
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TransactionSetGet()
    {
        IStore store = new MemoryStore();
        var transactionSet = new TransactionSet(ref store);
        Multiple(() =>
        {
            Throws<KeyNotFoundException>(() => _ = transactionSet[Transaction.Id]);
            Throws<KeyNotFoundException>(() => _ = transactionSet[Transaction2.Id]);
        });
        
        store.PutTransaction(Transaction);
        Multiple(() =>
        {
            That(transactionSet[Transaction.Id], Is.EqualTo(Transaction));
            Throws<KeyNotFoundException>(() => _ = transactionSet[Transaction2.Id]);
        });
        
        store.PutTransaction(Transaction2);
        Multiple(() =>
        {
            That(transactionSet[Transaction.Id], Is.EqualTo(Transaction));
            That(transactionSet[Transaction2.Id], Is.EqualTo(Transaction2));
        });
    }

    [Test]
    public void TransactionSetSet()
    {
        IStore store = new MemoryStore();
        var transactionSet = new TransactionSet(ref store);
        
        That(store.CountTransactions(), Is.Zero);

        transactionSet[Transaction.Id] = Transaction;
        Multiple(() =>
        {
            That(store.CountTransactions(), Is.EqualTo(1));
            That(store.IterateTransactionIds(), Is.EqualTo(
                ImmutableHashSet<TxId>.Empty
                    .Add(Transaction.Id)
            ));
            That(store.GetTransaction(Transaction.Id), Is.EqualTo(Transaction));
        });

        transactionSet[Transaction2.Id] = Transaction2;
        Multiple(() =>
        {
            That(store.CountTransactions(), Is.EqualTo(2));
            That(store.IterateTransactionIds(), Is.EqualTo(
                ImmutableHashSet<TxId>.Empty
                    .Add(Transaction.Id)
                    .Add(Transaction2.Id)
            ));
            That(store.GetTransaction(Transaction2.Id), Is.EqualTo(Transaction2));

            Throws<TransactionIdError>(() => transactionSet[Transaction.Id] = Transaction2);
        });
    }

    [Test]
    public void TransactionSetRemove()
    {
        IStore store = new MemoryStore();
        var transactionSet = new TransactionSet(ref store);
        
        store.PutTransaction(Transaction);
        store.PutTransaction(Transaction2);

        transactionSet.Remove(Transaction.Id);
        Multiple(() =>
        {
            Throws<KeyNotFoundException>(() => _ = transactionSet[Transaction.Id]);
            Throws<KeyNotFoundException>(() => transactionSet.Remove(Transaction.Id));
            That(transactionSet[Transaction2.Id], Is.EqualTo(Transaction2));
            That(store.GetTransaction(Transaction.Id), Is.Null);
            That(store.GetTransaction(Transaction2.Id), Is.EqualTo(Transaction2));
        });

        Multiple(() =>
        {
            transactionSet.Remove(Transaction2.Id);
            Throws<KeyNotFoundException>(() => _ = transactionSet[Transaction2.Id]);
            Throws<KeyNotFoundException>(() => transactionSet.Remove(Transaction2.Id));
            Throws<KeyNotFoundException>(() => _ = transactionSet[Transaction2.Id]);
            Throws<KeyNotFoundException>(() => transactionSet.Remove(Transaction2.Id));
            That(store.GetTransaction(Transaction.Id), Is.Null);
            That(store.GetTransaction(Transaction2.Id), Is.Null);
        });
    }

    [Test]
    public void TransactionSetCount()
    {
        IStore store = new MemoryStore();
        var transactionSet = new TransactionSet(ref store);
        That(transactionSet.Count, Is.Zero);
        
        store.PutTransaction(Transaction);
        That(transactionSet.Count, Is.EqualTo(1));
        
        store.PutTransaction(Transaction2);
        That(transactionSet.Count, Is.EqualTo(2));
    }

    [Test]
    public void TransactionSetIter()
    {
        IStore store = new MemoryStore();
        var transactionSet = new TransactionSet(ref store);
        
        That(transactionSet.Keys.ToImmutableHashSet(), Is.EqualTo(ImmutableHashSet<TxId>.Empty));
        
        store.PutTransaction(Transaction);
        That(
            transactionSet.Keys.ToImmutableHashSet(), 
            Is.EqualTo(
                ImmutableHashSet<TxId>.Empty
                    .Add(Transaction.Id)
            )
        );
        
        store.PutTransaction(Transaction2);
        That(
            transactionSet.Keys.ToImmutableHashSet(), 
            Is.EqualTo(
                ImmutableHashSet<TxId>.Empty
                    .Add(Transaction.Id)
                    .Add(Transaction2.Id)
            )
        );
    }

    private PrivateKey PrivateKey2 => 
        new PrivateKey("918BE0C29D739D3B40D54CB6E4F07AEFB7105718B7868BFA201C0AC5F6FA6CBE");

    private Address Address2 => new Address(PrivateKey2.PublicKey);

    [Test]
    public void GetStates()
    {
        var second = 0;
        IStore store = new MemoryStore();
        var blockchain = new Blockchain(ref store);
        blockchain.Append(
            GenesisBlock with
            {
                Transactions = ImmutableArray<Transaction>.Empty
                    .Add(
                        Transaction.Make(
                            PrivateKey,
                            recipient: Address,
                            timestamp: new DateTime(
                                2017, 12, 31, 23, 59, 59, DateTimeKind.Utc
                                ),
                            actions: ImmutableArray<IAction>.Empty
                                .Add(new InitializeAction(Address))
                                .Add(new InitializeAction(Address2))
                            )
                        )
            }
            );

        void MakeTx(PrivateKey privateKey, IAction action)
        {
            var tx1 = Transaction.Make(
                privateKey,
                recipient: RecipientAddress,
                timestamp: new DateTime(2018, 1, 1, 0, 0, second, DateTimeKind.Utc),
                actions: ImmutableArray<IAction>.Empty
                    .Add(action)
            );
            blockchain!.StageTransactions(
                ImmutableHashSet<Transaction>.Empty
                    .Add(tx1)
                );
            blockchain.MineBlock(Address);
            second += 10;
        }

        var states = blockchain.GetStates(
            ImmutableArray<Address>.Empty.Add(Address)
        );
        var initState = new Balance(1000000, Address);
        That(states[Address], Is.EqualTo(initState.Serialize()));
        MakeTx(PrivateKey, new TransferAction(PublicKey, RecipientAddress, 100));
        MakeTx(PrivateKey, new TransferAction(PublicKey, RecipientAddress, 200));
        MakeTx(PrivateKey2, new TransferAction(PrivateKey2.PublicKey, RecipientAddress, 300));
        states = blockchain.GetStates(
            ImmutableArray<Address>.Empty.Add(RecipientAddress)
        );
        var finalState = new Balance(600, RecipientAddress);
        That(states[RecipientAddress], Is.EqualTo(finalState.Serialize()));
        states = blockchain.GetStates(
            ImmutableArray<Address>.Empty.Add(Address).Add(RecipientAddress)
        );
        That(states[RecipientAddress], Is.EqualTo(finalState.Serialize()));

        ImmutableDictionary<Address, Dictionary> GetState(int offset)
        {
            var prevHash = store.IndexBlockHash(
                store.CountIndex() - 1 - offset
            );
            return blockchain!.GetStates(
                ImmutableArray<Address>.Empty
                    .Add(Address)
                    .Add(RecipientAddress),
                offset: prevHash);
        }

        Dictionary RecipientState(int offset) => GetState(offset)[RecipientAddress];
        
        That(RecipientState(0), Is.EqualTo(new Balance(600, RecipientAddress).Serialize()));
        That(RecipientState(1), Is.EqualTo(new Balance(300, RecipientAddress).Serialize()));
        That(RecipientState(2), Is.EqualTo(new Balance(100, RecipientAddress).Serialize()));
        That(GetState(3).ContainsKey(RecipientAddress), Is.False);
    }
}