﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Users;
using Nop.Core.Plugins;
using Nop.Services.Authentication.External;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Themes;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Plugins;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class PluginController : BaseAdminController
    {
        #region Fields

        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly IUserActivityService _userActivityService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IPluginFinder _pluginFinder;
        private readonly IPluginModelFactory _pluginModelFactory;
        private readonly ISettingService _settingService;
        private readonly IUploadService _uploadService;
        private readonly IWebHelper _webHelper;
        private readonly IWidgetService _widgetService;
        private readonly WidgetSettings _widgetSettings;

        #endregion

        #region Ctor

        public PluginController(ExternalAuthenticationSettings externalAuthenticationSettings,
            IUserActivityService userActivityService,
            IEventPublisher eventPublisher,
            IExternalAuthenticationService externalAuthenticationService,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            IPluginFinder pluginFinder,
            IPluginModelFactory pluginModelFactory,
            ISettingService settingService,
            IUploadService uploadService,
            IWebHelper webHelper,
            IWidgetService widgetService,
            WidgetSettings widgetSettings)
        {
            this._externalAuthenticationSettings = externalAuthenticationSettings;
            this._userActivityService = userActivityService;
            this._eventPublisher = eventPublisher;
            this._externalAuthenticationService = externalAuthenticationService;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._pluginFinder = pluginFinder;
            this._pluginModelFactory = pluginModelFactory;
            this._settingService = settingService;
            this._uploadService = uploadService;
            this._webHelper = webHelper;
            this._widgetService = widgetService;
            this._widgetSettings = widgetSettings;
        }

        #endregion

        #region Methods

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual IActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            var model = _pluginModelFactory.PreparePluginsConfigurationModel(new PluginsConfigurationModel());

            return View(model);
        }

        [HttpPost]
        public virtual IActionResult ListSelect(PluginSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedKendoGridJson();

            //prepare model
            var model = _pluginModelFactory.PreparePluginListModel(searchModel);

            return Json(model);
        }

        public virtual IActionResult SearchList()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return Json(new System.Collections.Generic.List<string>());

            //prepare model
            var model = _pluginModelFactory.PreparePluginListModel(
                new PluginSearchModel { PageSize = int.MaxValue });

            //negative rate is set to move plugins to the end of list
            var filtredPlugins = model.Data
                .Where(m => !string.IsNullOrEmpty(m.ConfigurationUrl))
                .Select(m => new
                {
                    title = m.FriendlyName,
                    link = m.ConfigurationUrl,
                    parent = "Plugins",
                    grandParent = "",
                    rate = -50
                })
                .ToList();

            return Json(filtredPlugins);
        }

        [HttpPost]
        public virtual IActionResult UploadPluginsAndThemes(IFormFile archivefile)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            try
            {
                if (archivefile == null || archivefile.Length == 0)
                {
                    ErrorNotification(_localizationService.GetResource("Admin.Common.UploadFile"));
                    return RedirectToAction("List");
                }

                var descriptors = _uploadService.UploadPluginsAndThemes(archivefile);
                var pluginDescriptors = descriptors.OfType<PluginDescriptor>().ToList();
                var themeDescriptors = descriptors.OfType<ThemeDescriptor>().ToList();

                //activity log
                foreach (var descriptor in pluginDescriptors)
                {
                    _userActivityService.InsertActivity("UploadNewPlugin",
                        string.Format(_localizationService.GetResource("ActivityLog.UploadNewPlugin"), descriptor.FriendlyName));
                }

                foreach (var descriptor in themeDescriptors)
                {
                    _userActivityService.InsertActivity("UploadNewTheme",
                        string.Format(_localizationService.GetResource("ActivityLog.UploadNewTheme"), descriptor.FriendlyName));
                }

                //events
                if (pluginDescriptors.Any())
                    _eventPublisher.Publish(new PluginsUploadedEvent(pluginDescriptors));

                if (themeDescriptors.Any())
                    _eventPublisher.Publish(new ThemesUploadedEvent(themeDescriptors));

                var message = string.Format(_localizationService.GetResource("Admin.Configuration.Plugins.Uploaded"), pluginDescriptors.Count, themeDescriptors.Count);
                SuccessNotification(message);

                //restart application
                _webHelper.RestartAppDomain();
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
            }

            return RedirectToAction("List");
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired(FormValueRequirement.StartsWith, "install-plugin-link-")]
        public virtual IActionResult Install(IFormCollection form)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            try
            {
                //get plugin system name
                string systemName = null;
                foreach (var formValue in form.Keys)
                    if (formValue.StartsWith("install-plugin-link-", StringComparison.InvariantCultureIgnoreCase))
                        systemName = formValue.Substring("install-plugin-link-".Length);

                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName(systemName, LoadPluginsMode.All);
                if (pluginDescriptor == null)
                    //No plugin found with the specified id
                    return RedirectToAction("List");

                //check whether plugin is not installed
                if (pluginDescriptor.Installed)
                    return RedirectToAction("List");

                //install plugin
                pluginDescriptor.Instance().Install();

                //activity log
                _userActivityService.InsertActivity("InstallNewPlugin",
                    string.Format(_localizationService.GetResource("ActivityLog.InstallNewPlugin"), pluginDescriptor.FriendlyName));

                SuccessNotification(_localizationService.GetResource("Admin.Configuration.Plugins.Installed"));

                //restart application
                _webHelper.RestartAppDomain();
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
            }

            return RedirectToAction("List");
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired(FormValueRequirement.StartsWith, "uninstall-plugin-link-")]
        public virtual IActionResult Uninstall(IFormCollection form)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            try
            {
                //get plugin system name
                string systemName = null;
                foreach (var formValue in form.Keys)
                    if (formValue.StartsWith("uninstall-plugin-link-", StringComparison.InvariantCultureIgnoreCase))
                        systemName = formValue.Substring("uninstall-plugin-link-".Length);

                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName(systemName, LoadPluginsMode.All);
                if (pluginDescriptor == null)
                    //No plugin found with the specified id
                    return RedirectToAction("List");

                //check whether plugin is installed
                if (!pluginDescriptor.Installed)
                    return RedirectToAction("List");

                //uninstall plugin
                pluginDescriptor.Instance().Uninstall();

                //activity log
                _userActivityService.InsertActivity("UninstallPlugin",
                    string.Format(_localizationService.GetResource("ActivityLog.UninstallPlugin"), pluginDescriptor.FriendlyName));

                SuccessNotification(_localizationService.GetResource("Admin.Configuration.Plugins.Uninstalled"));

                //restart application
                _webHelper.RestartAppDomain();
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
            }

            return RedirectToAction("List");
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired(FormValueRequirement.StartsWith, "delete-plugin-link-")]
        public virtual IActionResult Delete(IFormCollection form)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            try
            {
                //get plugin system name
                string systemName = null;
                foreach (var formValue in form.Keys)
                    if (formValue.StartsWith("delete-plugin-link-", StringComparison.InvariantCultureIgnoreCase))
                        systemName = formValue.Substring("delete-plugin-link-".Length);

                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName(systemName, LoadPluginsMode.All);
                if (!PluginManager.DeletePlugin(pluginDescriptor))
                    return RedirectToAction("List");

                //activity log
                _userActivityService.InsertActivity("DeletePlugin",
                    string.Format(_localizationService.GetResource("ActivityLog.DeletePlugin"), pluginDescriptor.FriendlyName));

                SuccessNotification(_localizationService.GetResource("Admin.Configuration.Plugins.Deleted"));

                //restart application
                _webHelper.RestartAppDomain();
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
            }

            return RedirectToAction("List");
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("plugin-reload-grid")]
        public virtual IActionResult ReloadList()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //restart application
            _webHelper.RestartAppDomain();

            return RedirectToAction("List");
        }

        public virtual IActionResult EditPopup(string systemName)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //try to get a plugin with the specified system name
            var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName(systemName, LoadPluginsMode.All);
            if (pluginDescriptor == null)
                return RedirectToAction("List");

            //prepare model
            var model = _pluginModelFactory.PreparePluginModel(null, pluginDescriptor);

            return View(model);
        }

        [HttpPost]
        public virtual IActionResult EditPopup(PluginModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //try to get a plugin with the specified system name
            var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName(model.SystemName, LoadPluginsMode.All);
            if (pluginDescriptor == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                //we allow editing of 'friendly name', 'display order'
                pluginDescriptor.FriendlyName = model.FriendlyName;
                pluginDescriptor.DisplayOrder = model.DisplayOrder;
                pluginDescriptor.LimitedToUserRoles.Clear();
                if (model.SelectedUserRoleIds.Any())
                    pluginDescriptor.LimitedToUserRoles = model.SelectedUserRoleIds;

                //update the description file
                PluginManager.SavePluginDescriptor(pluginDescriptor);

                //reset plugin cache
                _pluginFinder.ReloadPlugins(pluginDescriptor);

                //locales
                foreach (var localized in model.Locales)
                {
                    _localizationService.SaveLocalizedFriendlyName(pluginDescriptor.Instance(), localized.LanguageId, localized.FriendlyName);
                }

                //enabled/disabled
                if (pluginDescriptor.Installed)
                {
                    var pluginInstance = pluginDescriptor.Instance();
                    switch (pluginInstance)
                    {
                        case IExternalAuthenticationMethod externalAuthenticationMethod:
                            if (_externalAuthenticationService.IsExternalAuthenticationMethodActive(externalAuthenticationMethod) && !model.IsEnabled)
                            {
                                //mark as disabled
                                _externalAuthenticationSettings.ActiveAuthenticationMethodSystemNames.Remove(pluginDescriptor.SystemName);
                                _settingService.SaveSetting(_externalAuthenticationSettings);
                                break;
                            }

                            if (!_externalAuthenticationService.IsExternalAuthenticationMethodActive(externalAuthenticationMethod) && model.IsEnabled)
                            {
                                //mark as enabled
                                _externalAuthenticationSettings.ActiveAuthenticationMethodSystemNames.Add(pluginDescriptor.SystemName);
                                _settingService.SaveSetting(_externalAuthenticationSettings);
                            }

                            break;
                        case IWidgetPlugin widgetPlugin:
                            if (_widgetService.IsWidgetActive(widgetPlugin) && !model.IsEnabled)
                            {
                                //mark as disabled
                                _widgetSettings.ActiveWidgetSystemNames.Remove(pluginDescriptor.SystemName);
                                _settingService.SaveSetting(_widgetSettings);
                                break;
                            }

                            if (!_widgetService.IsWidgetActive(widgetPlugin) && model.IsEnabled)
                            {
                                //mark as enabled
                                _widgetSettings.ActiveWidgetSystemNames.Add(pluginDescriptor.SystemName);
                                _settingService.SaveSetting(_widgetSettings);
                            }

                            break;
                    }

                    //activity log
                    _userActivityService.InsertActivity("EditPlugin",
                        string.Format(_localizationService.GetResource("ActivityLog.EditPlugin"), pluginDescriptor.FriendlyName));
                }

                ViewBag.RefreshPage = true;

                return View(model);
            }

            //prepare model
            model = _pluginModelFactory.PreparePluginModel(model, pluginDescriptor, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        public virtual IActionResult OfficialFeed()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //prepare model
            var model = _pluginModelFactory.PrepareOfficialFeedPluginSearchModel(new OfficialFeedPluginSearchModel());

            return View(model);
        }
        #endregion
    }
}