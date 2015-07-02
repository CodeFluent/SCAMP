using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ScampApi.Infrastructure;
using ScampTypes.ViewModels;
using DocumentDbRepositories.Implementation;
using DocumentDbRepositories;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using ProvisioningLibrary;
using Microsoft.AspNet.Authorization;

namespace ScampApi.Controllers.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/groups")]
    public class UsersGroupsController : Controller
    {
        private readonly ISecurityHelper _securityHelper;
        private readonly IUserRepository _userRepository;
        private static IVolatileStorageController _volatileStorageController = null;

        public UsersGroupsController(ISecurityHelper securityHelper, IUserRepository userRepository, IVolatileStorageController volatileStorageController)
        {
            _userRepository = userRepository;
            _securityHelper = securityHelper;
            _volatileStorageController = volatileStorageController;
        }

        /// <summary>
        /// returns the specified view of a given user
        /// </summary>
        /// <param name="view"> view to be returned (either "user" or "admin"</param>
        /// <param name="userId">Id of user to get view for</param>
        /// <returns></returns>
        [HttpGet("{view}")]
        public async Task<IActionResult> Get(string userId, string view)
        {
            // get requested user document
            ScampUser userDoc = await _userRepository.GetUserById(userId);
            if (userDoc == null)
                return HttpNotFound();

            //TODO: authorization check

            // build return view
            if (view == "admin") // do admin view
            {
                List<ScampAdminGroupReference> rtnView = new List<ScampAdminGroupReference>();

                // build return view
                foreach (ScampUserGroupMbrship group in userDoc.GroupMembership)
                {
                    if (!(await _securityHelper.CurrentUserCanManageGroup(group.Id))) continue;

                    // get group budget
                    var groupBudget = await _volatileStorageController.GetGroupBudgetState(group.Id);

                    // build return list item
                    ScampAdminGroupReference tmpGroupRef = new ScampAdminGroupReference()
                    {
                        Id = group.Id,
                        Name = group.Name,
                        // be sure to handle user without a budget values
                        totUnitsUsed = (groupBudget == null ? 0 : groupBudget.UnitsUsed),
                        totUnitsAllocated = (groupBudget == null ? 0 : groupBudget.UnitsAllocated),
                        totUnitsBudgeted = (groupBudget == null ? 0 : groupBudget.UnitsBudgetted)
                    };
                    // add item to list
                    rtnView.Add(tmpGroupRef);
                }

                // return results
                return new ObjectResult(rtnView) { StatusCode = 200 };
            }
            else if (view == "user") // do user view
            {
                List<ScampUserGroupReference> rtnView = new List<ScampUserGroupReference>();

                // get user group budgets
                var groupBudgets = await _volatileStorageController.GetUserBudgetStates(userId);

                foreach (ScampUserGroupMbrship group in userDoc.GroupMembership)
                {
                    // get group 
                    var groupBudget = groupBudgets.First(g => g.groupId == group.Id);

                    // build return object
                    ScampUserGroupReference tmpGroupRef = new ScampUserGroupReference()
                    {
                        Id = group.Id,
                        Name = group.Name,
                        // be sure to handle user without a budget values
                        totUnitsUsedByUser = (groupBudget == null ? 0 : groupBudget.UnitsUsed),
                        totUnitsRemainingForUser = (groupBudget == null ? 0 : (groupBudget.UnitsBudgetted - groupBudget.UnitsUsed))
                    };
                    rtnView.Add(tmpGroupRef);
                }

                // return document
                return new ObjectResult(rtnView) { StatusCode = 200 };
            }
            else
            {
                //TODO: invalid argument "view"
            }

            return new ObjectResult(null) { StatusCode = 200 };
        }

    }
}
