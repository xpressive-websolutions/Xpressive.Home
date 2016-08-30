using System;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Xpressive.Home.WebApi.Controllers
{
    [HubName("loggingHub")]
    public class LoggingHub : Hub
    {
        public LoggingHub()
        {
            SignalrAppender.LocalInstance.MessageLogged = OnMessageLogged;
        }

        public void OnMessageLogged(LogEntry e)
        {
            Clients.All.onLoggedEvent(e.FormattedEvent, e.LoggingEvent);
        }
    }

    public class SignalrAppender : AppenderSkeleton
    {
        public Action<LogEntry> MessageLogged;

        public SignalrAppender()
        {
            System.Diagnostics.Debug.WriteLine("Instantiating");
            LocalInstance = this;
        }

        public static SignalrAppender LocalInstance { get; private set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var formattedEvent = RenderLoggingEvent(loggingEvent);

            var logEntry = new LogEntry(formattedEvent, new JsonLoggingEventData(loggingEvent));

            MessageLogged?.Invoke(logEntry);
        }
    }


    public class LogEntry
    {
        public LogEntry(string formttedEvent, JsonLoggingEventData loggingEvent)
        {
            FormattedEvent = formttedEvent;
            LoggingEvent = loggingEvent;
        }

        public string FormattedEvent { get; set; }
        public JsonLoggingEventData LoggingEvent { get; set; }
    }

    public class JsonLoggingEventData
    {
        private const FixFlags Flags = FixFlags.Exception | FixFlags.UserName | FixFlags.Partial;

        public JsonLoggingEventData()
        {
        }

        public JsonLoggingEventData(LoggingEvent loggingEvent)
        {
            var loggingEventData = loggingEvent.GetLoggingEventData(Flags);
            Domain = loggingEventData.Domain;
            ExceptionString = loggingEventData.ExceptionString;
            Level = loggingEventData.Level.DisplayName;
            LoggerName = loggingEventData.LoggerName;
            Message = loggingEventData.Message;
            Properties = loggingEventData.Properties;
            ThreadName = loggingEventData.ThreadName;
            TimeStamp = loggingEventData.TimeStamp.ToString("u");
        }

        public string Domain { get; set; }

        public string ExceptionString { get; set; }

        public string Level { get; set; }

        public string LoggerName { get; set; }

        public string Message { get; set; }

        public PropertiesDictionary Properties { get; set; }

        public string ThreadName { get; set; }

        public string TimeStamp { get; set; }
    }
}
