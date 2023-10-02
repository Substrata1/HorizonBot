using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;

namespace HorizonBot.Applications
{
    public class RoleSelection
    {
        private readonly Config _config;
        private readonly Data _data;
        private readonly DiscordClient _client;

        public RoleSelection(DiscordClient client, Config config, Data data)
        {
            _config = config;
            _data = data;
            _client = client;
        }

        public async Task HandleClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            await SendRoleSelectMessage();
        }

        public async Task HandleReactionAddedAsync(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            await HandleRoleSelectActionAsync(sender, e.Guild, e.Channel, e.Message, e.User, e.Emoji, "grant");
        }

        public async Task HandleReactionRemovedAsync(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            if (_config.RoleSelect.RemoveReactionRevokesRole)
                await HandleRoleSelectActionAsync(sender, e.Guild, e.Channel, e.Message, e.User, e.Emoji, "revoke");
        }

        private async Task HandleRoleSelectActionAsync(DiscordClient sender, DiscordGuild guild, DiscordChannel channel, DiscordMessage message, DiscordUser user, DiscordEmoji emoji, string action)
        {
            DiscordChannel roleSelectChannel = await GetChannelAsync(_config.RoleSelect.ChannelID);
            if (roleSelectChannel == null || channel == null || channel != roleSelectChannel) return;

            DiscordMessage roleSelectMessage = await GetMessageAsync(roleSelectChannel, _data.RoleSelect.MessageID);
            if (roleSelectMessage == null || message == null || message != roleSelectMessage) return;

            var emojiName = emoji.GetDiscordName();
            if (!_config.RoleSelect.Roles.ContainsKey(emojiName)) return;

            var desiredRoleID = _config.RoleSelect.Roles[emojiName];
            var desiredRole = guild.GetRole(desiredRoleID);
            if (desiredRole == null)
            {
                _client.Logger.Log(LogLevel.Warning, new EventId(), $"[Role Selection] Role with ID {desiredRoleID} does not exist");
                return;
            }

            DiscordMember member = await GetMemberAsync(guild, user.Id);
            if (member == null) return;

            var botMember = await GetMemberAsync(guild, sender.CurrentUser.Id);

            bool hasDesiredRole = member.Roles.Contains(desiredRole);

            if (action == "grant" && !hasDesiredRole)
            {
                if (HasElevatedRole(botMember, desiredRole))
                    await member.GrantRoleAsync(desiredRole);
                else
                    _client.Logger.Log(LogLevel.Warning, new EventId(), $"[User Verification] Cannot grant role. Bot role must be higher than '{desiredRole.Name}'.");
            }
            else if (action == "revoke" && hasDesiredRole)
            {
                if (HasElevatedRole(botMember, desiredRole))
                    await member.RevokeRoleAsync(desiredRole);
                else
                    _client.Logger.Log(LogLevel.Warning, new EventId(), $"[User Verification] Cannot revoke role. Bot role must be higher than '{desiredRole.Name}'.");
            }
        }

        public async Task SendRoleSelectMessage(CommandContext ctx = null)
        {
            if (!_config.RoleSelect.Enabled) return;

            var channelID = _config.RoleSelect.ChannelID;
            DiscordChannel channel = await GetChannelAsync(channelID);
            if (channel == null)
            {
                _client.Logger.Log(LogLevel.Warning, new EventId(), $"[Role Selection] Role selection channel with ID {channelID} not found");
                return;
            }

            var messageID = _data.RoleSelect.MessageID;
            DiscordMessage message = await GetMessageAsync(channel, messageID);

            if (message == null || messageID == 0)
            {
                if (ctx == null)
                {
                    if (message == null && messageID != 0)
                        _client.Logger.Log(LogLevel.Information, new EventId(), $"[Role Selection] Role selection message with ID {channelID} not found in channel. Creating a new one...");
                    else
                        _client.Logger.Log(LogLevel.Information, new EventId(), $"[Role Selection] Creating a new role selection message in {channel.Name}[{channelID}]...");
                }
                else
                    await ctx.RespondAsync($"Creating a new role selection message in <#{channelID}>...");

                var embedMessage = new DiscordEmbedBuilder()
                {
                    Title = string.Format(_config.RoleSelect.MessageTitle, channel.Guild.Name),
                    Description = _config.RoleSelect.MessageDescription,
                    Color = DiscordColor.CornflowerBlue
                };

                var roleSelectMessage = await channel.SendMessageAsync(embed: embedMessage);

                if (roleSelectMessage != null)
                {
                    foreach (var emoji in _config.RoleSelect.Roles.Keys)
                    {
                        await roleSelectMessage.CreateReactionAsync(DiscordEmoji.FromName(_client, emoji));
                    }

                    _data.RoleSelect.MessageID = roleSelectMessage.Id;
                    await SaveDataToFile();
                }
            }
            else if (ctx != null)
                await ctx.RespondAsync($"Role selection message already exists: https://discord.com/channels/{ctx.Guild.Id}/{channelID}/{messageID}\nPlease delete this and try again.");
        }

        private async Task SaveDataToFile()
        {
            string path = "data.json";
            var jsonData = JsonConvert.SerializeObject(_data, Formatting.Indented);
            await File.WriteAllTextAsync(path, jsonData);
        }

        public async Task RefreshRoleEmojis(CommandContext ctx)
        {
            ulong roleSelectChannelID = _config.RoleSelect.ChannelID;
            DiscordChannel roleSelectChannel = await GetChannelAsync(roleSelectChannelID);
            if (roleSelectChannel == null)
            {
                await ctx.RespondAsync($"Role selection channel with ID {roleSelectChannelID} not found");
                return;
            }

            ulong roleSelectMessageID = _data.RoleSelect.MessageID;
            DiscordMessage roleSelectMessage = await GetMessageAsync(roleSelectChannel, roleSelectMessageID);
            if (roleSelectMessage == null)
            {
                await ctx.RespondAsync($"Role selection message with ID {roleSelectMessageID} not found");
                return;
            }

            foreach (var reaction in roleSelectMessage.Reactions)
            {
                if (!_config.RoleSelect.Roles.ContainsKey(reaction.Emoji.GetDiscordName()))
                {
                    await roleSelectMessage.DeleteReactionsEmojiAsync(reaction.Emoji);
                }
            }

            foreach (var reactionName in _config.RoleSelect.Roles.Keys)
            {
                var emoji = DiscordEmoji.FromName(_client, reactionName);
                if (emoji == null) return;

                if (!roleSelectMessage.Reactions.Select(x => x.Emoji).Contains(emoji))
                    await roleSelectMessage.CreateReactionAsync(emoji);
            }
        }

        #region Helpers
        private bool HasElevatedRole(DiscordMember member, DiscordRole checkRole)
        {
            foreach (var role in member.Roles)
            {
                if (role.Position > checkRole.Position)
                    return true;
            }

            return false;
        }

        private async Task<DiscordChannel> GetChannelAsync(ulong channelID)
        {
            try
            {
                return await _client.GetChannelAsync(channelID);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        private async Task<DiscordMessage> GetMessageAsync(DiscordChannel channel, ulong messageID)
        {
            try
            {
                return await channel.GetMessageAsync(messageID);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        private async Task<DiscordMember> GetMemberAsync(DiscordGuild guild, ulong userId)
        {
            try
            {
                return await guild.GetMemberAsync(userId);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }
        #endregion
    }
}