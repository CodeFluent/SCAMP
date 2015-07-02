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
    [Route("api/users/{userId}/budget")]
    public class UsersBudgetController : Controller
    {
        private readonly ISecurityHelper _securityHelper;
        private readonly IResourceRepository _resourceRepository;
        private readonly IUserRepository _userRepository;
        private static IVolatileStorageController _volatileStorageController = null;

        public UsersBudgetController(ISecurityHelper securityHelper, IResourceRepository resourceRepository, IUserRepository userRepository, IVolatileStorageController volatileStorageController)
        {
            _resourceRepository = resourceRepository;
            _userRepository = userRepository;
            _securityHelper = securityHelper;
            _volatileStorageController = volatileStorageController;
        }

        /// <summary>
        /// Gets group budget data for a specific user
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

            if (view == "summary")
            {
                UserBudgetSummary tmpBudgetSummary = new UserBudgetSummary();

                foreach(var group in userDoc.GroupMembership)
                {
                    if (group.isManager)
                    {
                        // get this group's current budget
                        var groupBudget = await _volatileStorageController.GetGroupBudgetState(group.Id);

                        tmpBudgetSummary.totGroups++;
                        tmpBudgetSummary.unitsBudgeted += groupBudget.UnitsBudgetted;
                        tmpBudgetSummary.totUnitsUsed += groupBudget.UnitsUsed;
                        tmpBudgetSummary.totUnitsAllocated += groupBudget.UnitsAllocated;
                    }
                    
                };
                
                // return view                
                return new ObjectResult(tmpBudgetSummary) { StatusCode = 200 };
            }
            else
            {
                return new ObjectResult(string.Format("view '{0}' not supported", view)) { StatusCode = 400 };
            }
        }        
	}
}
