using LabApi.Features.Wrappers;
using System;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Integrations
{
    internal static class UCI
    {
        private const string AssemblyName = "UncomplicatedCustomItems";
        private const string SummonedItemClass = "UncomplicatedCustomItems.API.Features.CustomItemAPI.SummonedAPICustomItem";
        private const string BaseItemClass = "UncomplicatedCustomItems.API.Features.CustomItemAPI.APICustomItem";
        public static bool IsCustomItem(Item item, int requiredId)
        {
            if (item == null) return false;

            try
            {
                var tryGet = DynamicInvoke.GetMethod(AssemblyName, SummonedItemClass, "TryGet");
                if (tryGet == null) return false;

                object[] parameters = [item.Serial, null];

                bool result = (bool)tryGet.Invoke(null, parameters);

                if (result)
                {
                    object summonedItem = parameters[1];
                    if (summonedItem == null) return false;

                    var customItemProp = DynamicInvoke.GetProperty(AssemblyName, SummonedItemClass, "CustomItem");
                    object apiCustomItem = customItemProp?.GetValue(summonedItem);

                    if (apiCustomItem != null)
                    {
                        var idProp = DynamicInvoke.GetProperty(AssemblyName, BaseItemClass, "Id");
                        if (idProp != null)
                        {
                            uint id = (uint)idProp.GetValue(apiCustomItem);

                            LogManager.Debug($"Item Serial {item.Serial} identified as CustomID: {id}. Required: {requiredId}");

                            return id == (uint)requiredId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{ex.Message}");
            }

            return false;
        }
    }
}