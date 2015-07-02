using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using ScampApi.Infrastructure;
using ScampTypes.ViewModels;
using System.Threading.Tasks;
using DocumentDbRepositories;
using System.Linq;
using ProvisioningLibrary;
using Microsoft.AspNet.Authorization;

namespace ScampApi.Controllers
{
    [Authorize]
    [Route("api/groups/{groupId}/users")]
    public class GroupsUsersController : Controller
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISecurityHelper _securityHelper;
        private static IVolatileStorageController _volatileStorageController = null;
        private IWebJobController _webJobController;

        public GroupsUsersController(ISecurityHelper securityHelper, IGroupRepository groupRepository, IUserRepository userRepository, IVolatileStorageController volatileStorageController, IWebJobController webJobController)
        {
            _groupRepository = groupRepository;
            _userRepository = userRepository;
            _securityHelper = securityHelper;
            _volatileStorageController = volatileStorageController;
            _webJobController = webJobController;
        }

        /// <summary>
        /// returns a view of a group's information
        /// </summary>
        /// <param name="groupId">Id of group to get list of users for</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Get(string groupId)
        {
            //TODO: add in group admin/manager authorization check
            //if (!await CurrentUserCanViewGroup(group))
            //    return new HttpStatusCodeResult(403); // Forbidden
            //}

            // get group details
            var group = await _groupRepository.GetGroup(groupId);
            if (group == null)
            {
                return HttpNotFound();
            }

            // build return view
            List<UserGroupSummary> rtnView = new List<UserGroupSummary>();

            foreach (ScampUserGroupMbrship userRef in group.Members)
            {
                // get user budget for this group
                var groupBudget = await _volatileStorageController.GetUserBudgetState(userRef.Id, group.Id);

                // build summary item for return
                UserGroupSummary tmpSummary = new UserGroupSummary()
                {
                    Id = userRef.Id,
                    Name = userRef.Name,
                    isManager = userRef.isManager,
                    // be sure to handle user without a budget values
                    totUnitsUsed = (groupBudget == null ? 0 : groupBudget.UnitsUsed),
                    totUnitsRemaining = (groupBudget == null ? 0 : (groupBudget.UnitsBudgetted - groupBudget.UnitsUsed))
                };
                rtnView.Add(tmpSummary); // add item to list
            }

            // return list
            return new ObjectResult(rtnView) { StatusCode = 200 };

        }

        /// <summary>
        /// adds a user to a group
        /// </summary>
        /// <param name="groupId">Id of group to add user to</param>
        /// <returns></returns>
        [HttpPost()]
        public async Task<IActionResult> AddUserToGroup(string groupId, [FromBody] UserSummary newUser)
        {
            if (!await _securityHelper.CurrentUserCanManageGroup(groupId)) {
                return new HttpStatusCodeResult(403); // Forbidden
            }

            string userId = newUser.Id;

            // get group details
            var rscGroup = await _groupRepository.GetGroup(groupId);
            if (rscGroup == null)
            {
                return new ObjectResult("designated group does not exist") { StatusCode = 400 };
            }

            // make sure user isn't already in group
            IEnumerable<ScampUserGroupMbrship> userList = from ur in rscGroup.Members
                                                          where ur.Id == userId
                                                          select ur;
            if (userList.Count() > 0) // user is already in the list
                return new ObjectResult("designated user is already a member of specified group") { StatusCode = 400 };

            // create the user if they don't exist
            //TODO: https://github.com/SimpleCloudManagerProject/SCAMP/issues/247
            if (!(await _userRepository.UserExists(userId)))
            {
                // build user object
                var tmpUser = new ScampUser(newUser);

                // insert into database   
                await _userRepository.CreateUser(tmpUser);
            }

            //TODO: Issue #152
            // check to make sure enough remains in the group allocation to allow add of user

            // create volatile storage budget entry for user
            var newBudget = new UserBudgetState(userId, groupId)
            {
                //TODO: Take into account the budget potentially sent in POST body
                UnitsBudgetted = rscGroup.Budget.DefaultUserAllocation,
                UnitsUsed = 0
            };
            await _volatileStorageController.AddUserBudgetState(newBudget);
            newUser.unitsBudgeted = newBudget.UnitsBudgetted;

            // create document updates
            await _groupRepository.AddUserToGroup(groupId, userId, false);

            //TODO: Issue #152
            // update group budget allocation to reflect addition of new user


            // return list
            return new ObjectResult(newUser) { StatusCode = 200 };
        }

        /// <summary>
        /// updates a user within a group
        /// </summary>
        /// <param name="groupId">Id of group to within which to update user</param>
        /// <param name="userId">Id of user</param>
        /// <returns></returns>
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUserInGroup(string groupId, [FromBody] UserSummary newUserSummary)
        {
            if (!await _securityHelper.CurrentUserCanEditGroupUsers()) {
                return new HttpStatusCodeResult(403); // Forbidden
            }

            // get group details
            var rscGroup = await _groupRepository.GetGroup(groupId);
            if (rscGroup == null)
                return new ObjectResult("designated group does not exist") { StatusCode = 400 };

            // make sure user is in group
            IEnumerable<ScampUserGroupMbrship> userList = from ur in rscGroup.Members
                                                          where ur.Id == newUserSummary.Id
                                                          select ur;
            if (userList.Count() == 0) // user is not in the list
                return new ObjectResult("designated user is not in group") { StatusCode = 400 };

            //TODO: Issue #152
            // check to make sure enough remains in the group allocation to handle the new allocation

            // update document
            await _groupRepository.UpdateUserInGroup(groupId, newUserSummary.Id, newUserSummary.isManager);

            // update volatile storage budget entry for user
            await _volatileStorageController.UpdateUserBudgetAllocation(newUserSummary.Id, groupId, newUserSummary.unitsBudgeted);

            return new ObjectResult(null) { StatusCode = 200 };
        }

        /// <summary>
        /// get the list of resources for the specified user and group
        /// </summary>
        /// <param name="groupId">group to check</param>
        /// <param name="userId">user to check</param>
        /// <returns>list of resources as a collection of ScampResourceSummary objects </returns>
        [HttpGet("{userId}/resources")]
        public async Task<IActionResult> Get(string groupId, string userId)
        {
            //TODO: add in group admin/manager authorization check
            //if (!await CurrentUserCanViewGroup(group))
            //    return new HttpStatusCodeResult(403); // Forbidden
            //}

            // get group details
            var tmpUser = await _userRepository.GetUserById(userId);
            if (tmpUser == null) // group not found, return appropriately
                return HttpNotFound();

            ScampUserGroupMbrship tmpGroup = tmpUser.GroupMembership.FirstOrDefault(g => g.Id == groupId);
            if (tmpGroup == null) // user not found in group, return appropriately
                return new HttpStatusCodeResult(204); // nothing found

            // build return view
            List<ScampResourceSummary> rtnView = new List<ScampResourceSummary>();

            foreach (ScampUserGroupResources resourceRef in tmpGroup.Resources)
            {
                // get resource usage
                var rscState = await _volatileStorageController.GetResourceState(resourceRef.Id);

                ScampResourceSummary tmpSummary = new ScampResourceSummary()
                {
                    Id = resourceRef.Id,
                    Name = resourceRef.Name,
                    State = rscState.State,
                    totUnitsUsed = rscState.UnitsUsed
                };
                rtnView.Add(tmpSummary);
            }

            return new ObjectResult(rtnView) { StatusCode = 200 };
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveUserFromGroup(string groupId, string userId)
        {
            if (!await _securityHelper.CurrentUserCanManageGroup(groupId))
            {
                return new HttpStatusCodeResult(403); // Forbidden
            }

            var requestingUser = await _securityHelper.GetOrCreateCurrentUser();
            // don't allow user to remove themselves
            if (requestingUser.Id == userId)
                return new ObjectResult("User cannot remove themselves") { StatusCode = 403 };

            // remove the user from the group
            await _groupRepository.RemoveUserFromGroup(groupId, userId);

            // get list of user resources in this group
            IEnumerable<ScampUserGroupResources> resources = await _groupRepository.GetGroupMemberResources(groupId, userId);
            foreach(ScampUserGroupResources resource in resources)
            {
                // request deprovisioning of user resources
                // this will delete the resource entries and update the group usage
                _webJobController.SubmitActionInQueue(resource.Id, ResourceAction.Delete);
            }

            // remove the user's budget entry for the group from the volatile store
            await _volatileStorageController.DeleteUserBudgetState(userId, groupId);

            return new ObjectResult(null) { StatusCode = 200 };
        }
    }
}