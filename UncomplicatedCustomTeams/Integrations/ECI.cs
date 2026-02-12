using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Integrations
{
    internal static class ECI
    {
        private static Type _customItemType;
        private static MethodInfo _getMethod;
        private static PropertyInfo _trackedSerialsProp;
        private static bool _reflectionFailed;
        private static bool _initialized;

        private static void InitializeReflection()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(x => x.GetName().Name.Contains("Exiled.CustomItems"));

                if (assembly == null)
                {
                    LogManager.Warn("[ECI] Integration failed: Exiled.CustomItems assembly not found.");
                    _reflectionFailed = true;
                    return;
                }

                _customItemType = assembly.GetType("Exiled.CustomItems.API.Features.CustomItem");
                if (_customItemType == null)
                {
                    LogManager.Warn("[ECI] Integration failed: CustomItem type not found.");
                    _reflectionFailed = true;
                    return;
                }

                _getMethod = _customItemType.GetMethod("Get", [typeof(uint)]);

                _trackedSerialsProp = _customItemType.GetProperty("TrackedSerials");

                if (_getMethod == null || _trackedSerialsProp == null)
                {
                    LogManager.Warn("[ECI] Integration failed: API methods/properties missing.");
                    _reflectionFailed = true;
                    return;
                }

                LogManager.Debug("[ECI] Exiled.CustomItems integration initialized successfully.");
            }
            catch (Exception e)
            {
                LogManager.Error($"[ECI] Critical error during initialization: {e}");
                _reflectionFailed = true;
            }
        }

        public static bool IsCustomItem(ushort itemSerial, int id)
        {
            InitializeReflection();

            if (_reflectionFailed) return false;

            try
            {
                object customItemInstance = _getMethod.Invoke(null, [(uint)id]);

                if (customItemInstance == null) return false;

                IEnumerable serials = _trackedSerialsProp.GetValue(customItemInstance) as IEnumerable;
                if (serials == null) return false;

                foreach (var s in serials)
                {
                    if (s is int serialInt && serialInt == (int)itemSerial)
                        return true;
                }

                return false;
            }
            catch (Exception e)
            {
                LogManager.Error($"[ECI] Error checking item ID {id}: {e}");
                return false;
            }
        }
    }
}