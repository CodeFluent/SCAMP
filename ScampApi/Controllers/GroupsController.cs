﻿using System;
using System.Collections.Generic;
using System.Linq;
using R = System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using DocumentDbRepositories;
using DocumentDbRepositories.Implementation;
using Microsoft.AspNet.Mvc;
using ScampApi.Infrastructure;
using ScampTypes.ViewModels;
using ProvisioningLibrary;

namespace ScampApi.Controllers
{
    //[Authorize]
    [Route("api/groups")]
    public class GroupsController : Controller
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISecurityHelper _securityHelper;
        private static IVolatileStorageController _volatileStorageController = null;

        public GroupsController(ISecurityHelper securityHelper, IGroupRepository groupRepository, IUserRepository userRepository, IVolatileStorageController volatileStorageController)
        {
            _groupRepository = groupRepository;
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
        [HttpGet("{view}/{userId}")]    
        public async Task<IActionResult> Get(string view, string userId)
        {
            //TODO: authorization check

            // get requested user document
            ScampUser userDoc = await _securityHelper.GetUserById(userId);
            if (userDoc == null)
                return HttpNotFound();

            // build return view
            if (view == "admin") // do admin view
            {
                List<ScampAdminGroupReference> rtnView = new List<ScampAdminGroupReference>();

                // build return view
                foreach (ScampUserGroupMbrship group in userDoc.GroupMembership)
                {
                    // get group budget
                    var groupBudget = await _volatileStorageController.GetGroupBudgetState(group.Id);

                    // build return list item
                    ScampAdminGroupReference tmpGroupRef = new ScampAdminGroupReference()
                    {
                        Id = group.Id,
                        Name = group.Name,
                        totUnitsUsed = groupBudget.UnitsUsed,
                        totUnitsAllocated = groupBudget.UnitsAllocated,
                        totUnitsBudgeted = groupBudget.UnitsUsed
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
                        totUnitsUsedByUser = groupBudget.UnitsUsed,
                        totUnitsRemainingForUser = (groupBudget.UnitsBudgetted - groupBudget.UnitsUsed)
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

        [HttpGet(Name = "Groups.GetAll")]
        public async Task<IEnumerable<GroupSummary>> Get()
        {
            IEnumerable<ScampResourceGroup> groups;
            //LINKED TO UI
            if (await _securityHelper.IsSysAdmin())
            {
                groups = await _groupRepository.GetGroups();
            }
            else
            {
                groups = await _groupRepository.GetGroupsByUser(await _securityHelper.GetUserReference());
            }
            return groups.Select(MapToSummary);
        }

        [HttpGet("{groupId}", Name = "Groups.GetSingle")]
        public async Task<IActionResult> Get(string groupId)
        {
            var group = await _groupRepository.GetGroupWithResources(groupId);
            if (group == null)
            {
                return HttpNotFound();
            }
            bool userCanViewGroup = await CurrentUserCanViewGroup(group);
            if (!userCanViewGroup)
            {
                return new HttpStatusCodeResult(403); // Forbidden
            }
            return new ObjectResult(Map(group)) { StatusCode = 200 };
        }

        [HttpPost]
        public async Task<GroupSummary> Post([FromBody]Group userInputGroup)
        {
            //Create a group
            if (!await CurrentUserCanCreateGroup()) return null;
            //Cleaning the object
            var group = new ScampResourceGroup()
            {
                Name = R.Regex.Replace(userInputGroup.Name.ToLowerInvariant(), "[^a-zA-Z0-9]", ""),
                Id = Guid.NewGuid().ToString()


            };
            var admin = await _securityHelper.GetUserReference();
            group.Admins.Add(admin);
            await _groupRepository.CreateGroup(group);
            var resp = new GroupSummary()
            {
                Id = group.Id,
                Name = group.Name
            };

            return resp;

        }

        [HttpPut("{groupId}")]
        public async Task<Group> Put(string groupId, [FromBody]Group value)
        {
            if (await _securityHelper.IsGroupAdmin(groupId) || await _securityHelper.IsSysAdmin())
            {
                //// we may need this
                //value.Admins.GroupBy(x => x.UserId).Select(y => y.First());	// remove duplicates
                //value.Members.GroupBy(x => x.UserId).Select(y => y.First());	// remove duplicates

                await _groupRepository.UpdateGroup(groupId, new ScampResourceGroup
                {
                    Admins = value.Admins.ConvertAll((a => new ScampUserReference()
                    {
                        Id = a.Id,
                        Name = a.Name
                    })),
                    Members = value.Members.ConvertAll((a => new ScampUserReference()
                    {
                        Id = a.Id,
                        Name = a.Name
                    })),
                    Id = value.Id,
                    Name = value.Name
                });

                return value;

            }
            return null;
        }

        [HttpDelete("{groupId}")]
        public void Delete(int groupId)
        {
            // TODO implement deleting a group
            throw new NotImplementedException();
        }

        private async Task<bool> CurrentUserCanCreateGroup()
        {
            if (await _securityHelper.IsSysAdmin()) return true;

            //TODO Who else can create a group? Do we need a flag on profile?
            return true;
        }

        private async Task<bool> CurrentUserCanViewGroup(ScampResourceGroupWithResources group)
        {
            var currentUser = await _securityHelper.GetCurrentUser();
            return currentUser.IsSystemAdmin                       // sys admin
                || group.Admins.Any(u => u.Id == currentUser.Id)   // group admin
                || group.Members.Any(u => u.Id == currentUser.Id); // group member
        }

        #region Mapping Functions
        private GroupSummary MapToSummary(ScampResourceGroup docDbGroup)
        {
            return new GroupSummary
            {
                Id = docDbGroup.Id,
                Name = docDbGroup.Name,
            };
        }

        private Group Map(ScampResourceGroupWithResources docDbGroup)
        {
            return new Group
            {
                Id = docDbGroup.Id,
                Name = docDbGroup.Name,
                Templates = new List<GroupTemplateSummary>(), // TODO map these when the repo supports them
                Members = docDbGroup.Members?.Select(MapToSummary).ToList(),
                Admins = docDbGroup.Admins?.Select(MapToSummary).ToList()
            };
        }
        private UserSummary MapToSummary(ScampUserReference docDbUser)
        {
            return new UserSummary
            {
                Id = docDbUser.Id,
                Name = docDbUser.Name,
            };
        }

        #endregion
    }
}
