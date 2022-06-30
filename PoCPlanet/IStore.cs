using System.Collections.Immutable;
using Bencodex.Types;

namespace PoCPlanet;

public interface IStore
{
    public int CountIndex();
    public IEnumerable<Hash> IterateIndex();
    public Hash? IndexBlockHash(int index);
    public int AppendIndex(Hash hash);
    public IEnumerable<Address> IterateAddresses();
    public IList<TxId>? GetAddressTransactionIds(Address address);
    public int AppendAddressTransactionId(Address address, TxId txId);
    public void StageTransactionIds(IEnumerable<TxId> txIds);
    public void UnstageTransactionIds(IEnumerable<TxId> txIds);
    public IEnumerable<TxId> IterateStagedTransactionIds();
    public IEnumerable<TxId> IterateTransactionIds();
    public Transaction? GetTransaction(TxId txId);
    public void PutTransaction(Transaction tx);
    public bool DeleteTransaction(TxId tx);
    public IEnumerable<Hash> IterateBlockHashes();
    public Block? GetBlock(Hash blockHash);
    public void PutBlock(Block block);
    public bool DeleteBlock(Hash blockHash);
    public ImmutableDictionary<Address, Dictionary> GetBlockStates(Hash blockHash);
    public void SetBlockStates(Hash blockHash, ImmutableDictionary<Address, Dictionary> states);
    public int CountTransactions();
    public int CountBlocks();
    public int CountAddresses();
}