using System.Collections.Generic;

namespace UncomplicatedCustomTeams.Utilities.Errors
{
    public class YamlError
    {
        public string File { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
    }

    public static class ErrorManager
    {
        public static List<YamlError> Errors { get; } = [];

        private static readonly Dictionary<string, string> _suggestionMap = new()
        {
            { "mapping values are not allowed", "Make sure there is a space after the colon (e.g., `name: GOC` instead of `name:GOC`)." },
            { "expected 'mappingstart', got 'sequencestart'", "Your YAML file begins with a list (`- item`) but should begin with a mapping. Try adding a top-level key like `teams:` before your list." },
            { "while parsing a block mapping", "Check indentation and YAML structure — something might be misaligned or nested incorrectly." },
            { "expected <block end>, but found", "Possibly missing a `-` for a list item or the element ends prematurely." },
            { "did not find expected key", "A key may be missing or misaligned — ensure all keys are followed by colons and correctly indented." },
            { "unexpected end of stream", "The file might be cut off unexpectedly — check for missing closing brackets or incomplete blocks." },
            { "duplicate key", "You may have defined the same key twice in the same block — YAML requires keys to be unique." },
            { "found character that cannot start any token", "There's probably an illegal character or wrong symbol — double-check for stray tabs or weird characters." },
            { "found unexpected ':'", "There might be a colon `:` in a value that should be quoted — try wrapping the value in quotes." },
            { "anchor", "You're referencing an anchor (&value or *value) that hasn't been defined." },
            { "alias", "YAML alias (*) points to something that doesn't exist — check spelling or anchor placement." },
            { "cannot convert", "A value might be of the wrong type — make sure it's in the correct format (e.g., number vs string)." },
            { "sequence entries are not allowed here", "You're probably using a list (`- item`) in an invalid place — check indentation and nesting." },
            { "unexpected key", "This key may be misplaced or invalid — double-check your schema or property names." },
            { "unexpected property", "This property is not recognized — check your spelling against the documentation." }
        };

        public static void Add(string file, string message, int? line = null, int? column = null, string suggestion = "")
        {
            Errors.Add(new YamlError
            {
                File = file,
                Message = message,
                Line = line,
                Column = column,
                Suggestion = suggestion
            });
        }

        public static void Clear() => Errors.Clear();

        public static string GetSuggestionFromMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Check your YAML syntax near this location.";

            string lowerMsg = message.ToLowerInvariant();

            foreach (var kvp in _suggestionMap)
            {
                if (lowerMsg.Contains(kvp.Key))
                    return kvp.Value;
            }

            return "Check your YAML syntax near this location. Be sure indentation, colons, and types are correct.";
        }
    }
}