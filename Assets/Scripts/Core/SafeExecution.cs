using UnityEngine;
using System;

namespace Shredsquatch.Core
{
    /// <summary>
    /// Static utility class for safe execution of code that might throw exceptions.
    /// Use this to wrap potentially dangerous operations.
    /// </summary>
    public static class SafeExecution
    {
        /// <summary>
        /// Execute an action safely, catching any exceptions.
        /// </summary>
        public static bool Try(Action action, string context = null)
        {
            if (action == null) return false;

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, context);
                NotifyErrorManager(ex, context);
                return false;
            }
        }

        /// <summary>
        /// Execute a function safely, returning a fallback value on failure.
        /// </summary>
        public static T Try<T>(Func<T> func, T fallback = default, string context = null)
        {
            if (func == null) return fallback;

            try
            {
                return func();
            }
            catch (Exception ex)
            {
                LogError(ex, context);
                NotifyErrorManager(ex, context);
                return fallback;
            }
        }

        /// <summary>
        /// Execute an action that takes a parameter safely.
        /// </summary>
        public static bool Try<TArg>(Action<TArg> action, TArg arg, string context = null)
        {
            if (action == null) return false;

            try
            {
                action(arg);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, context);
                NotifyErrorManager(ex, context);
                return false;
            }
        }

        /// <summary>
        /// Execute an action in Update loop safely (silently fails to avoid spam).
        /// </summary>
        public static void TryUpdate(Action action, ref bool hasErrored, string context = null)
        {
            if (action == null || hasErrored) return;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                hasErrored = true;
                LogError(ex, context);
                NotifyErrorManager(ex, context);
            }
        }

        /// <summary>
        /// Safely invoke an event.
        /// </summary>
        public static void TryInvoke(Action eventAction, string context = null)
        {
            if (eventAction == null) return;

            foreach (var handler in eventAction.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception ex)
                {
                    LogError(ex, $"{context ?? "Event"} handler");
                }
            }
        }

        /// <summary>
        /// Safely invoke an event with one argument.
        /// </summary>
        public static void TryInvoke<T>(Action<T> eventAction, T arg, string context = null)
        {
            if (eventAction == null) return;

            foreach (var handler in eventAction.GetInvocationList())
            {
                try
                {
                    ((Action<T>)handler)(arg);
                }
                catch (Exception ex)
                {
                    LogError(ex, $"{context ?? "Event"} handler");
                }
            }
        }

        /// <summary>
        /// Safely get a component, returning null if not found or on error.
        /// </summary>
        public static T TryGetComponent<T>(GameObject obj) where T : class
        {
            if (obj == null) return null;

            try
            {
                return obj.GetComponent<T>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Safely get a component, returning null if not found or on error.
        /// </summary>
        public static T TryGetComponent<T>(Component component) where T : class
        {
            if (component == null) return null;

            try
            {
                return component.GetComponent<T>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void LogError(Exception ex, string context)
        {
            string prefix = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
            Debug.LogWarning($"[SafeExecution] {prefix}Caught exception: {ex.Message}");
        }

        private static void NotifyErrorManager(Exception ex, string context)
        {
            if (ErrorRecoveryManager.Instance != null)
            {
                ErrorRecoveryManager.Instance.HandleException(ex, context);
            }
        }
    }
}
