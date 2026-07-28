using LabApi.Features.Wrappers;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UncomplicatedCustomTeams.API.Extensions
{
    public static class CassieExtensions
    {

        private static readonly Dictionary<string, Func<string>> _cassieTags = new()
        {
            {"{SCPLEFT}", () => Player.ReadyList.Count(p => p.IsAlive && p.Role.GetTeam() == Team.SCPs).ToString() },

            {"{MTFLEFT}", () => Player.ReadyList.Count(p => p.IsAlive && p.Role.GetTeam() == Team.FoundationForces).ToString() },

            {"{CHAOSLEFT}", () => Player.ReadyList.Count(p => p.IsAlive && p.Role.GetTeam() == Team.ChaosInsurgency).ToString() },

            {"{CLASSDLEFT}", () => Player.ReadyList.Count(p => p.IsAlive && p.Role.GetTeam() == Team.ClassD).ToString() },

            {"{SCIENTISTLEFT}", () => Player.ReadyList.Count(p => p.IsAlive && p.Role.GetTeam() == Team.Scientists).ToString() },

            {"{ROUNDTIME}", () =>
                {
                    var elapsed = Round.Duration;
                    int minutes = (int)elapsed.TotalMinutes;
                    int seconds = elapsed.Seconds;

                    if (minutes > 0)
                    {
                        string minWord = minutes == 1 ? "minute" : "minutes";
                        string secWord = seconds == 1 ? "second" : "seconds";
                        return $"{minutes} {minWord} and {seconds} {secWord}";
                    }
                    else
                    {
                        string secWord = seconds == 1 ? "second" : "seconds";
                        return $"{seconds} {secWord}";
                    }
                }
            }
        };

        public static string ProcessCassieVariables(this string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            foreach (var tag in _cassieTags)
            {
                if (message.Contains(tag.Key))
                {
                    message = message.Replace(tag.Key, tag.Value.Invoke());
                }
            }

            return message;
        }
    }
}
