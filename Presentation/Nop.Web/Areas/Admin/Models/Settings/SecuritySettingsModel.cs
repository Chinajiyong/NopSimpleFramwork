﻿using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Settings
{
    /// <summary>
    /// Represents a security settings model
    /// </summary>
    public partial class SecuritySettingsModel : BaseNopModel, ISettingsModel
    {
        #region Properties

        public int ActiveScopeConfiguration { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.EncryptionKey")]
        public string EncryptionKey { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.AdminAreaAllowedIpAddresses")]
        public string AdminAreaAllowedIpAddresses { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.ForceSslForAllPages")]
        public bool ForceSslForAllPages { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.EnableXSRFProtectionForAdminArea")]
        public bool EnableXsrfProtectionForAdminArea { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.EnableXSRFProtectionForPublicSite")]
        public bool EnableXsrfProtectionForPublicSite { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.HoneypotEnabled")]
        public bool HoneypotEnabled { get; set; }

        #endregion
    }
}