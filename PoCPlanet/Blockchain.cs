using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;

namespace PoCPlanet;

public class Blockchain : IReadOnlyList<Block>
{
    private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(5);
    
    private readonly IStore _store;
    private readonly BlockSet _blocks;
    private readonly TransactionSet _transactions;
    private AddressTransactionSet _addresses;

    public Blockchain(ref IStore store)
    {
        _store = store;
        _blocks = new BlockSet(ref _store);
        _transactions = new TransactionSet(ref _store);
        _addresses = new AddressTransactionSet(ref _store);
    }

    public IEnumerator<Block> GetEnumerator() =>
        (from index in _store.IterateIndex() select _blocks[index]).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _store.CountIndex();

    public Block this[int index]
    {
        get
        {
            var blockHash = _store.IndexBlockHash(index);
            if (blockHash is null)
            {
                throw new KeyNotFoundException();
            }

            return _blocks[blockHash];
        }
    }

    public ImmutableDictionary<Address, Dictionary> GetStates(
        IReadOnlyCollection<Address> addresses,
        Hash? offset = null
        )
    {
        if (offset is null)
        {
            try
            {
                offset = _store.IndexBlockHash(-1);
            }
            catch (IndexOutOfRangeException)
            {
                return ImmutableDictionary<Address, Dictionary>.Empty;
            }
        }
        var states = new Dictionary<Address, Dictionary>();
        while (offset is not null)
        {
            foreach (
                var a 
                in _store.GetBlockStates(offset)
                    .Where(
                        a =>
                            addresses.Contains(a.Key) && !states.ContainsKey(a.Key)
                            )
                )
            {
                states[a.Key] = a.Value;
            }

            if (states.Keys.ToImmutableHashSet().Equals(addresses.ToImmutableHashSet()))
            {
                break;
            }

            offset = _blocks[offset].PreviousHash;
        }

        return states.ToImmutableDictionary();
    }

    public void Append(Block block)
    {
        Validate(this.Concat(ImmutableArray<Block>.Empty.Add(block)).ToImmutableArray());
        _blocks[block.Hash] = block;
        EvaluateActions(block);
        var index = _store.AppendIndex(block.Hash);
        if (block.Index != index)
        {
            throw new Exception();
        }

        var txIds = block.Transactions.Aggregate(
            ImmutableHashSet<TxId>.Empty,
            (current, tx) => 
                current.Add(tx.Id)
                );
        
        _store.UnstageTransactionIds(
            from txId in _store.IterateStagedTransactionIds() where txIds.Contains(txId) select txId
            );

        foreach (var tx in block.Transactions)
        {
            _store.AppendAddressTransactionId(tx.Recipient, tx.Id);
        }
    }
    
    public void EvaluateActions(Block block)
    {
        var prevHash = block.PreviousHash;
        var states = ImmutableDictionary<Address, Dictionary>.Empty;
        Debug.WriteLine($"{block.Hash.ToString()[..5]}: {StateUtil.ToString(states)}");
        foreach (var tx in block.Transactions)
        {
            foreach(var x in tx.Actions.Select((action, i) => (i, action)))
            {
                var request = x.action.RequestStates(tx.Sender, tx.Recipient);
                var newStates =
                    GetStates(request.Except(states.Keys), offset: prevHash);
                states.ToList().ForEach(
                    kv => newStates = newStates.Remove(kv.Key).Add(kv.Key, kv.Value)
                    );
                states = newStates;
                var requestedStates = 
                    from addr in request
                    select new KeyValuePair<Address, Dictionary>(
                        addr,
                        states.ContainsKey(addr) ? states[addr] : Dictionary.Empty
                        );
                var stateChanges = x.action.Execute(
                    tx.Sender,
                    tx.Recipient,
                    requestedStates.ToImmutableDictionary()
                );
                stateChanges.ToList().ForEach(
                    kv => states = states.Remove(kv.Key).Add(kv.Key, kv.Value)
                    );
                Debug.WriteLine($"{block.Hash.ToString()[..5]}.{tx.Id.ToString()}#{x.i}: {states}");
            }
        }
        _store.SetBlockStates(block.Hash, states);
    }

    public static IEnumerable<Tuple<Block?, int>> ExpectDifficulties(
        IReadOnlyCollection<Block> blocks,
        bool yieldNext = false
        )
    {
        DateTime? prevTimestamp = null;
        DateTime? prevPrevTimestamp = null;
        var difficulty = 0;
        IEnumerable<Block?> tempBlocks = blocks;
        if (yieldNext)
        {
            tempBlocks = blocks.Concat(ImmutableArray<Block?>.Empty.Add(null));
        }

        foreach (var block in tempBlocks)
        {
            var needMoreDifficulty = prevTimestamp is not null && (
                    prevPrevTimestamp is null ||
                    prevTimestamp - prevPrevTimestamp < BlockInterval
                    );
            difficulty += Math.Max(needMoreDifficulty ? 1 : -1, 0);
            yield return new Tuple<Block?, int>(block, difficulty);
            if (block is null) continue;
            prevPrevTimestamp = prevTimestamp;
            prevTimestamp = block.Timestamp;
        }
    }

    public static void Validate(IReadOnlyCollection<Block> blocks)
    {
        Hash? prevHash = null;
        DateTime? prevTimestamp = null;
        DateTime now = DateTime.UtcNow;
        foreach (
            var x in ExpectDifficulties(blocks).Select(
                (expect, i) => (i, block: expect.Item1, difficulty: expect.Item2)
                )
            )
        {
            if (x.block is null)
            {
                throw new Exception();
            }

            if (x.block.Index != x.i)
            {
                throw new BlockIndexError(
                    $"the expected block index is {x.i} but its index is {x.block.Index}"
                    );
            }

            if (x.block.Difficulty < x.difficulty)
            {
                throw new BlockDifficultyError(
                    $"the expected difficulty of the block #{x.i} is {x.difficulty}, "
                    + "but its difficulty is {x.block.Difficulty}"
                    );
            }

            if (x.block.PreviousHash != prevHash)
            {
                if (prevHash is null)
                {
                    throw new BlockPreviousHashError("the genesis block must not have a previous block");
                }

                var actualPrevHash = 
                    x.block.PreviousHash is not null ? x.block.PreviousHash.ToString() : "nothing";

                throw new BlockPreviousHashError(
                    $"the block #{x.i} is not continuous from the block #{x.i - 1}; "
                    + $"while the previous block's hash is {prevHash}, "
                    + $"the block #{x.i}'s pointer to the previous hash refers to {actualPrevHash}"
                    );
            }

            if (now < x.block.Timestamp)
            {
                throw new BlockTimestampError(
                    $"the block #{x.i}'s timestamp ({x.block.Timestamp}) is later "
                    + $"than now ({now})"
                );
            }

            if (prevTimestamp is not null && x.block.Timestamp <= prevTimestamp)
            {
                throw new BlockTimestampError(
                    $"the block #{x.i}'s timestamp ({x.block.Timestamp}) is"
                    + $"earlier than the block #{x.i - 1}'s ({prevTimestamp})"
                );
            }
            
            x.block.Validate();
            prevHash = x.block.Hash;
            prevTimestamp = x.block.Timestamp;
        }
    }

    public void StageTransactions(ImmutableHashSet<Transaction> txs)
    {
        foreach (var x in from tx in txs select (tx.Id, tx))
        {
            _transactions[x.Id] = x.tx;
        }
        _store.StageTransactionIds(from tx in txs select tx.Id);
    }

    public Block MineBlock(Address rewardBeneficiary)
    {
        var index = _store.CountIndex();
        var difficulty = (
            from x in ExpectDifficulties(this, yieldNext: true) 
            where x.Item1 is null select x.Item2
            ).First();
        var block = Block.Mine(
            index: index,
            difficulty: difficulty,
            rewardBeneficiary: rewardBeneficiary,
            previousHash: _store.IndexBlockHash(index - 1),
            timestamp: DateTime.UtcNow,
            transactions:
            from txId in _store.IterateStagedTransactionIds()
            where _store.GetTransaction(txId) is not null
            select _store.GetTransaction(txId)
        );
        Append(block);
        return block;
    }
}

public class BlockSet : IDictionary<Hash, Block>
{
    private readonly IStore _store;

    public BlockSet(ref IStore store) => _store = store;

    public IEnumerator<KeyValuePair<Hash, Block>> GetEnumerator() => (
        from hash in _store.IterateBlockHashes()
        select new KeyValuePair<Hash, Block>(hash, _store.GetBlock(hash))
        ).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<Hash, Block> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        foreach (var i in this)
        {
            _store.DeleteBlock(i.Key);
        }
    }

    public bool Contains(KeyValuePair<Hash, Block> item)
    {
        var block = _store.GetBlock(item.Key);
        return block is not null && block == item.Value;
    }

    public void CopyTo(KeyValuePair<Hash, Block>[] array, int arrayIndex)
    {
        foreach (var i in this)
        {
            array[arrayIndex++] = i;
        }
    }

    public bool Remove(KeyValuePair<Hash, Block> item) => Remove(item.Key);

    public int Count => _store.CountBlocks();
    public bool IsReadOnly => false;
    public void Add(Hash key, Block value)
    {
        if (value.Hash != key)
        {
            throw new BlockHashError($"{value}.hash does not match {key}");
        }
        value.Validate();
        _store.PutBlock(value);
    }

    public bool ContainsKey(Hash key) => _store.GetBlock(key) is not null;

    public bool Remove(Hash key)
    {
        var success = _store.DeleteBlock(key);
        if (!success)
        {
            throw new KeyNotFoundException();
        }

        return success;
    }

    public bool TryGetValue(Hash key, out Block value)
    {
        try
        {
            value = this[key];
        }
        catch (KeyNotFoundException)
        {
            value = null!;
            return false;
        }

        return true;
    }

    public Block this[Hash key]
    {
        get
        {
            var value = _store.GetBlock(key);
            if (value is null)
            {
                throw new KeyNotFoundException();
            }

            if (value.Hash != key)
            {
                throw new Exception();
            }
            value.Validate();

            return value;
        }
        set => Add(key, value);
    }

    public ICollection<Hash> Keys =>
        _store.IterateBlockHashes().Aggregate(
            ImmutableArray<Hash>.Empty,
            (current, hash) => current.Add(hash)
            );

    public ICollection<Block> Values =>
        _store.IterateBlockHashes().Aggregate(
            ImmutableArray<Block>.Empty,
            (current, hash) => current.Add(_store.GetBlock(hash)!)
            );

}

public class TransactionSet : IDictionary<TxId, Transaction>
{
    private readonly IStore _store;

    public TransactionSet(ref IStore store) => _store = store;

    public IEnumerator<KeyValuePair<TxId, Transaction>> GetEnumerator() => (
        from txId in _store.IterateTransactionIds()
        select new KeyValuePair<TxId, Transaction>(txId, _store.GetTransaction(txId))
        ).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TxId, Transaction> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        foreach (var i in this)
        {
            _store.DeleteTransaction(i.Key);
        }
    }

    public bool Contains(KeyValuePair<TxId, Transaction> item)
    {
        var tx = _store.GetTransaction(item.Key);
        return tx is not null && tx == item.Value;
    }

    public void CopyTo(KeyValuePair<TxId, Transaction>[] array, int arrayIndex)
    {
        foreach (var i in this)
        {
            array[arrayIndex++] = i;
        }
    }

    public bool Remove(KeyValuePair<TxId, Transaction> item) => Remove(item.Key);

    public int Count => _store.CountTransactions();
    public bool IsReadOnly => false;
    public void Add(TxId key, Transaction value)
    {
        if (value.Id != key)
        {
            throw new TransactionIdError($"{value}.Id does not match {key}");
        }
        value.Validate();
        _store.PutTransaction(value);
    }

    public bool ContainsKey(TxId key) => _store.GetTransaction(key) is not null;

    public bool Remove(TxId key)
    {
        var success = _store.DeleteTransaction(key);
        if (!success)
        {
            throw new KeyNotFoundException();
        }

        return success;
    }

    public bool TryGetValue(TxId key, out Transaction value)
    {
        try
        {
            value = this[key];
        }
        catch (KeyNotFoundException)
        {
            value = null!;
            return false;
        }

        return true;
    }

    public Transaction this[TxId key]
    {
        get
        {
            var value = _store.GetTransaction(key);
            if (value is null)
            {
                throw new KeyNotFoundException();
            }

            if (value.Id != key)
            {
                throw new Exception();
            }
            value.Validate();

            return value;
        }
        set => Add(key, value);
    }

    public ICollection<TxId> Keys =>
        _store.IterateTransactionIds().Aggregate(
            ImmutableArray<TxId>.Empty,
            (current, txId) => current.Add(txId)
            );


    public ICollection<Transaction> Values =>
        _store.IterateTransactionIds().Aggregate(
            ImmutableArray<Transaction>.Empty,
            (current, txId) => current.Add(_store.GetTransaction(txId)!)
            );
}

public class AddressTransactionSet : IReadOnlyDictionary<Address, IList<Transaction>>
{
    private readonly IStore _store;

    public AddressTransactionSet(ref IStore store) => _store = store;
    public IEnumerator<KeyValuePair<Address, IList<Transaction>>> GetEnumerator() => (
        from address in _store.IterateAddresses()
        let txs = _store.GetAddressTransactionIds(address)
            .Aggregate(
                ImmutableArray<Transaction>.Empty,
                (current, i) => current.Add(_store.GetTransaction(i))
                )
        select new KeyValuePair<Address, IList<Transaction>>(address, txs)
        ).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _store.CountAddresses();

    public bool ContainsKey(Address key) => _store.IterateAddresses().Contains(key);

    public bool TryGetValue(Address key, out IList<Transaction> value)
    {
        try
        {
            value = this[key];
        }
        catch (KeyNotFoundException)
        {
            value = null!;
            return false;
        }

        return true;
    }

    public IList<Transaction> this[Address key]
    {
        get
        {
            var txIdSet = _store.GetAddressTransactionIds(key);
            if (txIdSet is null)
            {
                throw new KeyNotFoundException();
            }

            return new List<Transaction>(
                from tx in (
                    from txId in txIdSet select _store.GetTransaction(txId)
                    ) 
                where tx is not null select tx
                );
        }
    }

    public IEnumerable<Address> Keys => 
        _store.IterateAddresses().Aggregate(
            ImmutableArray<Address>.Empty,
            (current, address) => current.Add(address)
            );

    public IEnumerable<IList<Transaction>> Values =>
            _store.IterateAddresses().Aggregate(
                ImmutableArray<IList<Transaction>>.Empty,
                (current, address) => current.Add(this[address])
                );
}