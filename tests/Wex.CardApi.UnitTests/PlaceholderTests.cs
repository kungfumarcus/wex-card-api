using Xunit;

namespace Wex.CardApi.UnitTests;

public class PlaceholderTests
{
    // Real unit tests arrive with the business logic in the next iteration:
    //   - currency conversion math + rounding
    //   - rate selection (on/before purchase date, within 6 months) for Requirement #3
    //   - latest-rate selection for Requirement #4
    //   - available-balance calculation
    //   - request validation
    [Fact]
    public void Test_harness_is_wired_up()
    {
        Assert.True(true);
    }
}
