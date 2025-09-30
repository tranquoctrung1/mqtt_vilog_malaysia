using Microsoft.Extensions.Configuration;
using MQTT_Vilog_Malaysia.ConfigClass;
using MQTT_Vilog_Malaysia.MQTT;

internal class Program
{
    private static async Task Main(string[] args)
    {

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