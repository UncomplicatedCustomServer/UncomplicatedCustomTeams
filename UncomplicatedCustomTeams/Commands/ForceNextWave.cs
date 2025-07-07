using CommandSystem;
using Exiled.API.Features;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Interfaces;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class ForceNextWave : IUCTCommand
    {
        public string Name { get; } = "fnw";

        public string Description { get; } = "Forces the next wave to be a custom team.";

        public string RequiredPermission { get; } = "uct.fnw";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsStarted)
            {
                response = "Cannot force a wave when the round hasn't started!";
                return false;
            }

            if (arguments.Count != 1)
            {
                response = "Usage: uct fnw <TeamId>";
                return false;
            }

            if (!uint.TryParse(arguments[0], out var id))
            {
                response = "Invalid team ID!";
                return false;
            }
            var team = Team.List.FirstOrDefault(t => t.Id == id);

            if (team is null)
            {
                response = $"Team with ID {id} does not exist.";
                return false;
            }

            if (team.SpawnConditions.SpawnWave != API.Enums.WaveType.NtfWave && team.SpawnConditions.SpawnWave != API.Enums.WaveType.ChaosWave)
            {
                response = $"This team cannot be forced (SpawnWave must be NtfWave or ChaosWave).";
                return false;
            }

            Plugin.NextTeam = new SummonedTeam(team);
            Plugin.Instance.Handler.ForcedNextWave = true;

            response = $"Next wave will spawn team '{team.Name}' successfully.";
            return true;
        }
    }
}