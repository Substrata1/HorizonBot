using DSharpPlus;
using DSharpPlus.EventArgs;
using HorizonBot.Applications;

namespace HorizonBot.Handlers
{
    public class ReactionHandler
    {
        private readonly Config _config;
        private readonly Data _data;
        private readonly DiscordClient _client;
        private readonly UserVerification _userVerification;
        private readonly RoleSelection _roleSelection;

        public ReactionHandler(DiscordClient client, Config config, Data data)
        {
            _config = config;
            _data = data;
            _client = client;
            _client.MessageReactionAdded += OnMessageReactionAdded;
            _client.MessageReactionRemoved += OnMessageReactionRemoved;
            _userVerification = new UserVerification(_client, _config, _data);
            _roleSelection = new RoleSelection(_client, _config, _data);
        }

        private async Task OnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot) return;
            await _userVerification.HandleReactionAddedAsync(sender, e);
            await _roleSelection.HandleReactionAddedAsync(sender, e);
        }

        private async Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            if (e.User.IsBot) return;
            await _roleSelection.HandleReactionRemovedAsync(sender, e);
        }
    }
}