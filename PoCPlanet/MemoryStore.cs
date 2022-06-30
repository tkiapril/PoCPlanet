using System.Collections.Immutable;
using Bencodex.Types;

namespace PoCPlanet;

public class MemoryStore : IStore
{
    private ImmutableArray<Hash> _indices = ImmutableArray<Hash>.Empty;
    private ImmutableDictionary<Address, ImmutableArray<TxId>> _addressTxIds =
        ImmutableDictionary<Address, ImmutableArray<TxId>>.Empty;
    private ImmutableDictionary<TxId, Transaction> _txs = ImmutableDictionary<TxId, Transaction>.Empty;
    private ImmutableArray<TxId> _stagedTxs = ImmutableArray<TxId>.Empty;
    private ImmutableDictionary<Hash, Block> _blocks = ImmutableDictionary<Hash, Block>.Empty;
    private ImmutableDictionary<Hash, ImmutableDictionary<Address, Dictionary>> _states =
        ImmutableDictionary<Hash, ImmutableDictionary<Address, Dictionary>>.Empty;

    public int CountIndex() => _indices.Length;

    public IEnumerable<Hash> IterateIndex() => _indices.OfType<Hash>();

    public Hash? IndexBlockHash(int index)
    {
        if (_indices.IsEmpty) return null;
        if (index >= 0) return new Hash(_indices[index]);
        index += CountIndex();
        return index < 0 ? null : new Hash(_indices[index]);
    }

    public int AppendIndex(Hash hash)
    {
        _indices = _indices.Add(hash);
        return _indices.Length - 1;
    }

    public IEnumerable<Address> IterateAddresses() => _addressTxIds.Keys;

    public IList<TxId>? GetAddressTransactionIds(Address address) =>
        _addressTxIds.ContainsKey(address) ? _addressTxIds[address] : null;

    public int AppendAddressTransactionId(Address address, TxId txId)
    {
        var txIds =
            _addressTxIds.ContainsKey(address) ? _addressTxIds[address] : ImmutableArray<TxId>.Empty;
        txIds = txIds.Add(txId);
        _addressTxIds = _addressTxIds.Remove(address).Add(address, txIds);
        return txId.Count - 1;
    }

    public void StageTransactionIds(IEnumerable<TxId> txIds)
    {
        foreach (var txId in txIds)
        {
            if (!_txs.ContainsKey(txId))
            {
                throw new Exception();
            }

            _stagedTxs = _stagedTxs.Remove(txId).Add(txId);
        }
    }

    public void UnstageTransactionIds(IEnumerable<TxId> txIds)
    {
        foreach (var txId in txIds)
        {
            _stagedTxs = _stagedTxs.Remove(txId);
        }
    }

    public IEnumerable<TxId> IterateStagedTransactionIds() => _stagedTxs.OfType<TxId>();

    public IEnumerable<TxId> IterateTransactionIds() => _txs.Keys;

    public Transaction? GetTransaction(TxId txId) => _txs.ContainsKey(txId) ? _txs[txId] : null;

    public void PutTransaction(Transaction tx) => _txs = _txs.Remove(tx.Id).Add(tx.Id, tx);

    public bool DeleteTransaction(TxId tx)
    {
        var success = _txs.ContainsKey(tx);
        _txs = _txs.Remove(tx);
        return success;
    }

    public IEnumerable<Hash> IterateBlockHashes() => _blocks.Keys;

    public Block? GetBlock(Hash blockHash) => _blocks.ContainsKey(blockHash) ? _blocks[blockHash] : null;

    public void PutBlock(Block block) => _blocks = _blocks.Remove(block.Hash).Add(block.Hash, block);

    public bool DeleteBlock(Hash blockHash)
    {
        var success = _blocks.ContainsKey(blockHash);
        _blocks = _blocks.Remove(blockHash);
        return success;
    }

    public ImmutableDictionary<Address, Dictionary> GetBlockStates(Hash blockHash) =>
        _states.ContainsKey(blockHash) ? _states[blockHash] : ImmutableDictionary<Address, Dictionary>.Empty;

    public void SetBlockStates(Hash blockHash, ImmutableDictionary<Address, Dictionary> states) =>
        _states = _states.Remove(blockHash).Add(blockHash, states);

    public int CountTransactions() => _txs.Count;

    public int CountBlocks() => _blocks.Count;

    public int CountAddresses() => _addressTxIds.Count;
}