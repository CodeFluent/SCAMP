using System;
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
using Microsoft.AspNet.Authorization;

namespace ScampApi.Controllers
{
    [Authorize]
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

        [HttpGet(Name = "Groups.GetAll")]
        public async Task<IActionResult> Get()
        {
            ScampUser currentUser = await _securityHelper.GetOrCreateCurrentUser();
            List<GroupSummary> groupSummaries = new List<GroupSummary>();
            foreach (ScampUserGroupMbrship group in currentUser.GroupMembership)
            {
                if (await _securityHelper.CurrentUserCanManageGroup(group.Id))
                {
                    groupSummaries.Add(MapToGroupSummary(group));
                }
            }
            return new ObjectResult(groupSummaries) { StatusCode = 200 };
        }

        /// <summary>
        /// retrieves full details on a single group
        /// </summary>
        /// <param name="groupId">Id of the group to be retrieved</param>
        /// <returns></returns>
        [HttpGet("{groupId}", Name = "Groups.GetSingle")]
        public async Task<IActionResult> Get(string groupId)
        {
            var group = await _groupRepository.GetGroup(groupId);
            if (group == null)
            {
                return HttpNotFound();
            }
            bool userCanViewGroup = await _securityHelper.CurrentUserCanManageGroup(group.Id);
            if (!userCanViewGroup)
            {
                return new HttpStatusCodeResult(403); // Forbidden
            }

            Group returnGroup = Map(group);
            // Fetch the group-specific budget for users in this group
            foreach (UserSummary user in returnGroup.Users)
            {
                UserBudgetState userBudget = await _volatileStorageController.GetUserBudgetState(user.Id, group.Id);
                user.unitsBudgeted = userBudget.UnitsBudgetted;
            }
            return new ObjectResult(returnGroup) { StatusCode = 200 };
        }

        /// <summary>
        /// create a new group
        /// </summary>
        /// <param name="userInputGroup">group object to be created</param>
        /// <returns></returns>
        // creates a new group
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Group userInputGroup)
        {
            // requestor is a group administration and can create groups
            if (!(await _securityHelper.IsGroupAdmin()))
                return new HttpStatusCodeResult(403); // Forbidden;

            //TODO: group parameter validation 

            // get current user
            var currentUser = await _securityHelper.GetOrCreateCurrentUser();

            // build the resource group object
            var group = new ScampResourceGroup()
            {
                Id = Guid.NewGuid().ToString(),
                Name = userInputGroup.Name,
                Description = userInputGroup.Description,
                Budget = new ScampGroupBudget()
                {
                    OwnerId = currentUser.Id,
                    OwnerName = currentUser.Name,
                    unitsBudgeted = userInputGroup.Budget.unitsBudgeted,
                    DefaultUserAllocation = userInputGroup.Budget.defaultUserBudget,
                    EndDate = userInputGroup.Budget.expiryDate
                }
            };

            await _groupRepository.CreateGroup(group);

            // after we know the user docs have completed successfully, add the volatile storage records
            Task[] tasks = new Task[2]; // collection to hold the parallel tasks
            
            // create group volatile storage entries
            var newGrpBudget = new GroupBudgetState(group.Id)
            {
                UnitsBudgetted = userInputGroup.Budget.unitsBudgeted,
                UnitsAllocated = userInputGroup.Budget.defaultUserBudget,
                UnitsUsed = 0
            };
            tasks[0] = _volatileStorageController.AddGroupBudgetState(newGrpBudget);

            // create volatile storage budget entry for user/group
            var newUsrBudget = new UserBudgetState(currentUser.Id, group.Id)
            {
                UnitsBudgetted = group.Budget.DefaultUserAllocation,
                UnitsUsed = 0
            };
            tasks[1] = _volatileStorageController.AddUserBudgetState(newUsrBudget);

            // wait for both operations to complete
            Task.WaitAll(tasks);

            return new ObjectResult(Map(group)) { StatusCode = 200 };
        }

        [HttpPut("{groupId}")]
        public async Task<IActionResult> Put(string groupId, [FromBody]Group value)
        {
            ScampUser currentUser = await _securityHelper.GetOrCreateCurrentUser();
            ScampResourceGroup group = await _groupRepository.GetGroup(groupId);
            // Only the group budget owner can edit the group information
            if (group.Budget.OwnerId == currentUser.Id)
            {
                await _groupRepository.UpdateGroup(groupId, new ScampResourceGroup
                {
                    Members = value.Users.ConvertAll((a => new ScampUserGroupMbrship()
                    {
                        Id = a.Id,
                        Name = a.Name,
                        isManager = a.isManager
                    })),
                    Id = value.Id,
                    Name = value.Name,
                    Description = value.Description,
                    Budget = new ScampGroupBudget()
                    {
                        OwnerId = group.Budget.OwnerId,
                        OwnerName = group.Budget.OwnerName,
                        unitsBudgeted = value.Budget.unitsBudgeted,
                        DefaultUserAllocation = value.Budget.defaultUserBudget,
                        EndDate = value.Budget.expiryDate
                    }
                });

                return new ObjectResult(value) { StatusCode = 200 };
            }
            else
            {
                return new HttpStatusCodeResult(403);
            }
        }

        #region Mapping Functions
        private GroupSummary MapToGroupSummary(ScampUserGroupMbrship groupMembership)
        {
            return new GroupSummary
            {
                Id = groupMembership.Id,
                Name = groupMembership.Name,
            };
        }

        private Group Map(ScampResourceGroup docDbGroup)
        {
            return new Group
            {
                Id = docDbGroup.Id,
                Name = docDbGroup.Name,
                Description = docDbGroup.Description,
                Templates = new List<GroupTemplateSummary>(), // TODO map these when the repo supports them
                Users = docDbGroup.Members?.Select(MapToSummary).ToList(),
                Budget = Map(docDbGroup.Budget)
            };
        }

        private GroupBudget Map(ScampGroupBudget scampGroupBudget)
        {
            return new GroupBudget
            {
                ownerId = scampGroupBudget.OwnerId,
                ownerName = scampGroupBudget.OwnerName,
                unitsBudgeted = scampGroupBudget.unitsBudgeted,
                defaultUserBudget = scampGroupBudget.DefaultUserAllocation,
                expiryDate = scampGroupBudget.EndDate
            };
        }

        private UserSummary MapToSummary(ScampUserGroupMbrship docDbUser)
        {
            return new UserSummary
            {
                Id = docDbUser.Id,
                Name = docDbUser.Name,
                isManager = docDbUser.isManager
            };
        }

        #endregion
    }
}
