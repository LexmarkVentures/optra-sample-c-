using System;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

class Program
{

    private static double currentTemperature;
    private static int frequencyInSeconds = 10;
    private static Boolean terminating = false;

    /**
     * Responds to twin property change events. These will be the inputs,
     * as defined when you add your skill to the portal.
     */
    static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
    {
        try
        {
            Console.WriteLine("Desired property change:");
            Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
            if (desiredProperties["inputs"] != null && !string.IsNullOrWhiteSpace(desiredProperties["inputs"]["FrequencyInSeconds"].ToString()))
            {

                int.TryParse(desiredProperties["inputs"]["FrequencyInSeconds"].ToString(), out frequencyInSeconds);

            }

        }
        catch (AggregateException ae)
        {
            foreach (Exception exception in ae.InnerExceptions)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", exception);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine();
            Console.WriteLine("Error when receiving desired property.");
            Console.WriteLine(e);
        }

        return Task.CompletedTask;
    }


    static void Main(string[] args)
    {
        // Required to more gracefully end the program.
        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Program.terminating = true;
        };

        // Run the main application loop.
        SkillMain().Wait();
    }

    static async Task SkillMain()
    {
        Console.WriteLine("Skill Initialized at: {0}", DateTimeOffset.UtcNow);

        try
        {
            // Initialize the Client
            Console.WriteLine("Initializing Module Client");
            ModuleClient client = await ModuleClient.CreateFromEnvironmentAsync(TransportType.Mqtt_WebSocket_Only);
            await client.OpenAsync();
            Console.WriteLine("Module Client Initialized");

            // Ensure device receives and processes desired property updates.
            Twin twin = await client.GetTwinAsync();
            await OnDesiredPropertiesUpdate(twin.Properties.Desired, client);
            await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // Display all optra environment variables
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                if (entry.Key.ToString().StartsWith("OPTRA"))
                {
                    Console.WriteLine("Optra Environment variable set: {0}: {1}", entry.Key, entry.Value);
                }
            }

            var iterations = 0;
            while (!terminating)
            {
                iterations += 1;
                string messageString = ConstructMessage().toJsonString();
                byte[] messageContent = Encoding.UTF8.GetBytes(messageString);
                Message message = new Message(messageContent);
                Console.WriteLine("Sending Message to IotHub: {0}", messageString);

                /* This message will produce an output called "temperature" for the portal.
                    Example Message -> {"createdAt":63814055590662,"data":{"temperature":0.4}}
                */

                client.SendEventAsync(message);

                try
                {
                    System.Threading.Thread.Sleep(frequencyInSeconds * 1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                Console.WriteLine("Iterations Passed: {0}", iterations);
            }

            Console.WriteLine("Closing Client");
            await client.CloseAsync();
            Console.WriteLine("Client has been closed");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

    }

    private static SkillMessage ConstructMessage()
    {
        SkillMessage message = new SkillMessage();
        Random random = new Random();
        double temperatureChange = -0.3 + random.NextDouble() * 0.6;
        currentTemperature += temperatureChange;
        currentTemperature = Math.Round(currentTemperature * 10) / 10;

        message.addData("temperature", currentTemperature);

        return message;
    }
}


