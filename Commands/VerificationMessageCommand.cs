using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using HorizonBot.Applications;

namespace HorizonBot.Commands
{
    public class VerificationMessageCommand : BaseCommandModule
    {
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly Data _data;

        public VerificationMessageCommand(DiscordClient client, Config config, Data data)
        {
            _config = config;
            _data = data;
            _client = client;
        }

        [Command("addverificationmessage")]
        [RequireOwner]
        [Cooldown(1, 30, CooldownBucketType.User)]
        public async Task VerificationMessage(CommandContext ctx)
        {
            var _userVerification = new UserVerification(_client, _config, _data);
            await _userVerification.SendVerificationMessage(ctx);
        }
    }
}