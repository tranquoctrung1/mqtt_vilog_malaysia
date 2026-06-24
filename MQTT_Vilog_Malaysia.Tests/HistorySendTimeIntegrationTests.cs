using MQTT_Vilog_Malaysia.Actions;
using MQTT_Vilog_Malaysia.Models;

namespace MQTT_Vilog_Malaysia.Tests;

/// <summary>
/// Integration tests — hit real MongoDB.
/// Run manually: dotnet test --filter "Category=Integration"
/// </summary>
public class HistorySendTimeIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task InsertHistorySendTime_RealMongoDB_InsertsAndIsRetrievable()
    {
        const string loggerid = "TEST-LOGGER-SENDTIME";
        var now = DateTime.Now;

        using (var action = new HistorySendTimeAction())
        {
            var his = new HistorySendTimeModel
            {
                SiteId = loggerid,
                Location = "TEST-LOCATION",
                LoggerId = loggerid,
                TimeStamp = now
            };

            await action.InsertHistorySendTime(his);
        }

        using var verify = new HistorySendTimeAction();
        var list = await verify.GetHistorySendTime(loggerid);

        Console.WriteLine($"[Integration] Records found for {loggerid}: {list.Count}");

        Assert.True(list.Count > 0, "No record found in t_historiesSendTime after insert.");
        Assert.Equal(loggerid, list[0].SiteId);
        Assert.Equal("TEST-LOCATION", list[0].Location);
        Assert.NotNull(list[0].TimeStamp);
    }
}
