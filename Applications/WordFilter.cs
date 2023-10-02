using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace HorizonBot.Applications
{
    public class WordFilter
    {
        private readonly Config _config;
        private readonly DiscordClient _client;

        public WordFilter(DiscordClient client, Config config)
        {
            _config = config;
            _client = client;
        }

        public async Task HandleMessageAsync(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (!_config.WordFilter.Enabled) return;

            var message = e.Message;
            var messageContent = message.Content;

            if (HasBannedWord(messageContent))
            {
                var guild = e.Guild;
                var channel = e.Channel;

                var messageSender = await GetMemberAsync(guild, message.Author.Id);
                if (messageSender != null && messageSender.IsOwner) return;

                string userName = messageSender != null ? messageSender.Username : "Unknown User";

                var botMember = await GetMemberAsync(guild, sender.CurrentUser.Id);
                if (botMember == null)
                {
                    _client.Logger.Log(LogLevel.Warning, new EventId(), "[Word Filter] Unable to access bot");
                    return;
                }

                bool messagedDeleted = false;
                if (_config.WordFilter.DeleteMessage)
                {
                    if (botMember.PermissionsIn(channel).HasPermission(Permissions.ManageMessages))
                    {
                        await message.DeleteAsync();
                        messagedDeleted = true;
                    }
                    else
                        _client.Logger.Log(LogLevel.Warning, new EventId(), $"[Word Filter] Unable to delete message[{message.Id}] from user {userName} - bot permission required");
                }

                bool userBanned = false;
                if (_config.WordFilter.BanUser)
                {
                    if (messageSender != null)
                    {
                        if (botMember.PermissionsIn(channel).HasPermission(Permissions.BanMembers))
                        {
                            await guild.BanMemberAsync(messageSender, _config.WordFilter.DaysOfMessagesToDelete, _config.WordFilter.BanReason);
                            userBanned = true;
                        }
                        else
                            _client.Logger.Log(LogLevel.Warning, new EventId(), $"[Word Filter] Unable to ban user {userName} for message[{message.Id}] - bot permission required");
                    }
                    else
                        _client.Logger.Log(LogLevel.Warning, new EventId(), "[Word Filter] Unable to access user");
                }

                ulong logChannelID = _config.WordFilter.LogChannelID;
                if (logChannelID > 0)
                {
                    var logChannel = await GetChannelAsync(logChannelID);
                    if (logChannel != null)
                    {
                        string isDeleted = messagedDeleted ? "\n:white_check_mark:⠀Message Deleted" : string.Empty;
                        string isBanned = userBanned ? "\n:white_check_mark:⠀User Banned" : string.Empty;

                        var embedMessage = new DiscordEmbedBuilder
                        {
                            Title = ":notepad_spiral:  Word Filter",
                            Color = new DiscordColor(15105570)
                        };

                        var userLink = messageSender != null ? $"<@{messageSender.Id}>" : userName;

                        embedMessage.AddField("User", userLink, false);
                        embedMessage.AddField("Message", $"\"{messageContent}\"", false);
                        embedMessage.AddField("Channel", $"<#{channel.Id}>", false);

                        if (messagedDeleted || userBanned)
                            embedMessage.AddField("Actions", isDeleted + isBanned, false);

                        await logChannel.SendMessageAsync(embed: embedMessage);
                    }
                    else
                        _client.Logger.Log(LogLevel.Warning, new EventId(), $"[Word Filter] Log channel with ID {logChannelID} not found");
                }
            }
        }

        #region Helpers
        bool HasBannedWord(string message)
        {
            foreach (string word in StripPunctuation(message.ToLower()).Split(' '))
            {
                if (_config.WordFilter.WordList.Contains(word))
                    return true;
            }
            return false;
        }

        HashSet<char> punc = new HashSet<char>() { '$', '^', '+', '|', '<', '>', '=', '–' };
        string StripPunctuation(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (!char.IsPunctuation(c) && !punc.Contains(c))
                    sb.Append(c);
            }
            return sb.ToString();
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