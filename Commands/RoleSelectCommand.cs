using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using HorizonBot.Applications;

namespace HorizonBot.Commands
{
    public class RoleSelectCommand : BaseCommandModule
    {
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly Data _data;
        private readonly RoleSelection _roleSelection;

        public RoleSelectCommand(DiscordClient client, Config config, Data data)
        {
            _config = config;
            _data = data;
            _client = client;
            _roleSelection = new RoleSelection(_client, _config, _data);
        }

        [Command("addroleselectmessage")]
        [RequireOwner]
        [Cooldown(1, 30, CooldownBucketType.User)]
        public async Task RoleSelectMessage(CommandContext ctx)
        {
            await _roleSelection.SendRoleSelectMessage(ctx);
        }

        [Command("refreshroleemojis")]
        [RequireOwner]
        [Cooldown(1, 30, CooldownBucketType.User)]
        public async Task RoleSelectEmojiRefresh(CommandContext ctx)
        {
            await _roleSelection.RefreshRoleEmojis(ctx);
        }
    }
}