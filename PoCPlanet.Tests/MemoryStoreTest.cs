using System.Collections.Immutable;
using Bencodex.Types;
using static NUnit.Framework.Assert;

namespace PoCPlanet.Tests;

public class MemoryStoreTest : FixtureBase
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void StoreBlock()
    {
        var store = new MemoryStore();
        var blockchain = Blockchain();
        var a = blockchain[0];
        var b = blockchain[1];
        var c = blockchain[2];

        Multiple(() =>
        {
            That(store.CountBlocks(), Is.Zero);
            That(
                store.IterateBlockHashes().Aggregate(
                    ImmutableHashSet<Hash>.Empty,
                    (current, blockHash) => current.Add(blockHash)
                ),
                Is.Empty
            );
            That(store.GetBlock(a.Hash), Is.Null);
            That(store.GetBlock(b.Hash), Is.Null);
            That(store.GetBlock(c.Hash), Is.Null);
        });
        
        var deleted = store.DeleteBlock(a.Hash);
        That(deleted, Is.False);

        store.PutBlock(a);
        Multiple(() =>
        {
            That(store.CountBlocks(), Is.EqualTo(1));
            That(
                store.IterateBlockHashes().Aggregate(
                    ImmutableHashSet<Hash>.Empty,
                    (current, blockHash) => current.Add(blockHash)
                ),
                Is.EqualTo(
                    ImmutableHashSet<Hash>.Empty
                        .Add(a.Hash)
                )
            );
            That(store.GetBlock(a.Hash), Is.EqualTo(a));
            That(store.GetBlock(b.Hash), Is.Null);
            That(store.GetBlock(c.Hash), Is.Null);
        });

        store.PutBlock(b);
        Multiple(() =>
        {
            That(store.CountBlocks(), Is.EqualTo(2));
            That(
                store.IterateBlockHashes().Aggregate(
                    ImmutableHashSet<Hash>.Empty,
                    (current, blockHash) => current.Add(blockHash)
                ),
                Is.EqualTo(
                    ImmutableHashSet<Hash>.Empty
                        .Add(a.Hash)
                        .Add(b.Hash)
                )
            );
            That(store.GetBlock(a.Hash), Is.EqualTo(a));
            That(store.GetBlock(b.Hash), Is.EqualTo(b));
            That(store.GetBlock(c.Hash), Is.Null);
        });

        deleted = store.DeleteBlock(a.Hash);
        Multiple(() =>
        {
            That(deleted);
            That(store.CountBlocks(), Is.EqualTo(1));
            That(
                store.IterateBlockHashes().Aggregate(
                    ImmutableHashSet<Hash>.Empty,
                    (current, blockHash) => current.Add(blockHash)
                ),
                Is.EqualTo(
                    ImmutableHashSet<Hash>.Empty
                        .Add(b.Hash)
                )
            );
        });
        That(store.GetBlock(a.Hash), Is.Null);
        That(store.GetBlock(b.Hash), Is.EqualTo(b));
        That(store.GetBlock(c.Hash), Is.Null);
    }

    [Test]
    public void StoreTx()
    {
        var store = new MemoryStore();
        Multiple(() =>
        {
            That(store.CountTransactions(), Is.Zero);
            That(
                store.IterateTransactionIds().Aggregate(
                    ImmutableHashSet<TxId>.Empty,
                    (current, txId) => current.Add(txId)
                ),
                Is.Empty
            );
            That(store.GetTransaction(Transaction.Id), Is.Null);
            That(store.GetTransaction(Transaction2.Id), Is.Null);
        });

        var deleted = store.DeleteTransaction(Transaction.Id);
        That(deleted, Is.False);
        
        store.PutTransaction(Transaction);
        Multiple(() =>
        {
            That(store.CountTransactions(), Is.EqualTo(1));
            That(
                store.IterateTransactionIds().Aggregate(
                    ImmutableHashSet<TxId>.Empty,
                    (current, txId) => current.Add(txId)
                ),
                Is.EqualTo(
                    ImmutableHashSet<TxId>.Empty
                        .Add(Transaction.Id)
                )
            );
            That(store.GetTransaction(Transaction.Id), Is.EqualTo(Transaction));
            That(store.GetTransaction(Transaction2.Id), Is.Null);
        });
        
        store.PutTransaction(Transaction2);
        Multiple(() =>
        {
            That(store.CountTransactions(), Is.EqualTo(2));
            That(
                store.IterateTransactionIds().Aggregate(
                    ImmutableHashSet<TxId>.Empty,
                    (current, txId) => current.Add(txId)
                ),
                Is.EqualTo(
                    ImmutableHashSet<TxId>.Empty
                        .Add(Transaction.Id)
                        .Add(Transaction2.Id)
                )
            );
            That(store.GetTransaction(Transaction.Id), Is.EqualTo(Transaction));
            That(store.GetTransaction(Transaction2.Id), Is.EqualTo(Transaction2));
        });

        deleted = store.DeleteTransaction(Transaction2.Id);
        Multiple(() =>
        {
            That(deleted);
            That(store.CountTransactions(), Is.EqualTo(1));
            That(
                store.IterateTransactionIds().Aggregate(
                    ImmutableHashSet<TxId>.Empty,
                    (current, txId) => current.Add(txId)
                ),
                Is.EqualTo(
                    ImmutableHashSet<TxId>.Empty
                        .Add(Transaction.Id)
                )
            );
            That(store.GetTransaction(Transaction.Id), Is.EqualTo(Transaction));
            That(store.GetTransaction(Transaction2.Id), Is.Null);
        });
    }

    [Test]
    public void StoreIndex()
    {
        var store = new MemoryStore();
        Multiple(() =>
        {
            That(store.CountIndex(), Is.Zero);
            That(
                store.IterateIndex().Aggregate(
                    ImmutableArray<Hash>.Empty,
                    (current, hash) => current.Add(hash)
                ),
                Is.Empty
            );
            That(store.IndexBlockHash(0), Is.Null);
            That(store.IndexBlockHash(-1), Is.Null);
        });

        var hash00 = new Hash(Enumerable.Repeat((byte)0x00, 32).ToArray());
        store.AppendIndex(hash00);
        Multiple(() =>
        {
            That(store.CountIndex(), Is.EqualTo(1));
            That(
                store.IterateIndex().Aggregate(
                    ImmutableArray<Hash>.Empty,
                    (current, hash) => current.Add(hash)
                ),
                Is.EqualTo(
                    ImmutableArray<Hash>.Empty
                        .Add(hash00)
                )
            );
            That(store.IndexBlockHash(0), Is.EqualTo(hash00));
            That(store.IndexBlockHash(-1), Is.EqualTo(hash00));
        });
        
        var hash01 = new Hash(Enumerable.Repeat((byte)0x01, 32).ToArray());
        store.AppendIndex(hash01);
        Multiple(() =>
        {
            That(store.CountIndex(), Is.EqualTo(2));
            That(
                store.IterateIndex().Aggregate(
                    ImmutableArray<Hash>.Empty,
                    (current, hash) => current.Add(hash)
                ),
                Is.EqualTo(
                    ImmutableArray<Hash>.Empty
                        .Add(hash00)
                        .Add(hash01)
                )
            );
            That(store.IndexBlockHash(0), Is.EqualTo(hash00));
            That(store.IndexBlockHash(1), Is.EqualTo(hash01));
            That(store.IndexBlockHash(-1), Is.EqualTo(hash01));
            That(store.IndexBlockHash(-2), Is.EqualTo(hash00));
        });
    }

    [Test]
    public void StoreStage()
    {
        var store = new MemoryStore();
        store.PutTransaction(Transaction);
        store.PutTransaction(Transaction2);
        That(
            store.IterateStagedTransactionIds().Aggregate(
                ImmutableArray<TxId>.Empty,
                (current, txId) => current.Add(txId)
            ),
            Is.Empty
        );
        
        store.StageTransactionIds(
            ImmutableArray<TxId>.Empty
                .Add(Transaction.Id)
                .Add(Transaction2.Id)
            );
        That(
            store.IterateStagedTransactionIds().Aggregate(
                ImmutableArray<TxId>.Empty,
                (current, txId) => current.Add(txId)
            ),
            Is.EqualTo(
                ImmutableArray<TxId>.Empty
                    .Add(Transaction.Id)
                    .Add(Transaction2.Id)
            )
        );
        
        store.UnstageTransactionIds(ImmutableArray<TxId>.Empty.Add(Transaction.Id));
        That(
            store.IterateStagedTransactionIds().Aggregate(
                ImmutableArray<TxId>.Empty,
                (current, txId) => current.Add(txId)
            ),
            Is.EqualTo(
                ImmutableArray<TxId>.Empty
                    .Add(Transaction2.Id)
            )
        );
    }

    [Test]
    public void StoreAddresses()
    {
        var store = new MemoryStore();
        Multiple(() =>
        {
            That(store.CountAddresses(), Is.Zero);
            That(
                store.IterateAddresses().Aggregate(
                    ImmutableHashSet<Address>.Empty,
                    (current, address) => current.Add(address)
                ),
                Is.Empty
            );
            That(store.GetAddressTransactionIds(Address), Is.Null);
            That(store.GetAddressTransactionIds(RecipientAddress), Is.Null);
        });

        var txId00 = new TxId(Enumerable.Repeat((byte)0x00, 32).ToArray());
        store.AppendAddressTransactionId(Address, txId00);
        Multiple(() =>
        {
            That(store.CountAddresses(), Is.EqualTo(1));
            That(
                store.IterateAddresses().Aggregate(
                    ImmutableHashSet<Address>.Empty,
                    (current, address) => current.Add(address)
                ),
                Is.EqualTo(
                    ImmutableHashSet<Address>.Empty
                        .Add(Address)
                )
            );
            var txIds = store.GetAddressTransactionIds(Address);
            That(txIds, Is.Not.Null);
            That(
                txIds!.Aggregate(
                    ImmutableArray<TxId>.Empty,
                    (current, txId) => current.Add(txId)
                ),
                Is.EqualTo(
                    ImmutableArray<TxId>.Empty
                        .Add(txId00)
                )
            );
            That(store.GetAddressTransactionIds(RecipientAddress), Is.Null);
        });

        var txId01 = new TxId(Enumerable.Repeat((byte)0x01, 32).ToArray());
        store.AppendAddressTransactionId(Address, txId01);
        Multiple(() =>
        {
            That(store.CountAddresses(), Is.EqualTo(1));
            That(
                store.IterateAddresses().Aggregate(
                    ImmutableHashSet<Address>.Empty,
                    (current, address) => current.Add(address)
                ),
                Is.EqualTo(
                    ImmutableHashSet<Address>.Empty
                        .Add(Address)
                )
            );
            var txIds = store.GetAddressTransactionIds(Address);
            That(txIds, Is.Not.Null);
            That(
                txIds!.Aggregate(
                    ImmutableArray<TxId>.Empty,
                    (current, txId) => current.Add(txId)
                ),
                Is.EqualTo(
                    ImmutableArray<TxId>.Empty
                        .Add(txId00)
                        .Add(txId01)
                )
            );
            That(store.GetAddressTransactionIds(RecipientAddress), Is.Null);
        });

        var txId02 = new TxId(Enumerable.Repeat((byte)0x02, 32).ToArray());
        store.AppendAddressTransactionId(RecipientAddress, txId02);
        Multiple(() =>
        {
            That(store.CountAddresses(), Is.EqualTo(2));
            That(
                store.IterateAddresses().Aggregate(
                    ImmutableHashSet<Address>.Empty,
                    (current, address) => current.Add(address)
                ),
                Is.EqualTo(
                    ImmutableHashSet<Address>.Empty
                        .Add(Address)
                        .Add(RecipientAddress)
                )
            );
            var txIds = store.GetAddressTransactionIds(Address);
            That(txIds, Is.Not.Null);
            That(
                txIds!.Aggregate(
                    ImmutableArray<TxId>.Empty,
                    (current, txId) => current.Add(txId)
                ),
                Is.EqualTo(
                    ImmutableArray<TxId>.Empty
                        .Add(txId00)
                        .Add(txId01)
                )
            );
            txIds = store.GetAddressTransactionIds(RecipientAddress);
            That(txIds, Is.Not.Null);
            That(
                txIds!.Aggregate(
                    ImmutableArray<TxId>.Empty,
                    (current, txId) => current.Add(txId)
                ),
                Is.EqualTo(
                    ImmutableArray<TxId>.Empty
                        .Add(txId02)
                )
            );
        });
    }

    [Test]
    public void StoreBlockState()
    {
        var store = new MemoryStore();
        var hash00 = new Hash(Enumerable.Repeat((byte)0x00, 32).ToArray());
        That(store.GetBlockStates(hash00), Is.Empty);

        var state = ImmutableDictionary<Address, Dictionary>.Empty
            .Add(
                Address,
                Dictionary.Empty
                    .Add("a", 1)
            )
            .Add(
                RecipientAddress,
                Dictionary.Empty
                    .Add("b", 2)
                );
        
        store.SetBlockStates(hash00, state);
        That(store.GetBlockStates(hash00), Is.EqualTo(state));
    }
}