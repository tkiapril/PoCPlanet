using System.Collections.Immutable;
using System.Numerics;
using System.Resources;
using Bencodex.Types;
using Libplanet.Crypto;

namespace PoCPlanet.Tests;

public class ScenarioTest
{
    private static Block MakeGenesisBlock(PrivateKey seed) => new Block(
        Index: 0,
        Difficulty: 0,
        Nonce: new Nonce(Array.Empty<byte>()),
        RewardBeneficiary: null,
        PreviousHash: null,
        Timestamp: new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Transactions: ImmutableArray<Transaction>.Empty
            .Add(
                Transaction.Make(
                    seed,
                    recipient: new Address(seed.PublicKey),
                    actions: ImmutableArray<IAction>.Empty
                        .Add(new InitializeAction(new Address(seed.PublicKey))),
                    timestamp: DateTime.Now
                    )
                )
    );

    private static Transaction CreateTransaction(PrivateKey privateKey, Address recipient, BigInteger amount) =>
        Transaction.Make(
            privateKey,
            recipient: recipient,
            actions: ImmutableArray<IAction>.Empty
                .Add(new TransferAction(privateKey.PublicKey, recipient, amount)),
            timestamp: DateTime.Now
        );

    private static BigInteger QueryBalance(Blockchain blockchain, Address address)
    {
        var states = 
            blockchain.GetStates(ImmutableList<Address>.Empty.Add(address));
        return states.ContainsKey(address) ? Balance.Deserialize(states[address]).BalanceValue : 0;
    }
        

    [Test]
    public void Scenario()
    {
        // generate seed user
        var seedPrivate = new PrivateKey();
        Console.WriteLine($"Seed user's address is {new Address(seedPrivate.PublicKey)}");
        
        // initialize blockchain
        IStore store = new MemoryStore();
        var blockchain = new Blockchain(ref store);
        blockchain.Append(MakeGenesisBlock(seedPrivate));
        Console.WriteLine(
            $"Hash of the genesis block is {(ImmutableHexBytes)blockchain[0].Hash}"
            + $", and the TxId of seeding tx is {blockchain[0].Transactions.First()}"
            );
        
        // generate new user A
        var privateA = new PrivateKey();
        Console.WriteLine($"User A has address {new Address(privateA.PublicKey)}");
        
        // seed user creates transaction to send user A 100 tokens
        var newTx = CreateTransaction(seedPrivate, new Address(privateA.PublicKey), 100);
        blockchain.StageTransactions(ImmutableHashSet<Transaction>.Empty.Add(newTx));
        Console.WriteLine($"New tx {newTx} has been staged");
        
        // Mine block
        blockchain.MineBlock(rewardBeneficiary: new Address(seedPrivate.PublicKey));
        Console.WriteLine($"Block {blockchain[-1].Hash} mined");
        
        // query balance of seed and A
        Console.WriteLine($"Balance of seed: {QueryBalance(blockchain, new Address(seedPrivate.PublicKey))}");
        Console.WriteLine($"Balance of A: {QueryBalance(blockchain, new Address(privateA.PublicKey))}");
        
        // generate new user B
        var privateB = new PrivateKey();
        Console.WriteLine($"User B has address {new Address(privateB.PublicKey)}");
        
        // user A tries to send user B 200 tokens
        newTx = CreateTransaction(privateA, new Address(privateB.PublicKey), 200);
        blockchain.StageTransactions(ImmutableHashSet<Transaction>.Empty.Add(newTx));
        Console.WriteLine($"New tx {newTx} has been staged");
        
        // Mine block
        blockchain.MineBlock(rewardBeneficiary: new Address(seedPrivate.PublicKey));
        
        // query balance of A and B
        Console.WriteLine($"Balance of A: {QueryBalance(blockchain, new Address(privateA.PublicKey))}");
        Console.WriteLine($"Balance of B: {QueryBalance(blockchain, new Address(privateB.PublicKey))}");
        // action does get into the block, but the state does not change
        
        // seed user sends user A 100 tokens, user A sends user B 200 tokens
        newTx = CreateTransaction(seedPrivate, new Address(privateA.PublicKey), 100);
        var newTx2 = CreateTransaction(privateA, new Address(privateB.PublicKey), 200);
        blockchain.StageTransactions(ImmutableHashSet<Transaction>.Empty.Add(newTx).Add(newTx2));
        Console.WriteLine($"New tx {newTx} and {newTx2} has been staged");
        
        // Mine block
        blockchain.MineBlock(rewardBeneficiary: new Address(seedPrivate.PublicKey));
        
        // query balance of A and B
        Console.WriteLine($"Balance of A: {QueryBalance(blockchain, new Address(privateA.PublicKey))}");
        Console.WriteLine($"Balance of B: {QueryBalance(blockchain, new Address(privateB.PublicKey))}");
        // transfer of 200 tokens from A to B fails
        
        // user A sends both seed user and user B 200 tokens
        newTx = CreateTransaction(privateA, new Address(seedPrivate.PublicKey), 200);
        newTx2 = CreateTransaction(privateA, new Address(privateB.PublicKey), 200);
        blockchain.StageTransactions(ImmutableHashSet<Transaction>.Empty.Add(newTx).Add(newTx2));
        Console.WriteLine($"New tx {newTx} and {newTx2} has been staged");
        
        // Mine block
        blockchain.MineBlock(rewardBeneficiary: new Address(seedPrivate.PublicKey));
        
        // query balance of A and B
        Console.WriteLine($"Balance of A: {QueryBalance(blockchain, new Address(privateA.PublicKey))}");
        Console.WriteLine($"Balance of B: {QueryBalance(blockchain, new Address(privateB.PublicKey))}");
        Console.WriteLine($"Balance of seed user: {QueryBalance(blockchain, new Address(seedPrivate.PublicKey))}");

        foreach (var block in blockchain)
        {
            Console.WriteLine($"Block {block.Index}\n\tBalances:");
            var states =
                blockchain.GetStates(store.IterateAddresses().ToImmutableArray(), block.Hash);
            var updatedStates = blockchain.GetStates(
                store.IterateAddresses().ToImmutableArray(),
                block.PreviousHash
            );
            foreach (var state in states)
            {
                Console.WriteLine($"\t\t{state.Key}: {Balance.Deserialize(state.Value).BalanceValue}");
            }

            Console.WriteLine("\tTransfers:");
            var failedActions = ImmutableArray<TransferAction>.Empty;
            foreach (var tx in block.Transactions)
            {
                foreach (var action in tx.Actions)
                {
                    if (action is not TransferAction transfer) continue;
                    try
                    {
                        var stateUpdate = transfer.Execute(
                            tx.Sender,
                            tx.Recipient,
                            updatedStates
                        );
                        updatedStates = stateUpdate.Aggregate(
                            updatedStates,
                            (current, state) => 
                                current.Remove(state.Key).Add(state.Key, state.Value)
                                );

                        Console.WriteLine($"\t\tTransfer from {tx.Sender} to {tx.Recipient} of {transfer.Amount}");
                    }
                    catch (StateTransitionError e)
                    {
                        failedActions = failedActions.Add(transfer);
                    }
                }
            }
            
            Console.WriteLine("\tFailed transfers:");
            
            foreach (var transfer in failedActions)
            {
                Console.WriteLine(
                    $"\t\tTransfer from {new Address(transfer.PublicKey)} to {transfer.Recipient} of {transfer.Amount}"
                    );
            }

            Console.WriteLine();
        }
    }
}