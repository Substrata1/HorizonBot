using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HorizonBot.Applications
{
    public class UserVerification
    {
        private readonly Config _config;
        private readonly Data _data;
        private readonly DiscordClient _client;

        public UserVerification(DiscordClient client, Config config, Data data)
        {
            _config = config;
            _data = data;
            _client = client;
        }

        public async Task HandleClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            await SendVerificationMessage();
        }

        public async Task HandleReactionAddedAsync(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            var guild = e.Guild;
            var channel = e.Channel;
            var message = e.Message;

            DiscordChannel verificationChannel = await GetChannelAsync(_config.Verification.ChannelID);
            if (verificationChannel == null || channel == null || channel != verificationChannel) return;

            DiscordMessage verificationMessage = await GetMessageAsync(verificationChannel, _data.Verification.MessageID);
            if (verificationMessage == null || message == null || message != verificationMessage) return;

            var emojiName = e.Emoji.GetDiscordName();
            if (emojiName != ":white_check_mark:") return;

            var verifiedRole = guild.GetRole(_config.Verification.RoleID);
            if (verifiedRole == null)
            {
                _client.Logger.Log(LogLevel.Warning, new EventId(), $"[User Verification] Verified role with ID {_config.Verification.RoleID} not found");
                return;
            }

            DiscordMember member = await GetMemberAsync(guild, e.User.Id);
            if (member == null) return;

            var botMember = await GetMemberAsync(guild, sender.CurrentUser.Id);

            if (!member.Roles.Contains(verifiedRole))
            {
                if (HasElevatedRole(botMember, verifiedRole))
                    await member.GrantRoleAsync(verifiedRole);
                else
                    _client.Logger.Log(LogLevel.Warning, new EventId(), $"[User Verification] Cannot assign role. Bot role must be higher than '{verifiedRole.Name}'.");
            }
        }

        public async Task SendVerificationMessage(CommandContext ctx = null)
        {
            if (!_config.Verification.Enabled) return;

            var channelID = _config.Verification.ChannelID;
            DiscordChannel channel = await GetChannelAsync(channelID);
            if (channel == null)
            {
                _client.Logger.Log(LogLevel.Warning, new EventId(), $"[User Verification] Verification channel with ID {channelID} not found");
                return;
            }

            var messageID = _data.Verification.MessageID;
            DiscordMessage message = await GetMessageAsync(channel, messageID);

            if (message == null || messageID == 0)
            {
                if (ctx == null)
                {
                    if (message == null && messageID != 0)
                        _client.Logger.Log(LogLevel.Information, new EventId(), $"[User Verification] Verification message with ID {channelID} not found in channel. Creating a new one...");
                    else
                        _client.Logger.Log(LogLevel.Information, new EventId(), $"[User Verification] Creating a new verification message in {channel.Name}[{channelID}]...");
                }
                else
                    await ctx.RespondAsync($"Creating a new verification message in <#{channelID}>...");

                var embedMessage = new DiscordEmbedBuilder()
                {
                    Title = string.Format(_config.Verification.MessageTitle, channel.Guild.Name),
                    Description = _config.Verification.MessageDescription,
                    Color = DiscordColor.CornflowerBlue
                };

                var verificationMessage = await channel.SendMessageAsync(embed: embedMessage);

                if (verificationMessage != null)
                {
                    await verificationMessage.CreateReactionAsync(DiscordEmoji.FromName(_client, ":white_check_mark:"));

                    _data.Verification.MessageID = verificationMessage.Id;
                    await SaveDataToFile();
                }
            }
            else if (ctx != null)
                await ctx.RespondAsync($"Verification message already exists: https://discord.com/channels/{ctx.Guild.Id}/{channelID}/{messageID}\nPlease delete this and try again.");
        }

        private async Task SaveDataToFile()
        {
            string path = "data.json";
            var jsonData = JsonConvert.SerializeObject(_data, Formatting.Indented);
            await File.WriteAllTextAsync(path, jsonData);
        }

        #region Helpers
        private bool HasElevatedRole(DiscordMember member, DiscordRole checkRole)
        {
            foreach(var role in member.Roles)
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