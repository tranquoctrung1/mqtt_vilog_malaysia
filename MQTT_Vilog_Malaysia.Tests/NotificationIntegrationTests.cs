using MQTT_Vilog_Malaysia.Actions;
using MQTT_Vilog_Malaysia.Models;

namespace MQTT_Vilog_Malaysia.Tests;

/// <summary>
/// Integration tests — hit real MongoDB + real FCM.
/// Run manually: dotnet test --filter "Category=Integration"
/// </summary>
public class NotificationIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SubmitNotification_RealFCM_WithTokensFromMongoDB()
    {
        // Lấy token từ MongoDB thật
        List<DeviceTokenAppModel> listToken;
        using (var deviceAction = new DeviceTokenAppAction())
        {
            listToken = await deviceAction.GetDeivceTokenApps();
        }

        Console.WriteLine($"[Integration] Token count from MongoDB: {listToken.Count}");
        foreach (var t in listToken)
        {
            Console.WriteLine($"  User={t.UserName} | Token={t.DeviceToken?[..Math.Min(20, t.DeviceToken.Length)]}... | Status={t.Status}");
        }

        Assert.True(listToken.Count > 0, "No device tokens in MongoDB — insert at least one to test.");

        // Gọi FCM thật
        using var notif = new NotificationAction();
        var result = await notif.SubmitNotification(
            loggerID: "TEST-LOGGER",
            title: "Test Push",
            body: $"Integration test lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}",
            listTokenApp: listToken
        );

        Assert.True(result);
        Console.WriteLine("[Integration] SubmitNotification returned true — check device for notification.");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SubmitNotification_RealFCM_WithHardcodedToken()
    {
        // Thay token này bằng FCM token thật từ app
        const string hardcodedToken = "REPLACE_WITH_REAL_FCM_TOKEN";

        if (hardcodedToken == "REPLACE_WITH_REAL_FCM_TOKEN")
        {
            Console.WriteLine("[Integration] Skipped — replace hardcodedToken with real FCM token.");
            return;
        }

        var listToken = new List<DeviceTokenAppModel>
        {
            new() { DeviceToken = hardcodedToken, UserName = "manual-test", Status = true, Sound = true }
        };

        using var notif = new NotificationAction();
        var result = await notif.SubmitNotification(
            loggerID: "TEST-LOGGER",
            title: "Test Push Manual",
            body: $"Test lúc {DateTime.Now:HH:mm:ss}",
            listTokenApp: listToken
        );

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAccessToken_RealKey_ReturnsNonEmptyToken()
    {
        // Chỉ test lấy token Firebase — không gửi notification
        using var notif = new NotificationAction();
        string[] scopes = {
            "https://www.googleapis.com/auth/firebase.messaging",
            "https://www.googleapis.com/auth/cloud-platform"
        };

        var token = notif.GetAccessToken("C:\\web\\fbkey.json", scopes);

        Console.WriteLine($"[Integration] Access token (first 40 chars): {token?[..Math.Min(40, token.Length)]}...");
        Assert.False(string.IsNullOrEmpty(token));
    }
}
