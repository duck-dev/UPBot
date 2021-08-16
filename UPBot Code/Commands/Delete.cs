﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command will delete the last x messages
/// or the last x messages of a specific user
/// author: Duck
/// </summary>
public class Delete : BaseCommandModule
{
    private const int MessageLimit = 50;
    private const string CallbackLimitExceeded = ", since you can't delete more than 50 messages at a time.";

    /// <summary>
    /// Delete the last x messages of any user
    /// </summary>
    [Command("delete")]
    [Aliases("clear", "purge")]
    [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
    [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
    public async Task DeleteCommand(CommandContext ctx, int count)
    {
        UtilityFunctions.LogUserCommand(ctx);
        if (count <= 0)
        {
            await ErrorCallback(CommandErrors.InvalidParamsDelete, ctx, count);
            return;
        }

        bool limitExceeded = CheckLimit(count);

        var messages = ctx.Channel.GetMessagesAsync(count + 1).Result;
        await DeleteMessages(ctx, messages);

        await Success(ctx, limitExceeded, count);
    }

    /// <summary>
    /// Delete the last x messages of the specified user
    /// </summary>
    [Command("delete")]
    [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
    [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
    public async Task DeleteCommand(CommandContext ctx, DiscordMember targetUser, int count)
    {
        UtilityFunctions.LogUserCommand(ctx);
        if (count <= 0)
        {
            await ErrorCallback(CommandErrors.InvalidParamsDelete, ctx, count);
            return;
        }

        bool limitExceeded = CheckLimit(count);

        var allMessages = ctx.Channel.GetMessagesAsync().Result; // Get last 100 messages
        var userMessages = allMessages.Where(x => x.Author == targetUser).Take(count);
        await DeleteMessages(ctx, userMessages);

        await Success(ctx, limitExceeded, count, targetUser);
    }

    /// <summary>
    /// The core-process of deleting the messages
    /// </summary>
    public async Task DeleteMessages(CommandContext ctx, IEnumerable<DiscordMessage> messages)
    {
        foreach (DiscordMessage m in messages)
        {
            if (m != ctx.Message)
                await m.DeleteAsync();
        }
    }

    /// <summary>
    /// Will be called at the end of every execution of this command and tells the user that the execution succeeded
    /// including a short summary of the command (how many messages, by which user etc.)
    /// </summary>
    private async Task Success(CommandContext ctx, bool limitExceeded, int count, DiscordMember targetUser = null)
    {
        string mentionUserStr = targetUser == null ? string.Empty : $"by '{targetUser.DisplayName}'";
        string overLimitStr = limitExceeded ? CallbackLimitExceeded : string.Empty;
        string messagesLiteral = UtilityFunctions.PluralFormatter(count, "message", "messages");
        string hasLiteral = UtilityFunctions.PluralFormatter(count, "has", "have");

        await ctx.Message.DeleteAsync();
        string message = $"The last {count} {messagesLiteral} {mentionUserStr} {hasLiteral} been successfully deleted{overLimitStr}.";

        await UtilityFunctions.BuildEmbedAndExecute("Success", message, UtilityFunctions.Green, ctx, true);
    }

    private async Task ErrorCallback(CommandErrors error, CommandContext ctx, params object[] additionalParams)
    {
        string message = string.Empty;
        switch (error)
        {
            case CommandErrors.InvalidParams:
                message = $"Invalid params for the command {ctx.Command.Name}.";
                break;
            case CommandErrors.InvalidParamsDelete:
                if (additionalParams[0] is int count)
                    message = $"You can't delete {count} messages. Try to eat {count} apples, does that make sense?";
                else
                    goto case CommandErrors.InvalidParams;
                break;
        }
        await UtilityFunctions.BuildEmbedAndExecute("Error", message, UtilityFunctions.Red, ctx, true);
    }

    private bool CheckLimit(int count)
    {
        return count > MessageLimit;
    }
}