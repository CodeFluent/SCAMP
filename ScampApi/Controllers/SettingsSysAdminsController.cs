using DocumentDbRepositories;
using Microsoft.AspNet.Mvc;
using ScampApi.Infrastructure;
using ScampTypes.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;

namespace ScampApi.Controllers.Controllers
{
    [Authorize]
    [Route("api/settings/sysadmins")]
    public class SettingsSysAdminController : Controller
    {
        private readonly ISystemSettingsRepository _settingsRepository;
        private readonly IUserRepository _userRepository;
        private ISecurityHelper _securityHelper;

        public SettingsSysAdminController(ISystemSettingsRepository settingsRepository, IUserRepository userRepository, ISecurityHelper securityHelper)
        {
            _settingsRepository = settingsRepository;
            _userRepository = userRepository;
            _securityHelper = securityHelper;
        }

        /// <summary>
        /// Retrieve a list of system administrators
        /// </summary>
        /// <returns>List of users with System Admin permission</returns>
        [HttpGet(Name = "Admin.Get")]
        public async Task<IActionResult> Get()
        {
            // only system admins can access this functionality
            if (!await _securityHelper.IsSysAdmin())
                return new HttpStatusCodeResult(403); // Forbidden

            List<UserSummary> rtnView = new List<UserSummary>();

            // fetch user from database
            List<ScampUser> adminList = await _settingsRepository.GetSystemAdministrators();

            // map data model to view model
            if (adminList != null)
            {
                foreach (ScampUser tmpUser in adminList)
                {
                    rtnView.Add(new UserSummary
                    {
                        Id = tmpUser.Id,
                        Name = tmpUser.Name
                    });
                }
            }

            // return list
            return new ObjectResult(rtnView) { StatusCode = 200 };
        }

        // grant a user system administrator permission
        [HttpPost]
        public async Task<IActionResult> grantAdmin([FromBody] UserSummary usrSummary)
        {
            // ensure requestor has system admin permissions
            if (!await _securityHelper.IsSysAdmin())
                return new ObjectResult("Access Denied, requestor is not a system administrator") { StatusCode = 403 };

            ScampUser tmpUser = await _userRepository.GetUserById(usrSummary.Id);

            // if user wasn't found, add them to the scamp DB
            if (tmpUser == null)
            {
                // build new document
                tmpUser = new ScampUser()
                {
                    Id = usrSummary.Id,
                    Name = usrSummary.Name,
                    IsSystemAdmin = true
                };
                // create user 
                await _userRepository.CreateUser(tmpUser);
            }
            else
            {
                tmpUser.IsSystemAdmin = true;
                await _userRepository.UpdateUser(tmpUser); // save updated setting
            }
            return new ObjectResult(null) { StatusCode = 204 };
        }

        // revoke system administrator permissions for a user
        [HttpDelete("{userId}", Name = "Admin.Revoke")]
        public async Task<IActionResult> revokeAdmin(string userId)
        {
            // check for last remaining admin
            List<ScampUser> adminList = await _settingsRepository.GetSystemAdministrators();
            if (adminList.Count <= 1)
                return new ObjectResult("This would remove the last system admin. There must always be at least 1.") { StatusCode = 403 };

            ScampUser tmpUser = await _userRepository.GetUserById(userId);

            // if user wasn't found, add them to the scamp DB
            if (tmpUser == null)
                return new ObjectResult("Specified user does not exist") { StatusCode = 400 };

            // revoke admin rights
            tmpUser.IsSystemAdmin = false;
            await _userRepository.UpdateUser(tmpUser); // save updated setting
            return new ObjectResult(null) { StatusCode = 204 };
        }
    }
}
