using CommandSystem;
using LabApi.Features.Wrappers;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.EventHandlers.SpawnWaves;
using UncomplicatedCustomTeams.Interfaces;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    internal class ForceNextWave : IUCTCommand
    {
        public string Name { get; } = "fnw";
        public string Description { get; } = "Forces the next wave to be a custom team.";
        public string RequiredPermission { get; } = "uct.fnw";
        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsRoundStarted)
            {
                response = "Cannot force a wave when the round hasn't started!";
                return false;
            }

            if (arguments.Count == 0)
            {
                DefaultSpawnWaves.ForceAnyCustomTeam = true;
                DefaultSpawnWaves.ForcedNextWave = false;
                DefaultSpawnWaves.NextWaveDefinition = null;

                response = "Next wave will perform a guaranteed spawn of a RANDOM Custom Team (matching the Spawn Wave type).";
                return true;
            }

            if (arguments.Count < 2)
            {
                response = "Usage: uct fnw <TeamId> <ForceSpawn (true/false)>\nExample: uct fnw 1 true";
                return false;
            }

            if (!uint.TryParse(arguments.ElementAt(0), out var id))
            {
                response = "Invalid team ID!";
                return false;
            }

            bool ignoreChance = true;
            if (arguments.Count >= 2) bool.TryParse(arguments.ElementAt(1), out ignoreChance);

            var team = Team.List.FirstOrDefault(t => t.Id == id);

            if (team is null)
            {
                response = $"Team with ID {id} does not exist.";
                return false;
            }

            if (team.SpawnConditions.SpawnWave != WaveType.NtfWave && team.SpawnConditions.SpawnWave != WaveType.ChaosWave)
            {
                response = $"This team cannot be forced (SpawnWave must be NtfWave or ChaosWave).";
                return false;
            }

            DefaultSpawnWaves.ForcedNextWave = true;
            DefaultSpawnWaves.NextWaveDefinition = team;
            DefaultSpawnWaves.ForceAnyCustomTeam = false;

            response = $"Next wave explicitly set to team '{team.Name}'.";
            return true;
        }
    }
}