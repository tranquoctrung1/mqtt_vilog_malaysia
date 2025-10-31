using Microsoft.Extensions.Configuration;
using MQTT_Vilog_Malaysia.Actions;
using MQTT_Vilog_Malaysia.ConfigClass;
using MQTT_Vilog_Malaysia.Models;
using MQTT_Vilog_Malaysia.MQTT;
using System.Security.Claims;

internal class Program
{
    private static async Task Main(string[] args)
    {

        //// push notification
        //using (DeviceTokenAppAction deviceTokenAppAction = new DeviceTokenAppAction())
        //{
        //    List<DeviceTokenAppModel> listToken = await deviceTokenAppAction.GetDeivceTokenApps();
        //    if (listToken.Count > 0)
        //    {
        //        using (NotificationAction notificationAction = new NotificationAction())
        //        {
        //            await notificationAction.SubmitNotification("PWSTEST", "PWSTEST", "Channel 2.7 Battery with value 3.4 is Lower battery", listToken);
        //            await notificationAction.SubmitNotification("PWSTEST", "PWSTEST", "Channel 2.6 Signal with value 20 is Lower Signal", listToken);
        //            await notificationAction.SubmitNotification("PWSTEST", "PWSTEST", "Channel 2.1 Forward Flow  with value 20 is higher threshold", listToken);
        //            await notificationAction.SubmitNotification("PWSTEST", "PWSTEST", "Channel 2.2 Reverse Flow  with value 3 is lower threshold", listToken);
        //        }
        //    }
        //}

        IConfiguration config = new ConfigurationBuilder()
          .AddJsonFile("settings.json")
           .AddEnvironmentVariables()
           .Build();

        Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

        Subscribe sub = new Subscribe();

        await sub.Handle_Received_Application_Message(settings.IpMQTT, int.Parse(settings.Port), settings.IpCheck);


        Console.ReadLine();

    }
}