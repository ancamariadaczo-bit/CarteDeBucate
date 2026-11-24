using System.Collections.Concurrent;
using System.Reflection;

public class ExtensionAuthenticationCodeServiceTests
{
    private static readonly DateTimeOffset InitialTime =
        new(2026, 9, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateCode_ShouldReturnNonEmptyDistinctCryptographicCodes()
    {
        TestTimeProvider timeProvider = new(InitialTime);
        ExtensionAuthenticationCodeService service = new(timeProvider);

        string firstCode = service.CreateCode(1);
        string secondCode = service.CreateCode(2);

        Assert.Matches("^[0-9A-F]{64}$", firstCode);
        Assert.Matches("^[0-9A-F]{64}$", secondCode);
        Assert.NotEqual(firstCode, secondCode);
    }

    [Fact]
    public void ConsumeCode_ShouldReturnTheStoredUserData()
    {
        TestTimeProvider timeProvider = new(InitialTime);
        ExtensionAuthenticationCodeService service = new(timeProvider);
        string code = service.CreateCode(42);

        ExtensionAuthenticationCodeData? result = service.ConsumeCode(code);

        Assert.NotNull(result);
        Assert.Equal(42, result.UserId);
        Assert.Equal(InitialTime.AddMinutes(2), result.ExpiresAt);
    }

    [Fact]
    public void ConsumeCode_ShouldConsumeACodeOnlyOnce()
    {
        TestTimeProvider timeProvider = new(InitialTime);
        ExtensionAuthenticationCodeService service = new(timeProvider);
        string code = service.CreateCode(42);

        ExtensionAuthenticationCodeData? firstResult = service.ConsumeCode(code);
        ExtensionAuthenticationCodeData? secondResult = service.ConsumeCode(code);

        Assert.NotNull(firstResult);
        Assert.Null(secondResult);
    }

    [Fact]
    public void ConsumeCode_ShouldReturnNullForAnUnknownCode()
    {
        TestTimeProvider timeProvider = new(InitialTime);
        ExtensionAuthenticationCodeService service = new(timeProvider);

        ExtensionAuthenticationCodeData? result =
            service.ConsumeCode("UNKNOWN-CODE");

        Assert.Null(result);
    }

    [Fact]
    public void ConsumeCode_ShouldReturnNullForAnExpiredCode()
    {
        TestTimeProvider timeProvider = new(InitialTime);
        ExtensionAuthenticationCodeService service = new(timeProvider);
        string code = service.CreateCode(42);
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        ExtensionAuthenticationCodeData? result = service.ConsumeCode(code);

        Assert.Null(result);
    }

    [Fact]
    public void CreateCode_ShouldKeepExistingValidCodesDuringCleanup()
    {
        TestTimeProvider timeProvider = new(InitialTime);
        ExtensionAuthenticationCodeService service = new(timeProvider);
        string existingCode = service.CreateCode(1);
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        service.CreateCode(2);
        ExtensionAuthenticationCodeData? result =
            service.ConsumeCode(existingCode);

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
    }

    [Fact]
    public void CreateCode_ShouldRemoveAbandonedExpiredCodesDuringCleanup()
    {
        TestTimeProvider timeProvider = new(InitialTime);
        ExtensionAuthenticationCodeService service = new(timeProvider);
        string expiredCode = service.CreateCode(1);
        ConcurrentDictionary<string, ExtensionAuthenticationCodeData> storedCodes =
            GetStoredCodes(service);
        Assert.Contains(expiredCode, storedCodes.Keys);
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        string newCode = service.CreateCode(2);

        Assert.DoesNotContain(expiredCode, storedCodes.Keys);
        Assert.Contains(newCode, storedCodes.Keys);
    }

    private static ConcurrentDictionary<string, ExtensionAuthenticationCodeData>
        GetStoredCodes(ExtensionAuthenticationCodeService service)
    {
        FieldInfo codesField =
            typeof(ExtensionAuthenticationCodeService).GetField(
                "_codes",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "The authentication code storage field was not found.");

        return Assert.IsType<
            ConcurrentDictionary<string, ExtensionAuthenticationCodeData>>(
                codesField.GetValue(service));
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
