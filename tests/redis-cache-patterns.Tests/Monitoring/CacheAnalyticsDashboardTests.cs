#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Monitoring;
using Xunit;

namespace RedisCachePatterns.Tests.Monitoring;

public class CacheAnalyticsDashboardTests
{
    private readonly Mock<ILogger<CacheAnalyticsDashboard>> _loggerMock = new();

    private CacheAnalyticsDashboard CreateDashboard(
        int topN = 5,
        double lowHitRateThreshold = 0.3,
        TimeSpan? coldKeyAge = null) =>
        new(_loggerMock.Object, topN, lowHitRateThreshold, coldKeyAge);

    // ─── RecordHit / RecordMiss ───────────────────────────────────────────────

    [Fact]
    public void RecordHit_IncreasesHitCountForKey()
    {
        var dashboard = CreateDashboard();

        dashboard.RecordHit("user:1");
        dashboard.RecordHit("user:1");

        var stats = dashboard.GetKeyStats("user:1");
        stats.Should().NotBeNull();
        stats!.Hits.Should().Be(2);
        stats.Misses.Should().Be(0);
    }

    [Fact]
    public void RecordMiss_IncreasesMissCountForKey()
    {
        var dashboard = CreateDashboard();

        dashboard.RecordMiss("product:99");

        var stats = dashboard.GetKeyStats("product:99");
        stats.Should().NotBeNull();
        stats!.Misses.Should().Be(1);
        stats.Hits.Should().Be(0);
    }

    [Fact]
    public void RecordHit_WithEmptyKey_DoesNotThrow()
    {
        var dashboard = CreateDashboard();

        var act = () => dashboard.RecordHit(string.Empty);

        act.Should().NotThrow();
        dashboard.GetSnapshot().UniqueKeysTracked.Should().Be(0);
    }

    // ─── GetSnapshot ─────────────────────────────────────────────────────────

    [Fact]
    public void GetSnapshot_ReturnsCorrectAggregates()
    {
        var dashboard = CreateDashboard();

        dashboard.RecordHit("k1");
        dashboard.RecordHit("k1");
        dashboard.RecordMiss("k1");
        dashboard.RecordMiss("k2");

        var snap = dashboard.GetSnapshot();

        snap.TotalHits.Should().Be(2);
        snap.TotalMisses.Should().Be(2);
        snap.OverallHitRate.Should().BeApproximately(0.5, 0.001);
        snap.UniqueKeysTracked.Should().Be(2);
    }

    [Fact]
    public void GetSnapshot_HotKeys_OrderedByTotalAccessesDescending()
    {
        var dashboard = CreateDashboard(topN: 3);

        // k1 = 10 accesses, k2 = 5, k3 = 1
        for (var i = 0; i < 10; i++) dashboard.RecordHit("k1");
        for (var i = 0; i < 5; i++) dashboard.RecordMiss("k2");
        dashboard.RecordHit("k3");

        var snap = dashboard.GetSnapshot();

        snap.HotKeys.Should().HaveCount(3);
        snap.HotKeys[0].Key.Should().Be("k1");
        snap.HotKeys[1].Key.Should().Be("k2");
        snap.HotKeys[2].Key.Should().Be("k3");
    }

    [Fact]
    public void GetSnapshot_LowHitRateKeys_OnlyIncludesKeysWithMinFiveAccesses()
    {
        var dashboard = CreateDashboard(lowHitRateThreshold: 0.5);

        // 2 accesses = below threshold but < 5, should NOT appear
        dashboard.RecordHit("rare:1");
        dashboard.RecordMiss("rare:1");

        // 6 accesses, hit rate 1/6 ~ 0.17 = below 0.5 threshold AND >= 5, should appear
        dashboard.RecordHit("frequent-miss:1");
        for (var i = 0; i < 5; i++) dashboard.RecordMiss("frequent-miss:1");

        var snap = dashboard.GetSnapshot();

        snap.LowHitRateKeys.Should().Contain(s => s.Key == "frequent-miss:1");
        snap.LowHitRateKeys.Should().NotContain(s => s.Key == "rare:1");
    }

    [Fact]
    public void GetSnapshot_ColdKeys_IncludesKeysNotAccessedWithinColdAge()
    {
        var dashboard = CreateDashboard(coldKeyAge: TimeSpan.FromMilliseconds(1));

        dashboard.RecordHit("stale:key");
        Thread.Sleep(10); // ensure the key ages past the cold threshold

        var snap = dashboard.GetSnapshot();

        snap.ColdKeys.Should().Contain(s => s.Key == "stale:key");
    }

    // ─── HitRate ─────────────────────────────────────────────────────────────

    [Fact]
    public void KeyAccessStats_HitRate_ReturnsZeroWhenNoAccesses()
    {
        var stats = new KeyAccessStats { Key = "new:key" };
        stats.HitRate.Should().Be(0);
    }

    [Fact]
    public void KeyAccessStats_HitRate_ComputedCorrectly()
    {
        var stats = new KeyAccessStats { Key = "k" };
        stats.Hits = 3;
        stats.Misses = 1;

        stats.HitRate.Should().BeApproximately(0.75, 0.001);
    }

    // ─── Reset ───────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsAllCounters()
    {
        var dashboard = CreateDashboard();

        dashboard.RecordHit("k1");
        dashboard.RecordMiss("k2");
        dashboard.Reset();

        var snap = dashboard.GetSnapshot();
        snap.TotalHits.Should().Be(0);
        snap.TotalMisses.Should().Be(0);
        snap.UniqueKeysTracked.Should().Be(0);
    }

    // ─── RenderReport ─────────────────────────────────────────────────────────

    [Fact]
    public void RenderReport_ContainsOverviewSection()
    {
        var dashboard = CreateDashboard();
        dashboard.RecordHit("product:1");
        dashboard.RecordMiss("product:2");

        var report = dashboard.RenderReport();

        report.Should().Contain("Cache Analytics Dashboard");
        report.Should().Contain("Total hits");
        report.Should().Contain("Total misses");
        report.Should().Contain("Overall hit rate");
    }

    [Fact]
    public void RenderReport_WhenEmpty_DoesNotThrow()
    {
        var dashboard = CreateDashboard();

        var act = () => dashboard.RenderReport();
        act.Should().NotThrow();
    }
}
