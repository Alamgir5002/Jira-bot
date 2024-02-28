﻿using System;

namespace Jira_bot.Models
{
    public class JiraWorkLog
    {
        public string baseUrl { get; set; }
        public string email { get; set; }
        public string token { get; set; }
        public string issueId { get; set; }
        public WorklogDetailsBody body {set; get; }
    }
    public class WorklogDetailsBody
    {
        public string started {get; set; }
        public int timeSpentSeconds { get; set;}
    }
}