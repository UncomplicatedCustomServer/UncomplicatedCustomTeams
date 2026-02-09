using Discord;
using System;
using System.Text.Json.Serialization;

namespace UncomplicatedCustomTeams.API.Features
{
    [method: JsonConstructor]
    internal class LogEntry(long time, string level, string content, string error = null)
    {
        /// <summary>
        /// Gets the time in unix milliseconds of the message
        /// </summary>
        public long Time { get; } = time;

        /// <summary>
        /// Gets the <see cref="Discord.LogLevel"/> or a custom LogLevel of the message
        /// </summary>
        public string Level { get; } = level;

        /// <summary>
        /// Gets the message of the log
        /// </summary>
        public string Content { get; } = content;

#nullable enable
        /// <summary>
        /// Gets the custom error code of the message - can be null!
        /// </summary>
        public string? Error { get; } = error;
#nullable disable

        /// <summary>
        /// Gets the instance of the Error as string
        /// </summary>
        public string PublicError => Error is null ? string.Empty : $"{Error} ";

        [JsonIgnore]
        public DateTimeOffset DateTimeOffset => DateTimeOffset.FromUnixTimeMilliseconds(Time);

        public LogEntry(long time, LogLevel level, string content, string error = null) : this(time, level.ToString(), content, error) { }

        public override string ToString() => $"[{DateTimeOffset.Year}-{DateTimeOffset.Month}-{DateTimeOffset.Day} {DateTimeOffset.Hour}:{DateTimeOffset.Minute}:{DateTimeOffset.Second} {DateTimeOffset.Offset}]  [{Level}]  [UncomplicatedCustomTeams] {PublicError}{Content}";
    }
}
