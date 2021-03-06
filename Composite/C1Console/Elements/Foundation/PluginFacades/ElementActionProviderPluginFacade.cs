﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using Composite.Core.Collections.Generic;
using Composite.C1Console.Elements.Plugins.ElementActionProvider;
using Composite.C1Console.Elements.Plugins.ElementActionProvider.Runtime;
using Composite.C1Console.Events;
using Composite.C1Console.Security;


namespace Composite.C1Console.Elements.Foundation.PluginFacades
{
    internal static class ElementActionProviderPluginFacade
    {
        private static ResourceLocker<Resources> _resourceLocker = new ResourceLocker<Resources>(new Resources(), Resources.Initialize);


        static ElementActionProviderPluginFacade()
        {
            GlobalEventSystemFacade.SubscribeToFlushEvent(OnFlushEvent);
        }



        public static IEnumerable<ElementAction> GetActions(string providerName, EntityToken entityToken)
        {
            Verify.ArgumentNotNullOrEmpty(providerName, "providerName");

            IElementActionProvider elementActionProvider = GetElementActionProvider(providerName);

            return elementActionProvider.GetActions(entityToken);
        }


        private static IElementActionProvider GetElementActionProvider(string providerName)
        {
            IElementActionProvider provider;

            if (_resourceLocker.Resources.ProviderCache.TryGetValue(providerName, out provider) == false)
            {
                try
                {
                    provider = _resourceLocker.Resources.Factory.Create(providerName);

                    using (_resourceLocker.Locker)
                    {
                        if (_resourceLocker.Resources.ProviderCache.ContainsKey(providerName) == false)
                        {
                            _resourceLocker.Resources.ProviderCache.Add(providerName, provider);
                        }
                    }

                }
                catch (ArgumentException ex)
                {
                    HandleConfigurationError(ex);
                }
                catch (ConfigurationErrorsException ex)
                {
                    HandleConfigurationError(ex);
                }
                catch (TargetInvocationException ex)
                {
                    HandleConfigurationError(ex);
                }
            }

            return provider;
        }



        private static void Flush()
        {
            _resourceLocker.ResetInitialization();
        }



        private static void OnFlushEvent(FlushEventArgs args)
        {
            Flush();
        }



        private static void HandleConfigurationError(Exception ex)
        {
            Flush();

            throw new ConfigurationErrorsException(string.Format("Failed to load the configuration section '{0}' from the configuration.", ElementActionProviderSettings.SectionName), ex);
        }



        private sealed class Resources
        {
            public ElementActionProviderFactory Factory { get; set; }
            public Dictionary<string, IElementActionProvider> ProviderCache { get; set; }

            public static void Initialize(Resources resources)
            {
                try
                {
                    resources.Factory = new ElementActionProviderFactory();
                }
                catch (NullReferenceException ex)
                {
                    HandleConfigurationError(ex);
                }
                catch (ConfigurationErrorsException ex)
                {
                    HandleConfigurationError(ex);
                }
                catch (TargetInvocationException ex)
                {
                    HandleConfigurationError(ex);
                }

                resources.ProviderCache = new Dictionary<string, IElementActionProvider>();
            }
        }
    }
}
