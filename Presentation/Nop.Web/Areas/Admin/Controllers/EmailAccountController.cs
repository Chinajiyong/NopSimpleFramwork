﻿using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class EmailAccountController : BaseAdminController
    {
        #region Fields

        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IUserActivityService _userActivityService;
        private readonly IEmailAccountModelFactory _emailAccountModelFactory;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IEmailSender _emailSender;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public EmailAccountController(EmailAccountSettings emailAccountSettings,
            IUserActivityService userActivityService,
            IEmailAccountModelFactory emailAccountModelFactory,
            IEmailAccountService emailAccountService,
            IEmailSender emailSender,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            ISettingService settingService)
        {
            this._emailAccountSettings = emailAccountSettings;
            this._userActivityService = userActivityService;
            this._emailAccountModelFactory = emailAccountModelFactory;
            this._emailAccountService = emailAccountService;
            this._emailSender = emailSender;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._settingService = settingService;
        }

        #endregion

        #region Methods

        public virtual IActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            //prepare model
            var model = _emailAccountModelFactory.PrepareEmailAccountSearchModel(new EmailAccountSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual IActionResult List(EmailAccountSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedKendoGridJson();

            //prepare model
            var model = _emailAccountModelFactory.PrepareEmailAccountListModel(searchModel);

            return Json(model);
        }

        public virtual IActionResult MarkAsDefaultEmail(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            var defaultEmailAccount = _emailAccountService.GetEmailAccountById(id);
            if (defaultEmailAccount == null)
                return RedirectToAction("List");

            _emailAccountSettings.DefaultEmailAccountId = defaultEmailAccount.Id;
            _settingService.SaveSetting(_emailAccountSettings);

            return RedirectToAction("List");
        }

        public virtual IActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            //prepare model
            var model = _emailAccountModelFactory.PrepareEmailAccountModel(new EmailAccountModel(), null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual IActionResult Create(EmailAccountModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var emailAccount = model.ToEntity<EmailAccount>();

                //set password manually
                emailAccount.Password = model.Password;
                _emailAccountService.InsertEmailAccount(emailAccount);

                //activity log
                _userActivityService.InsertActivity("AddNewEmailAccount",
                    string.Format(_localizationService.GetResource("ActivityLog.AddNewEmailAccount"), emailAccount.Id), emailAccount);

                SuccessNotification(_localizationService.GetResource("Admin.Configuration.EmailAccounts.Added"));

                return continueEditing ? RedirectToAction("Edit", new { id = emailAccount.Id }) : RedirectToAction("List");
            }

            //prepare model
            model = _emailAccountModelFactory.PrepareEmailAccountModel(model, null, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        public virtual IActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            //try to get an email account with the specified id
            var emailAccount = _emailAccountService.GetEmailAccountById(id);
            if (emailAccount == null)
                return RedirectToAction("List");

            //prepare model
            var model = _emailAccountModelFactory.PrepareEmailAccountModel(null, emailAccount);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        public virtual IActionResult Edit(EmailAccountModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            //try to get an email account with the specified id
            var emailAccount = _emailAccountService.GetEmailAccountById(model.Id);
            if (emailAccount == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                emailAccount = model.ToEntity(emailAccount);
                _emailAccountService.UpdateEmailAccount(emailAccount);

                //activity log
                _userActivityService.InsertActivity("EditEmailAccount",
                    string.Format(_localizationService.GetResource("ActivityLog.EditEmailAccount"), emailAccount.Id), emailAccount);

                SuccessNotification(_localizationService.GetResource("Admin.Configuration.EmailAccounts.Updated"));

                return continueEditing ? RedirectToAction("Edit", new { id = emailAccount.Id }) : RedirectToAction("List");
            }

            //prepare model
            model = _emailAccountModelFactory.PrepareEmailAccountModel(model, emailAccount, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("changepassword")]
        public virtual IActionResult ChangePassword(EmailAccountModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            //try to get an email account with the specified id
            var emailAccount = _emailAccountService.GetEmailAccountById(model.Id);
            if (emailAccount == null)
                return RedirectToAction("List");

            //do not validate model
            emailAccount.Password = model.Password;
            _emailAccountService.UpdateEmailAccount(emailAccount);

            SuccessNotification(_localizationService.GetResource("Admin.Configuration.EmailAccounts.Fields.Password.PasswordChanged"));

            return RedirectToAction("Edit", new { id = emailAccount.Id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("sendtestemail")]
        public virtual IActionResult SendTestEmail(EmailAccountModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            //try to get an email account with the specified id
            var emailAccount = _emailAccountService.GetEmailAccountById(model.Id);
            if (emailAccount == null)
                return RedirectToAction("List");

            if (!CommonHelper.IsValidEmail(model.SendTestEmailTo))
            {
                ErrorNotification(_localizationService.GetResource("Admin.Common.WrongEmail"), false);
                return View(model);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(model.SendTestEmailTo))
                    throw new NopException("Enter test email address");

                var subject = "Testing email functionality.";
                var body = "Email works fine.";
                _emailSender.SendEmail(emailAccount, subject, body, emailAccount.Email, emailAccount.DisplayName, model.SendTestEmailTo, null);

                SuccessNotification(_localizationService.GetResource("Admin.Configuration.EmailAccounts.SendTestEmail.Success"), false);
            }
            catch (Exception exc)
            {
                ErrorNotification(exc.Message, false);
            }

            //prepare model
            model = _emailAccountModelFactory.PrepareEmailAccountModel(model, emailAccount, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public virtual IActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageEmailAccounts))
                return AccessDeniedView();

            //try to get an email account with the specified id
            var emailAccount = _emailAccountService.GetEmailAccountById(id);
            if (emailAccount == null)
                return RedirectToAction("List");

            try
            {
                _emailAccountService.DeleteEmailAccount(emailAccount);

                //activity log
                _userActivityService.InsertActivity("DeleteEmailAccount",
                    string.Format(_localizationService.GetResource("ActivityLog.DeleteEmailAccount"), emailAccount.Id), emailAccount);

                SuccessNotification(_localizationService.GetResource("Admin.Configuration.EmailAccounts.Deleted"));

                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
                return RedirectToAction("Edit", new { id = emailAccount.Id });
            }
        }

        #endregion
    }
}