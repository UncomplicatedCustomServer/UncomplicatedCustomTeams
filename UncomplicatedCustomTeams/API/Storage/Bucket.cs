using Exiled.API.Features;
using PlayerRoles;
using System.Collections.Generic;
using UncomplicatedCustomTeams.API.Features;

namespace UncomplicatedCustomTeams.API.Storage
{
    internal class Bucket
    {
        public static List<int> SpawnBucket { get; set; } = new();

        public static SummonedTeam Team { get; set; }
    }
}
