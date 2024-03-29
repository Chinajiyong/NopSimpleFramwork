﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Users;
using Nop.Core.Plugins;
using Nop.Services.Events;

namespace Nop.Services.Plugins
{
    /// <summary>
    /// Plugin finder
    /// </summary>
    public class PluginFinder : IPluginFinder
    {
        #region Fields

        private readonly IEventPublisher _eventPublisher;

        private IList<PluginDescriptor> _plugins;
        private bool _arePluginsLoaded;

        #endregion

        #region Ctor

        public PluginFinder(IEventPublisher eventPublisher)
        {
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Ensure plugins are loaded
        /// </summary>
        protected virtual void EnsurePluginsAreLoaded()
        {
            if (_arePluginsLoaded) 
                return;

            var foundPlugins = PluginManager.ReferencedPlugins.ToList();
            foundPlugins.Sort();
            _plugins = foundPlugins.ToList();

            _arePluginsLoaded = true;
        }

        /// <summary>
        /// Check whether the plugin is available
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <returns>true - available; false - no</returns>
        protected virtual bool CheckLoadMode(PluginDescriptor pluginDescriptor, LoadPluginsMode loadMode)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            switch (loadMode)
            {
                case LoadPluginsMode.All:
                    //no filtering
                    return true;
                case LoadPluginsMode.InstalledOnly:
                    return pluginDescriptor.Installed;
                case LoadPluginsMode.NotInstalledOnly:
                    return !pluginDescriptor.Installed;
                default:
                    throw new Exception("Not supported LoadPluginsMode");
            }
        }

        /// <summary>
        /// Check whether the plugin is in a certain group
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="group">Group</param>
        /// <returns>true - available; false - no</returns>
        protected virtual bool CheckGroup(PluginDescriptor pluginDescriptor, string group)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (string.IsNullOrEmpty(group))
                return true;

            return group.Equals(pluginDescriptor.Group, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion
        
        #region Methods

        /// <summary>
        /// Check whether the plugin is available
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <returns>true - available; false - no</returns>
        public virtual bool Authenticate(PluginDescriptor pluginDescriptor)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            return (true);
        }

        /// <summary>
        /// Check that plugin is available for the specified user
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="user">User</param>
        /// <returns>True if authorized; otherwise, false</returns>
        public virtual bool AuthorizedForUser(PluginDescriptor pluginDescriptor, User user)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (user == null || !pluginDescriptor.LimitedToUserRoles.Any())
                return true;

            var userRoleIds = user.UserRoles.Where(role => role.Active).Select(role => role.Id);

            return pluginDescriptor.LimitedToUserRoles.Intersect(userRoleIds).Any();
        }

        /// <summary>
        /// Gets plugin groups
        /// </summary>
        /// <returns>Plugins groups</returns>
        public virtual IEnumerable<string> GetPluginGroups()
        {
            return GetPluginDescriptors(LoadPluginsMode.All).Select(x => x.Group).Distinct().OrderBy(x => x);
        }

        /// <summary>
        /// Gets plugins
        /// </summary>
        /// <typeparam name="T">The type of plugins to get.</typeparam>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="user">Load records allowed only to a specified user; pass null to ignore ACL permissions</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <returns>Plugins</returns>
        public virtual IEnumerable<T> GetPlugins<T>(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            User user = null, string group = null) where T : class, IPlugin
        {
            return GetPluginDescriptors<T>(loadMode, user, group).Select(p => p.Instance<T>());
        }

        /// <summary>
        /// Get plugin descriptors
        /// </summary>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="user">Load records allowed only to a specified user; pass null to ignore ACL permissions</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <returns>Plugin descriptors</returns>
        public virtual IEnumerable<PluginDescriptor> GetPluginDescriptors(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            User user = null, string group = null)
        {
            //ensure plugins are loaded
            EnsurePluginsAreLoaded();

            return _plugins.Where(p => CheckLoadMode(p, loadMode) && AuthorizedForUser(p, user) && Authenticate(p) && CheckGroup(p, group));
        }

        /// <summary>
        /// Get plugin descriptors
        /// </summary>
        /// <typeparam name="T">The type of plugin to get.</typeparam>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="user">Load records allowed only to a specified user; pass null to ignore ACL permissions</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <returns>Plugin descriptors</returns>
        public virtual IEnumerable<PluginDescriptor> GetPluginDescriptors<T>(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            User user = null, string group = null) 
            where T : class, IPlugin
        {
            return GetPluginDescriptors(loadMode, user, group)
                .Where(p => typeof(T).IsAssignableFrom(p.PluginType));
        }

        /// <summary>
        /// Get a plugin descriptor by its system name
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <returns>>Plugin descriptor</returns>
        public virtual PluginDescriptor GetPluginDescriptorBySystemName(string systemName, LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly)
        {
            return GetPluginDescriptors(loadMode)
                .SingleOrDefault(p => p.SystemName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Get a plugin descriptor by its system name
        /// </summary>
        /// <typeparam name="T">The type of plugin to get.</typeparam>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <returns>>Plugin descriptor</returns>
        public virtual PluginDescriptor GetPluginDescriptorBySystemName<T>(string systemName, LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly)
            where T : class, IPlugin
        {
            return GetPluginDescriptors<T>(loadMode)
                .SingleOrDefault(p => p.SystemName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Reload plugins after updating
        /// </summary>
        /// <param name="pluginDescriptor">Updated plugin descriptor</param>
        public virtual void ReloadPlugins(PluginDescriptor pluginDescriptor)
        {
            _arePluginsLoaded = false;
            EnsurePluginsAreLoaded();

            //raise event
            _eventPublisher.Publish(new PluginUpdatedEvent(pluginDescriptor));
        }

        #endregion
    }
}
