using System.Collections.Generic;
using UncomplicatedCustomTeams.API.Features;

namespace UncomplicatedCustomTeams.API.Storage
{
    internal class Bucket
    {
        public static List<int> SpawnBucket { get; set; } = [];

        public static SummonedTeam Team { get; set; }
    }
}
