using System.Collections.Immutable;
using System.Text;
using static NUnit.Framework.Assert;

namespace PoCPlanet.Tests;

public class BlockTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Mine()
    {
        var dt = DateTime.Now;
        var block = Block.Mine(
            index: 0,
            difficulty: 16,
            rewardBeneficiary: null,
            previousHash: null,
            timestamp: dt,
            transactions: ImmutableArray<Transaction>.Empty
            );
        Assert.Multiple(() =>
        {
            That(block.Difficulty, Is.EqualTo(16));
            That(Hashcash.HasLeadingZeroBits(block.Hash, 16));
            That(block.Index, Is.EqualTo(0));
            That(block.Timestamp, Is.EqualTo(dt));
            That(block.Transactions.SequenceEqual(new List<Transaction>()));
            That(block.PreviousHash, Is.Null);
            That(block.RewardBeneficiary, Is.Null);
        });
    }
}