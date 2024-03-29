using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Users;

namespace Nop.Services.Authentication.External
{
    /// <summary>
    /// External authentication service
    /// </summary>
    public partial interface IExternalAuthenticationService
    {
        #region External authentication methods

        /// <summary>
        /// Load active external authentication methods
        /// </summary>
        /// <param name="user">Load records allowed only to a specified user; pass null to ignore ACL permissions</param>
        /// <returns>Payment methods</returns>
        IList<IExternalAuthenticationMethod> LoadActiveExternalAuthenticationMethods(User user = null);

        /// <summary>
        /// Load external authentication method by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found external authentication method</returns>
        IExternalAuthenticationMethod LoadExternalAuthenticationMethodBySystemName(string systemName);

        /// <summary>
        /// Load all external authentication methods
        /// </summary>
        /// <param name="user">Load records allowed only to a specified user; pass null to ignore ACL permissions</param>
        /// <returns>External authentication methods</returns>
        IList<IExternalAuthenticationMethod> LoadAllExternalAuthenticationMethods(User user = null);

        /// <summary>
        /// Check whether authentication by the passed external authentication method is available 
        /// </summary>
        /// <param name="systemName">System name of the external authentication method</param>
        /// <returns>True if authentication is available; otherwise false</returns>
        bool ExternalAuthenticationMethodIsAvailable(string systemName);

        /// <summary>
        /// Check whether external authentication method is active
        /// </summary>
        /// <param name="method">External authentication method</param>
        /// <returns>True if method is active; otherwise false</returns>
        bool IsExternalAuthenticationMethodActive(IExternalAuthenticationMethod method);

        #endregion

        #region Authentication

        /// <summary>
        /// Authenticate user by passed parameters
        /// </summary>
        /// <param name="parameters">External authentication parameters</param>
        /// <param name="returnUrl">URL to which the user will return after authentication</param>
        /// <returns>Result of an authentication</returns>
        IActionResult Authenticate(ExternalAuthenticationParameters parameters, string returnUrl = null);

        #endregion

        /// <summary>
        /// Associate external account with user
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="parameters">External authentication parameters</param>
        void AssociateExternalAccountWithUser(User user, ExternalAuthenticationParameters parameters);

        /// <summary>
        /// Get the particular user with specified parameters
        /// </summary>
        /// <param name="parameters">External authentication parameters</param>
        /// <returns>User</returns>
        User GetUserByExternalAuthenticationParameters(ExternalAuthenticationParameters parameters);

        /// <summary>
        /// Remove the association
        /// </summary>
        /// <param name="parameters">External authentication parameters</param>
        void RemoveAssociation(ExternalAuthenticationParameters parameters);

        /// <summary>
        /// Delete the external authentication record
        /// </summary>
        /// <param name="externalAuthenticationRecord">External authentication record</param>
        void DeleteExternalAuthenticationRecord(ExternalAuthenticationRecord externalAuthenticationRecord);
    }
}