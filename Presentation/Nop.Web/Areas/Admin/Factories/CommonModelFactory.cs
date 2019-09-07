﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Net.Http.Headers;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Users;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;
using Nop.Services.Authentication.External;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Seo;
using Nop.Services.Users;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Areas.Admin.Models.Localization;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Security;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents common models factory implementation
    /// </summary>
    public partial class CommonModelFactory : ICommonModelFactory
    {
        #region Constants

        /// <summary>
        /// nopCommerce warning URL
        /// </summary>
        /// <remarks>
        /// {0} : store URL
        /// {1} : whether the store based is on the localhost
        /// </remarks>
        private const string NOPCOMMERCE_WARNING_URL = "https://www.nopcommerce.com/SiteWarnings.aspx?local={0}&url={1}";

        #endregion

        #region Fields

        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IUserService _userService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly IMaintenanceService _maintenanceService;
        private readonly INopFileProvider _fileProvider;
        private readonly IPluginFinder _pluginFinder;
        private readonly ISearchTermService _searchTermService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWebHelper _webHelper;
        private readonly IWidgetService _widgetService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public CommonModelFactory(AdminAreaSettings adminAreaSettings,
            IActionContextAccessor actionContextAccessor,
            IUserService userService,
            IDateTimeHelper dateTimeHelper,
            IExternalAuthenticationService externalAuthenticationService,
            INopFileProvider fileProvider,
            IHttpContextAccessor httpContextAccessor,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IMaintenanceService maintenanceService,
            IPluginFinder pluginFinder,
            ISearchTermService searchTermService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IWebHelper webHelper,
            IWidgetService widgetService,
            IWorkContext workContext)
        {
            this._adminAreaSettings = adminAreaSettings;
            this._actionContextAccessor = actionContextAccessor;
            this._userService = userService;
            this._dateTimeHelper = dateTimeHelper;
            this._externalAuthenticationService = externalAuthenticationService;
            this._fileProvider = fileProvider;
            this._httpContextAccessor = httpContextAccessor;
            this._languageService = languageService;
            this._localizationService = localizationService;
            this._maintenanceService = maintenanceService;
            this._pluginFinder = pluginFinder;
            this._searchTermService = searchTermService;
            this._urlHelperFactory = urlHelperFactory;
            this._urlRecordService = urlRecordService;
            this._webHelper = webHelper;
            this._widgetService = widgetService;
            this._workContext = workContext;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare copyright removal key warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        protected virtual void PrepareRemovalKeyWarningModel(IList<SystemWarningModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));
            
            var currentSiteUrl = string.Empty;
            var local = _webHelper.IsLocalRequest(_httpContextAccessor.HttpContext.Request);

            try
            {
                using (var client = new HttpClient())
                {
                    //specify request timeout
                    client.Timeout = TimeSpan.FromMilliseconds(2000);

                    var url = string.Format(NOPCOMMERCE_WARNING_URL, local, currentSiteUrl);
                    var warning = client.GetStringAsync(url).Result;

                    if (!String.IsNullOrEmpty(warning))
                        models.Add(new SystemWarningModel
                        {
                            Level = SystemWarningLevel.CopyrightRemovalKey,
                            Text = warning,
                            //this text could contain links. so don't encode it
                            DontEncode = true
                        });
                }
            }
            catch
            {
                //ignore exceptions
            }
        }

        /// <summary>
        /// Prepare plugins warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        protected virtual void PreparePluginsWarningModel(IList<SystemWarningModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            //check whether there are incompatible plugins
            if (!PluginManager.IncompatiblePlugins?.Any() ?? true)
                return;

            foreach (var pluginName in PluginManager.IncompatiblePlugins)
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(_localizationService.GetResource("Admin.System.Warnings.PluginNotLoaded"), pluginName)
                });
            }
        }

        /// <summary>
        /// Prepare performance settings warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        protected virtual void PreparePerformanceSettingsWarningModel(IList<SystemWarningModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));
        }

        /// <summary>
        /// Prepare file permissions warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        protected virtual void PrepareFilePermissionsWarningModel(IList<SystemWarningModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            var dirPermissionsOk = true;
            var dirsToCheck = FilePermissionHelper.GetDirectoriesWrite();
            foreach (var dir in dirsToCheck)
            {
                if (FilePermissionHelper.CheckPermissions(dir, false, true, true, false))
                    continue;

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(_localizationService.GetResource("Admin.System.Warnings.DirectoryPermission.Wrong"),
                        WindowsIdentity.GetCurrent().Name, dir)
                });
                dirPermissionsOk = false;
            }

            if (dirPermissionsOk)
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = _localizationService.GetResource("Admin.System.Warnings.DirectoryPermission.OK")
                });
            }

            var filePermissionsOk = true;
            var filesToCheck = FilePermissionHelper.GetFilesWrite();
            foreach (var file in filesToCheck)
            {
                if (FilePermissionHelper.CheckPermissions(file, false, true, true, true))
                    continue;

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(_localizationService.GetResource("Admin.System.Warnings.FilePermission.Wrong"),
                        WindowsIdentity.GetCurrent().Name, file)
                });
                filePermissionsOk = false;
            }

            if (filePermissionsOk)
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = _localizationService.GetResource("Admin.System.Warnings.FilePermission.OK")
                });
            }
        }

        /// <summary>
        /// Prepare backup file search model
        /// </summary>
        /// <param name="searchModel">Backup file search model</param>
        /// <returns>Backup file search model</returns>
        protected virtual BackupFileSearchModel PrepareBackupFileSearchModel(BackupFileSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare plugins enabled warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        protected virtual void PreparePluginsEnabledWarningModel(List<SystemWarningModel> models)
        {
            var pluginDescriptors = _pluginFinder.GetPluginDescriptors();

            var notEnabled = new List<string>();

            foreach (var plugin in pluginDescriptors.Select(pd => pd.Instance()))
            {
                var isEnabled = true;

                switch (plugin)
                {
                    case IExternalAuthenticationMethod externalAuthenticationMethod:
                        isEnabled = _externalAuthenticationService.IsExternalAuthenticationMethodActive(externalAuthenticationMethod);
                        break;

                    case IWidgetPlugin widgetPlugin:
                        isEnabled = _widgetService.IsWidgetActive(widgetPlugin);
                        break;
                }

                if (isEnabled)
                    continue;

                notEnabled.Add(plugin.PluginDescriptor.FriendlyName);
            }

            if (notEnabled.Any())
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = $"{_localizationService.GetResource("Admin.System.Warnings.PluginNotEnabled")}: {string.Join(", ", notEnabled)}"
                });
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare system info model
        /// </summary>
        /// <param name="model">System info model</param>
        /// <returns>System info model</returns>
        public virtual SystemInfoModel PrepareSystemInfoModel(SystemInfoModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.NopVersion = NopVersion.CurrentVersion;
            model.ServerTimeZone = TimeZoneInfo.Local.StandardName;
            model.ServerLocalTime = DateTime.Now;
            model.UtcTime = DateTime.UtcNow;
            model.CurrentUserTime = _dateTimeHelper.ConvertToUserTime(DateTime.Now);
            model.HttpHost = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Host];

            //ensure no exception is thrown
            try
            {
                model.OperatingSystem = Environment.OSVersion.VersionString;
                model.AspNetInfo = RuntimeEnvironment.GetSystemVersion();
                model.IsFullTrust = AppDomain.CurrentDomain.IsFullyTrusted.ToString();
            }
            catch { }

            foreach (var header in _httpContextAccessor.HttpContext.Request.Headers)
            {
                model.Headers.Add(new SystemInfoModel.HeaderModel
                {
                    Name = header.Key,
                    Value = header.Value
                });
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var loadedAssemblyModel = new SystemInfoModel.LoadedAssembly
                {
                    FullName = assembly.FullName
                };

                //ensure no exception is thrown
                try
                {
                    loadedAssemblyModel.Location = assembly.IsDynamic ? null : assembly.Location;
                    loadedAssemblyModel.IsDebug = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false)
                        .FirstOrDefault() is DebuggableAttribute attribute && attribute.IsJITOptimizerDisabled;

                    //https://stackoverflow.com/questions/2050396/getting-the-date-of-a-net-assembly
                    //we use a simple method because the more Jeff Atwood's solution doesn't work anymore 
                    //more info at https://blog.codinghorror.com/determining-build-date-the-hard-way/
                    loadedAssemblyModel.BuildDate = assembly.IsDynamic ? null : (DateTime?)TimeZoneInfo.ConvertTimeFromUtc(_fileProvider.GetLastWriteTimeUtc(assembly.Location), TimeZoneInfo.Local);

                }
                catch { }
                model.LoadedAssemblies.Add(loadedAssemblyModel);
            }

            return model;
        }

        /// <summary>
        /// Prepare system warning models
        /// </summary>
        /// <returns>List of system warning models</returns>
        public virtual IList<SystemWarningModel> PrepareSystemWarningModels()
        {
            var models = new List<SystemWarningModel>();

            //removal key
            PrepareRemovalKeyWarningModel(models);

            //incompatible plugins
            PreparePluginsWarningModel(models);

            //performance settings
            PreparePerformanceSettingsWarningModel(models);

            //validate write permissions (the same procedure like during installation)
            PrepareFilePermissionsWarningModel(models);

            //not active plugins
            PreparePluginsEnabledWarningModel(models);

            return models;
        }

        /// <summary>
        /// Prepare maintenance model
        /// </summary>
        /// <param name="model">Maintenance model</param>
        /// <returns>Maintenance model</returns>
        public virtual MaintenanceModel PrepareMaintenanceModel(MaintenanceModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.DeleteGuests.EndDate = DateTime.UtcNow.AddDays(-7);

            //prepare nested search model
            PrepareBackupFileSearchModel(model.BackupFileSearchModel);

            return model;
        }

        /// <summary>
        /// Prepare paged backup file list model
        /// </summary>
        /// <param name="searchModel">Backup file search model</param>
        /// <returns>Backup file list model</returns>
        public virtual BackupFileListModel PrepareBackupFileListModel(BackupFileSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get backup files
            var backupFiles = _maintenanceService.GetAllBackupFiles().ToList();

            //prepare list model
            var model = new BackupFileListModel
            {
                Data = backupFiles.PaginationByRequestModel(searchModel).Select(file =>
                {
                    //fill in model values from the entity
                    var backupFileModel = new BackupFileModel
                    {
                        Name = _fileProvider.GetFileName(file)
                    };

                    //fill in additional values (not existing in the entity)
                    backupFileModel.Length = $"{_fileProvider.FileLength(file) / 1024f / 1024f:F2} Mb";
                    backupFileModel.Link = $"{_webHelper.GetSiteLocation(false)}db_backups/{backupFileModel.Name}";

                    return backupFileModel;
                }),
                Total = backupFiles.Count
            };

            return model;
        }

        /// <summary>
        /// Prepare URL record search model
        /// </summary>
        /// <param name="searchModel">URL record search model</param>
        /// <returns>URL record search model</returns>
        public virtual UrlRecordSearchModel PrepareUrlRecordSearchModel(UrlRecordSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged URL record list model
        /// </summary>
        /// <param name="searchModel">URL record search model</param>
        /// <returns>URL record list model</returns>
        public virtual UrlRecordListModel PrepareUrlRecordListModel(UrlRecordSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get URL records
            var urlRecords = _urlRecordService.GetAllUrlRecords(slug: searchModel.SeName,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //get URL helper
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            //prepare list model
            var model = new UrlRecordListModel
            {
                Data = urlRecords.Select(urlRecord =>
                {
                    //fill in model values from the entity
                    var urlRecordModel = urlRecord.ToModel<UrlRecordModel>();

                    urlRecordModel.Name = urlRecord.Slug;

                    //fill in additional values (not existing in the entity)
                    urlRecordModel.Language = urlRecord.LanguageId == 0
                        ? _localizationService.GetResource("Admin.System.SeNames.Language.Standard")
                        : _languageService.GetLanguageById(urlRecord.LanguageId)?.Name ?? "Unknown";

                    //details URL
                    var detailsUrl = string.Empty;
                    var entityName = urlRecord.EntityName?.ToLowerInvariant() ?? string.Empty;
                    //switch (entityName)
                    //{
                        //case "Type":
                            //detailsUrl = urlHelper.Action("actionName", "controllerName", new { id = urlRecord.EntityId });
                            //break;
                    //}

                    urlRecordModel.DetailsUrl = detailsUrl;

                    return urlRecordModel;
                }),
                Total = urlRecords.TotalCount
            };

            return model;
        }

        /// <summary>
        /// Prepare language selector model
        /// </summary>
        /// <returns>Language selector model</returns>
        public virtual LanguageSelectorModel PrepareLanguageSelectorModel()
        {
            var model = new LanguageSelectorModel
            {
                CurrentLanguage = _workContext.WorkingLanguage.ToModel<LanguageModel>(),
                AvailableLanguages = _languageService
                    .GetAllLanguages()
                    .Select(language => language.ToModel<LanguageModel>()).ToList()
            };

            return model;
        }

        /// <summary>
        /// Prepare popular search term search model
        /// </summary>
        /// <param name="searchModel">Popular search term search model</param>
        /// <returns>Popular search term search model</returns>
        public virtual PopularSearchTermSearchModel PreparePopularSearchTermSearchModel(PopularSearchTermSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.PageSize = 5;
            searchModel.AvailablePageSizes = "5";

            return searchModel;
        }

        /// <summary>
        /// Prepare paged popular search term list model
        /// </summary>
        /// <param name="searchModel">Popular search term search model</param>
        /// <returns>Popular search term list model</returns>
        public virtual PopularSearchTermListModel PreparePopularSearchTermListModel(PopularSearchTermSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get popular search terms
            var searchTermRecordLines = _searchTermService.GetStats(pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare list model
            var model = new PopularSearchTermListModel
            {
                //fill in model values from the entity
                Data = searchTermRecordLines.Select(searchTerm => new PopularSearchTermModel
                {
                    Keyword = searchTerm.Keyword,
                    Count = searchTerm.Count
                }),
                Total = searchTermRecordLines.TotalCount
            };

            return model;
        }

        /// <summary>
        /// Prepare common statistics model
        /// </summary>
        /// <returns>Common statistics model</returns>
        public virtual CommonStatisticsModel PrepareCommonStatisticsModel()
        {
            var model = new CommonStatisticsModel();

            var userRoleIds = new[] { _userService.GetUserRoleBySystemName(NopUserDefaults.RegisteredRoleName).Id };
            model.NumberOfUsers = _userService.GetAllUsers(userRoleIds: userRoleIds,
                pageIndex: 0, pageSize: 1, getOnlyTotalCount: true).TotalCount;
            
            return model;
        }

        #endregion
    }
}