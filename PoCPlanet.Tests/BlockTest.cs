using System.Text;

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
            transactions: new List<Transaction>()
            );
        Assert.IsTrue(block.Difficulty == 16);
        Assert.IsTrue(Hashcash.HasLeadingZeroBits(block.Hash, 16));
        Assert.IsTrue(block.Index == 0);
        Assert.IsTrue(block.Timestamp == dt);
        Assert.IsTrue(block.Transactions.SequenceEqual(new List<Transaction>()));
        Assert.IsTrue(block.PreviousHash is null);
        Assert.IsTrue(block.RewardBeneficiary is null);
    }
}