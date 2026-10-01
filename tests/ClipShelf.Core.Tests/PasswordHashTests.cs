using ClipShelf.Core.Security;

namespace ClipShelf.Core.Tests;

public sealed class PasswordHashTests
{
    [Fact]
    public void The_right_password_verifies()
    {
        var stored = PasswordHash.Create("hunter2");

        Assert.True(PasswordHash.Verify("hunter2", stored));
    }

    [Fact]
    public void A_wrong_password_does_not_verify()
    {
        var stored = PasswordHash.Create("hunter2");

        Assert.False(PasswordHash.Verify("Hunter2", stored));
    }

    [Fact]
    public void The_same_password_gets_a_new_salt_every_time()
    {
        Assert.NotEqual(PasswordHash.Create("hunter2"), PasswordHash.Create("hunter2"));
    }

    [Theory]
    [InlineData("not-a-hash")]
    [InlineData("1.2.3")]
    [InlineData("0.AAAA.BBBB")]
    public void A_damaged_hash_never_verifies(string stored)
    {
        Assert.False(PasswordHash.Verify("hunter2", stored));
    }
}
