using System;
using MelonLoader;

namespace Mimesis_Mod_Menu.Core.Features
{
    public abstract class FeatureManager
    {
        public virtual bool IsEnabled { get; protected set; }

        public virtual void SetEnabled(bool value)
        {
            IsEnabled = value;
        }

        public virtual void Update() { }

        public virtual void Cleanup() { }

        protected void LogError(string methodName, Exception ex)
        {
            MelonLogger.Error($"{GetType().Name}.{methodName} error: {ex.Message}");
        }

        protected void LogMessage(string message)
        {
            MelonLogger.Msg($"[{GetType().Name}] {message}");
        }
    }
}
