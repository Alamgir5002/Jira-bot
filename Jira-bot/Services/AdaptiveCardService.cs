﻿using Jira_bot.Interfaces;
using Jira_bot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using Jira_bot.Bots;

namespace Jira_bot.Services
{
    public class AdaptiveCardService
    {
        private readonly ILogger<AdaptiveCardService> logger;
        private ISourceWorklogService sourceWorklogService;
        private const string JIRA_BOT_MESSAGE = "Added from JIRA-Bot";
        private BotAdaptiveCards adaptiveCards;
        public AdaptiveCardService(ILogger<AdaptiveCardService> logger, ISourceWorklogService sourceWorklogService)
        {
            this.logger = logger;
            this.sourceWorklogService = sourceWorklogService;
            adaptiveCards = new BotAdaptiveCards();
        }

        public async Task processWorklogCard(ITurnContext<IMessageActivity> turnContext, JObject jsonData, CancellationToken cancellationToken)
        {
            var worklogDetails = new SourceWorkLog
            {
                issueId = (string)jsonData["IssueNumber"],
                body = new WorklogDetailsBody
                {
                    started = ((DateTime)jsonData["Date"]).ToString("yyyy-MM-ddTHH:mm:ss.fffzzss"),
                    timeSpentSeconds = (int)((TimeSpan)jsonData["Time"]).TotalSeconds,
                    comment = createCommentRecord(jsonData.ContainsKey("WorklogDescription") ? (string)jsonData["WorklogDescription"] : null)
                }
            };

            var replyText = await sourceWorklogService.AddWorklogForUser(worklogDetails, turnContext.Activity.From.Id);
            logger.LogDebug("Reply Text:" + replyText);
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        }

        private CommentRecord createCommentRecord(string description)
        {
            if (String.IsNullOrWhiteSpace(description))
            {
                description = JIRA_BOT_MESSAGE;
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(description);
                stringBuilder.Append(" - ");
                stringBuilder.Append(JIRA_BOT_MESSAGE);
                description = stringBuilder.ToString();
            }

            return new CommentRecord(
            content: new List<ContentRecord>
            {
                new ContentRecord(
                    content: new List<TextContentRecord>
                    {
                        new TextContentRecord(text: description)
                    }
                )
            });
        }

        public async Task processSourceCard(ITurnContext<IMessageActivity> turnContext, JObject jsonData, CancellationToken cancellationToken)
        {
            var sourceDetails = new SourceDetails()
            {
                SourceToken = (string)jsonData["SourceToken"],
                UserEmail = (string)jsonData["Email"],
                SourceURL = (string)jsonData["SourceURL"],
                Id = turnContext.Activity.From.Id
            };
            await sourceWorklogService.AddSourceDetails(sourceDetails, turnContext, cancellationToken);
        }

        public async Task ShowAddSourceCredentialsCard<TActivity>(ITurnContext<TActivity> turnContext, CancellationToken cancellationToken) where TActivity : IActivity
        {
            var cardAttachment = adaptiveCards.CreateSourceAdaptiveCard();
            var reply = MessageFactory.Attachment(cardAttachment);
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
        public async Task ShowAddWorklogCard(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (!sourceWorklogService.checkIfUserAlreadyRegistered(turnContext.Activity.From.Id))
            {
                string notFoundMessage = "Source details not found";
                logger.LogWarning($"{notFoundMessage} against user with id {turnContext.Activity.From.Id}");
                await turnContext.SendActivityAsync(MessageFactory.Text(notFoundMessage, notFoundMessage), cancellationToken);
                await ShowAddSourceCredentialsCard(turnContext, cancellationToken);
                return ;
            }

            var cardAttachment = adaptiveCards.CreateWorklogAdaptiveCard();
            var reply = MessageFactory.Attachment(cardAttachment);
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
