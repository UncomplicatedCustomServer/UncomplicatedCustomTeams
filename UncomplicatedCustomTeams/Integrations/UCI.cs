using LabApi.Features.Wrappers;
using System;
using System.Linq;
using System.Reflection;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Integrations
{
    internal static class UCI
    {
        private static Type _summonedType;
        private static MethodInfo _getMethod;
        private static PropertyInfo _customItemProp;
        private static PropertyInfo _idProp;

        private static bool _initialized;
        private static bool _reflectionFailed;

        private static void InitializeReflection()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(x => x.GetName().Name.Contains("UncomplicatedCustomItems"));

                if (assembly == null)
                {
                    LogManager.Warn("[UCI] Integration failed: UncomplicatedCustomItems assembly not found.");
                    _reflectionFailed = true;
                    return;
                }

                _summonedType = assembly.GetType("UncomplicatedCustomItems.API.Features.SummonedCustomItem");

                if (_summonedType == null)
                {
                    LogManager.Warn("[UCI] Integration failed: 'UncomplicatedCustomItems.API.Features.SummonedCustomItem' type not found.");
                    _reflectionFailed = true;
                    return;
                }

                _getMethod = _summonedType.GetMethod("Get", [typeof(ushort)]);

                _customItemProp = _summonedType.GetProperty("CustomItem");

                if (_getMethod == null || _customItemProp == null)
                {
                    LogManager.Warn($"[UCI] Integration failed: Missing members. Get: {_getMethod != null}, CustomItem: {_customItemProp != null}");
                    _reflectionFailed = true;
                    return;
                }

                var iCustomItemType = _customItemProp.PropertyType;
                _idProp = iCustomItemType.GetProperty("Id");

                if (_idProp == null)
                {
                    LogManager.Warn("[UCI] Integration failed: 'Id' property missing on ICustomItem type.");
                    _reflectionFailed = true;
                    return;
                }

                LogManager.Debug("[UCI] UncomplicatedCustomItems integration initialized successfully.");
            }
            catch (Exception e)
            {
                LogManager.Error($"[UCI] Critical error during initialization: {e}");
                _reflectionFailed = true;
            }
        }

        public static bool IsCustomItem(Item item, int requiredId)
        {
            if (item == null) return false;

            InitializeReflection();

            if (_reflectionFailed) return false;

            try
            {
                object summonedItemInstance = _getMethod.Invoke(null, [item.Serial]);

                if (summonedItemInstance != null)
                {
                    object customItemDefinition = _customItemProp.GetValue(summonedItemInstance);
                    if (customItemDefinition == null) return false;

                    uint id = (uint)_idProp.GetValue(customItemDefinition);

                    return id == (uint)requiredId;
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"[UCI] Error checking item serial {item.Serial}: {ex}");
            }

            return false;
        }
    }
}