# NMyQ

Based on the work of https://github.com/hjdhjd/myq this library implements support for logging into the MyQ service, querying devices & sending commands. Currently in use with some of my own home automation code & services.

Logging in and triggering is as simple as:

            var client = new NMyQClient(logger);
            string username = "";
            string password = "";
            await client.Login(username, password);
            var devices = await client.GetDevicesAsync();
            await client.SendDoorCommand("garage_door_serial", GarageDoorCommands.Open);

In order to behave more like an official client, it is advised that you enable support for saving of the refresh token and other state (account id & access token expiry most of all) in between runs of the program.

One simple implementation of doing so is like the following prior to calling Login:

            client.RegisterConfigurationSaver((MyQConfiguration config) =>
            {
                File.WriteAllText("myq.config", JsonSerializer.Serialize<MyQConfiguration>(config));
            });
            //Doesn't check if file exists, use this on second run
            client.LoadConfig(JsonSerializer.Deserialize<MyQConfiguration>(File.ReadAllText("myq.config")));

Modify as needed to support storing data in S3/Azure Storage/AWS Secrets Manager/Azure KeyVault, etc.
