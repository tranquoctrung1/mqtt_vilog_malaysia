using MQTT_Vilog_Malaysia.Actions;
using MQTT_Vilog_Malaysia.Models;

namespace MQTT_Vilog_Malaysia.Tests;

/// <summary>
/// Subclass overrides external I/O so tests run without real file or HTTP.
/// </summary>
public class TestableNotificationAction : NotificationAction
{
    public string FakeAccessToken { get; set; } = "fake-access-token-xyz";
    public bool GetAccessTokenCalled { get; private set; }
    public bool PushNotificationCalled { get; private set; }
    public string? CapturedAccessToken { get; private set; }
    public List<string>? CapturedTokens { get; private set; }
    public string? CapturedTitle { get; private set; }
    public string? CapturedBody { get; private set; }

    public override string GetAccessToken(string serviceAccountKeyFilePath, string[] scopes)
    {
        GetAccessTokenCalled = true;
        return FakeAccessToken;
    }

    public override void PushNotification(string accessToken, List<string> fcmToken, string titleNoti, string bodyNoti)
    {
        PushNotificationCalled = true;
        CapturedAccessToken = accessToken;
        CapturedTokens = new List<string>(fcmToken);
        CapturedTitle = titleNoti;
        CapturedBody = bodyNoti;
    }
}

public class NotificationActionTests
{
    private static List<DeviceTokenAppModel> MakeTokenList(params string[] tokens)
        => tokens.Select(t => new DeviceTokenAppModel { DeviceToken = t, UserName = "user", Status = true, Sound = true }).ToList();

    [Fact]
    public async Task SubmitNotification_AlwaysReturnsTrue()
    {
        var sut = new TestableNotificationAction();
        var tokens = MakeTokenList("token-1");

        var result = await sut.SubmitNotification("logger-01", "Title", "Body", tokens);

        Assert.True(result);
    }

    [Fact]
    public async Task SubmitNotification_CallsGetAccessToken()
    {
        var sut = new TestableNotificationAction();

        await sut.SubmitNotification("logger-01", "Title", "Body", MakeTokenList("token-1"));

        Assert.True(sut.GetAccessTokenCalled);
    }

    [Fact]
    public async Task SubmitNotification_CallsPushNotification()
    {
        var sut = new TestableNotificationAction();

        await sut.SubmitNotification("logger-01", "Title", "Body", MakeTokenList("token-1"));

        Assert.True(sut.PushNotificationCalled);
    }

    [Fact]
    public async Task SubmitNotification_PassesCorrectTitleAndBody()
    {
        var sut = new TestableNotificationAction();

        await sut.SubmitNotification("logger-01", "Alert Title", "Alert Body", MakeTokenList("token-1"));

        Assert.Equal("Alert Title", sut.CapturedTitle);
        Assert.Equal("Alert Body", sut.CapturedBody);
    }

    [Fact]
    public async Task SubmitNotification_PassesAccessTokenFromGetAccessToken()
    {
        var sut = new TestableNotificationAction { FakeAccessToken = "my-test-token" };

        await sut.SubmitNotification("logger-01", "Title", "Body", MakeTokenList("token-1"));

        Assert.Equal("my-test-token", sut.CapturedAccessToken);
    }

    [Fact]
    public async Task SubmitNotification_ExtractsAllDeviceTokens()
    {
        var sut = new TestableNotificationAction();
        var tokens = MakeTokenList("token-A", "token-B", "token-C");

        await sut.SubmitNotification("logger-01", "Title", "Body", tokens);

        Assert.NotNull(sut.CapturedTokens);
        Assert.Equal(3, sut.CapturedTokens!.Count);
        Assert.Contains("token-A", sut.CapturedTokens);
        Assert.Contains("token-B", sut.CapturedTokens);
        Assert.Contains("token-C", sut.CapturedTokens);
    }

    [Fact]
    public async Task SubmitNotification_EmptyTokenList_ReturnsTrue()
    {
        var sut = new TestableNotificationAction();

        var result = await sut.SubmitNotification("logger-01", "Title", "Body", new List<DeviceTokenAppModel>());

        Assert.True(result);
        Assert.True(sut.PushNotificationCalled);
        Assert.NotNull(sut.CapturedTokens);
        Assert.Empty(sut.CapturedTokens!);
    }

    [Fact]
    public async Task SubmitNotification_NullTokenList_Throws()
    {
        var sut = new TestableNotificationAction();

        await Assert.ThrowsAsync<NullReferenceException>(
            () => sut.SubmitNotification("logger-01", "Title", "Body", null!));
    }

    [Fact]
    public async Task SubmitNotification_SingleToken_OnlyThatTokenPassed()
    {
        var sut = new TestableNotificationAction();

        await sut.SubmitNotification("logger-01", "Title", "Body", MakeTokenList("only-token"));

        Assert.NotNull(sut.CapturedTokens);
        Assert.Single(sut.CapturedTokens!);
        Assert.Equal("only-token", sut.CapturedTokens![0]);
    }
}
