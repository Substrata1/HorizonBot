using DSharpPlus;
using DSharpPlus.EventArgs;
using HorizonBot.Applications;

namespace HorizonBot.Handlers
{
    public class MessageHandler
    {
        private readonly Config _config;
        private readonly DiscordClient _client;
        private readonly WordFilter _wordFilter;

        public MessageHandler(DiscordClient client, Config config)
        {
            _config = config;
            _client = client;
            _client.MessageCreated += OnMessageCreated;
            _wordFilter = new WordFilter(_client, _config);
        }

        private async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot) return;
            await _wordFilter.HandleMessageAsync(sender, e);
        }
    }
}