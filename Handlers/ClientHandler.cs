using DSharpPlus;
using DSharpPlus.EventArgs;
using HorizonBot.Applications;
using Microsoft.Extensions.Logging;

namespace HorizonBot.Handlers
{
    public class ClientHandler
    {
        private readonly Config _config;
        private readonly Data _data;
        private readonly DiscordClient _client;
        private readonly UserVerification _userVerification;
        private readonly RoleSelection _roleSelection;

        public ClientHandler(DiscordClient client, Config config, Data data)
        {
            _config = config;
            _data = data;
            _client = client;
            _client.Ready += OnClientReady;
            _userVerification = new UserVerification(_client, _config, _data);
            _roleSelection = new RoleSelection(_client, _config, _data);
        }

        private Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            _client.Logger.Log(LogLevel.Information, new EventId(), "Bot connected");

            _userVerification.HandleClientReady(sender, e);
            _roleSelection.HandleClientReady(sender, e);

            return Task.CompletedTask;
        }
    }
}