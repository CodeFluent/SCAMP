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
    [Route("api/users/{userId}/usage")]
    public class UsersUsageController: Controller
    {
        private readonly ISecurityHelper _securityHelper;
        private readonly IResourceRepository _resourceRepository;
        private readonly IUserRepository _userRepository;
        private static IVolatileStorageController _volatileStorageController = null;

        public UsersUsageController(ISecurityHelper securityHelper, IResourceRepository resourceRepository, IUserRepository userRepository, IVolatileStorageController volatileStorageController)
        {
            _resourceRepository = resourceRepository;
            _userRepository = userRepository;
            _securityHelper = securityHelper;
            _volatileStorageController = volatileStorageController;
        }


        /// <summary>
        /// Gets usage data on a specific user
        /// </summary>
        /// <param name="userId">Id of user being requested</param>
        /// <param name="view">type of view of the data to be returned</param>
        /// <returns>populated view object</returns>
        [HttpGet("{view}")]
        public async Task<IActionResult> Get(string userId, string view)
        {
            //TODO: authorization check

            // get requested user document
            ScampUser userDoc = await _userRepository.GetUserById(userId);
            if (userDoc == null)
                return HttpNotFound();

            // get user usage across all resources
            List<UserBudgetState> usrBudgets = await _volatileStorageController.GetUserBudgetStates(userId);

            if (view == "summary")
            {
                UserUsageSummary tmpUserSummary = new UserUsageSummary()
                {
                    totGroups = userDoc.GroupMembership.Count()
                };

                // summarize resource usage
                foreach (var rscBudget in usrBudgets)
                {
                    tmpUserSummary.unitsBudgeted += rscBudget.UnitsBudgetted;
                    tmpUserSummary.totUnitsUsed += rscBudget.UnitsUsed;
                }

                return new ObjectResult(tmpUserSummary) { StatusCode = 200 };
            }
            else
            {
                return new ObjectResult(string.Format("view '{0}' not supported", view)) { StatusCode = 400 };
            }
        }

    }
}
