using CommandSystem;
using Exiled.API.Features;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.EventHandlers.SpawnWaves;
using UncomplicatedCustomTeams.Interfaces;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class ForceNextWave : IUCTCommand
    {
        public string Name { get; } = "fnw";

        public string Description { get; } = "Forces the next wave to be a custom team. Usage: 'uct fnw' (random) or 'uct fnw <ID> <ForceSpawn>' (specific).";

        public string RequiredPermission { get; } = "uct.fnw";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsStarted)
            {
                response = "Cannot force a wave when the round hasn't started!";
                return false;
            }

            if (arguments.Count == 0)
            {
                DefaultSpawnWaves.ForceAnyCustomTeam = true;
                DefaultSpawnWaves.ForcedNextWave = false;
                Plugin.NextTeam = null;

                response = "Next wave will perform a guaranteed spawn of a RANDOM Custom Team (matching the Spawn Wave type).";
                return true;
            }

            if (arguments.Count < 2)
            {
                response = "Usage: uct fnw <TeamId> <ForceSpawn (true/false)>\nExample: uct fnw 1 true";
                return false;
            }

            if (!uint.TryParse(arguments[0], out var id))
            {
                response = "Invalid team ID!";
                return false;
            }

            if (!bool.TryParse(arguments[1], out bool ignoreChance))
            {
                response = "Invalid boolean for ForceSpawn! Use 'true' (force 100%) or 'false' (respect spawn chance).";
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
            DefaultSpawnWaves.ForcedNextWave = true;
            DefaultSpawnWaves.IgnoreSpawnChance = ignoreChance;
            DefaultSpawnWaves.ForceAnyCustomTeam = false;

            string chanceMsg = ignoreChance ? "ignoring spawn chance (100% spawn)" : $"respecting spawn chance ({team.SpawnChance}%)";
            response = $"Next wave explicitly set to team '{team.Name}'. {chanceMsg}.";
            return true;
        }
    }
}