using DocumentDbRepositories;
using Microsoft.AspNet.Mvc;
using ScampApi.Infrastructure;
using ScampTypes.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using ProvisioningLibrary;

namespace ScampApi.Controllers.Controllers
{
    [Authorize]
    [Route("api/settings/groupadmins")]
    public class SettingsGroupAdminsController : Controller
    {
        private readonly ISystemSettingsRepository _settingsRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private ISecurityHelper _securityHelper;
        private static IVolatileStorageController _volatileStorageController;

        public SettingsGroupAdminsController(ISystemSettingsRepository settingsRepository, IUserRepository userRepository, ISecurityHelper securityHelper, IGroupRepository groupRepository, IVolatileStorageController volatileStorageController)
        {
            _settingsRepository = settingsRepository;
            _userRepository = userRepository;
            _groupRepository = groupRepository;
            _securityHelper = securityHelper;
            _volatileStorageController = volatileStorageController;
        }

        /// <summary>
        /// Retrieve a list of group managers
        /// </summary>
        /// <returns>List of users with System Admin permission</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // only system admins can access this functionality
            if (!await _securityHelper.IsSysAdmin())
                return new HttpStatusCodeResult(403); // Forbidden

            List<GroupAdminSummary> rtnView = new List<GroupAdminSummary>();

            // get group managers
            List<ScampUser> mgrList = await _settingsRepository.GetGroupAdmins();

            // build the return set
            foreach(ScampUser usr in mgrList)
            {

                // get groups user controls budget of
                var groupList = await _groupRepository.GetGroupsByBudgetOwner(usr.Id);

                // calculate total amount of user's budget that has been used across all groups
                long totUsed = 0; // initial total used of users allocated budget
                foreach (var group in groupList)
                {
                    // get group's budget state
                    var groupBudget = await _volatileStorageController.GetGroupBudgetState(group.Id);
                    if (groupBudget != null) // if we found budget state
                        totUsed += groupBudget.UnitsUsed;
                }

                // build summary view object
                GroupAdminSummary newAdmin = new GroupAdminSummary()
                {
                    Id = usr.Id,
                    Name = usr.Name,
                    unitsBudgeted = usr.budget.unitsBudgeted,
                    endDate = usr.budget.EndDate,
                    totUnitsUsed = totUsed,
                    totUnitsAllocated = usr.budget.Allocated,
                    totGroups = groupList.Count 
                };
                rtnView.Add(newAdmin);  // add it to the collection
            }

            // return list
            return new ObjectResult(rtnView) { StatusCode = 200 };
        }

        // grant a user system administrator permission
        // [marckup] can't find anyone that calls this
        [HttpPost]
        public async Task<IActionResult> grantGroupManager([FromBody] GroupAdminSummary groupManagerSummary)
        {
            // ensure requestor has system admin permissions
            if (!await _securityHelper.IsSysAdmin())
                return new ObjectResult("Access Denied, requestor is not a system administrator") { StatusCode = 403 };

            ScampUser tmpUser = await _userRepository.GetUserById(groupManagerSummary.Id);
            bool doingAdd = tmpUser == null;

            // if we're doing add operations
            if (doingAdd)
            {
                // build new document
                tmpUser = new ScampUser()
                {
                    Id = groupManagerSummary.Id,
                    Name = groupManagerSummary.Name
                };
            }

            // do validation
            // https://github.com/SimpleCloudManagerProject/SCAMP/issues/196

            // set budget info
            if (tmpUser.budget == null)
                tmpUser.budget = new ScampUserBudget();
            tmpUser.budget.unitsBudgeted = groupManagerSummary.unitsBudgeted;
            tmpUser.budget.EndDate = groupManagerSummary.endDate.Date.AddMinutes(1);

            // save changes
            if (doingAdd) // create user 
                    await _userRepository.CreateUser(tmpUser);
            else // else must be update
                await _userRepository.UpdateUser(tmpUser);

            return new ObjectResult(null) { StatusCode = 204 };
        }

        /// <summary>
        /// revokes group manager permissions
        /// </summary>
        /// <returns>user to remove group manager permissions on</returns>
        [HttpDelete("{userId}")]
        public async Task<IActionResult> Delete(string userId)
        {
            // only system admins can access this functionality
            if (!await _securityHelper.IsSysAdmin())
                return new HttpStatusCodeResult(403); // Forbidden

            // get subscription
            ScampUser tmpUser = await _userRepository.GetUserById(userId);
            if (tmpUser == null) // if not found
                return new ObjectResult("specified group manager does not exist") { StatusCode = 400 };

            //TODO: kick off process to clean up all resources in the managers groups
            // https://github.com/SimpleCloudManagerProject/SCAMP/issues/197

            // mark subscription as deleted
            tmpUser.budget = null;
            await _userRepository.UpdateUser(tmpUser);

            // return success
            return new ObjectResult(null) { StatusCode = 204 };
        }

    }

}
