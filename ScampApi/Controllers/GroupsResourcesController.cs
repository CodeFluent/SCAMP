using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using DocumentDbRepositories;
using DocumentDbRepositories.Implementation;
using Microsoft.AspNet.Mvc;
using ProvisioningLibrary;
using ScampApi.Infrastructure;
using System.IO; 
using Microsoft.AspNet.Http;
using ScampTypes.ViewModels;
using Microsoft.AspNet.Authorization;

namespace ScampApi.Controllers
{
    [Authorize]
    [Route("api/groups/{groupId}/resources")]
    public class GroupResourcesController : Controller
    {
        private ILinkHelper _linkHelper;
        private IResourceRepository _resourceRepository;
        private ISecurityHelper _securityHelper;
        private IGroupRepository _groupRepository;
        private IWebJobController _webJobController;
        private readonly ISystemSettingsRepository _settingsRepository;
        private static IVolatileStorageController _volatileStorageController = null;

        public GroupResourcesController(ILinkHelper linkHelper, ISecurityHelper securityHelper, IResourceRepository resourceRepository, IGroupRepository groupRepository, IWebJobController webJobController, ISystemSettingsRepository settingsRepository, IVolatileStorageController volatileStorageController)
        {
            _linkHelper = linkHelper;
            _resourceRepository = resourceRepository;
            _securityHelper = securityHelper;
            _groupRepository = groupRepository;
            _groupRepository = groupRepository;
            _webJobController = webJobController;
            _settingsRepository = settingsRepository;
            _volatileStorageController = volatileStorageController;
        }

        // allows you to take the specified action (start, stop) on a specified resource
        [HttpGet("{resourceId}/rdp")]
        public async Task<IActionResult> GetRdp(string groupId, string resourceId)
        {
            ScampResource res = await _resourceRepository.GetResource(resourceId);
            if (res == null)
                return new HttpStatusCodeResult(404); // not found

            // can user preform this action
            if (!(await CanManageResource(res, ResourceAction.Undefined)))
                return new HttpStatusCodeResult(403); // request denied, invalid permission

            ScampSubscription sub = await _settingsRepository.GetSubscription(res.SubscriptionId);
            var provisioningController = new ProvisioningController(sub.AzureManagementThumbnail, sub.AzureSubscriptionID);


            //Response.ContentType = "application/x-rdp";
            Response.Headers.Add("content-disposition", new string[] { "attachment; filename =" + res.CloudServiceName + ".rdp" });

            byte[] bytes = await provisioningController.GetRdpAsync(res.Name, res.CloudServiceName);
            var encoding = new System.Text.UTF8Encoding();
            var sRes = encoding.GetString(bytes);
            return new ObjectResult(sRes) { StatusCode = 200 };
        }


        // allows you to take the specified action (start, stop) on a specified resource
        [HttpPost("{resourceId}/{actionname}/{duration:int?}")]
        public async Task<IActionResult> Post(string groupId, string resourceId, string actionname, uint? duration = null)
        {
            ScampResource res = await _resourceRepository.GetResource(resourceId);
            if (res == null)
                return new HttpStatusCodeResult(404); // not found

            ResourceAction action = WebJobController.GetAction(actionname);
            ResourceState newState = ResourceState.Unknown;
            switch (action)
            {
                case ResourceAction.Start:
                    newState = ResourceState.Starting;
                    break;
                case ResourceAction.Stop:
                    newState = ResourceState.Stopping;
                    break;
            }

            if (await CanManageResource(res, action))
            {
                await _volatileStorageController.UpdateResourceState(resourceId, newState);
                _webJobController.SubmitActionInQueue(resourceId, action, duration);
            }

            return new HttpStatusCodeResult(204);
        }

        [HttpPost]
        public async Task<ScampResourceSummary>  Post(string groupId, [FromBody]ScampResourceSummary groupResource)
        {
            // set up resource to be created
            // need some preliminary values for the authorization check
            var grpRef = new ScampResourceGroupReference() { Id = groupId };
            var res = new ScampResource()
            {
                Id = Guid.NewGuid().ToString("d"),
                ResourceGroup = grpRef,
                Name = Regex.Replace(groupResource.Name.ToLowerInvariant(), "[^a-zA-Z0-9]", ""),
                ResourceType = ResourceType.VirtualMachine,
                //State = ResourceState.Allocated
            };

            // can user preform this action
            var checkPermission = await CanManageResource(res, ResourceAction.Create);
            if (!checkPermission)
            {
                //TODO return error
            } 

            await _resourceRepository.CreateResource(res);
            return Mapper.Map<ScampResourceSummary>(res);
        }

        [HttpPut("{resourceId}")]
        public void Put(int groupId, string resourceId, [FromBody]GroupResource value)
        {
            // TODO implement updating a group resource
            throw new NotImplementedException();
        }

        [HttpDelete("{resourceId}")]
        public async Task Delete(string  groupId, string resourceId)
        {
            var res = await _resourceRepository.GetResource(resourceId);
            //TODO what if resource doesn't exist?

            //LINKED TO UI
            var checkPermission = await CanManageResource(res, ResourceAction.Delete);
            if(checkPermission)
            {
                //res.State = ResourceState.Deleting;
                await  _resourceRepository.UpdateResource( res);
                _webJobController.SubmitActionInQueue(resourceId,ResourceAction.Delete );
            }
        }

        // this method will see if the requesting user has permissions to take the action on the 
        // specified resource
        private async Task<bool> CanManageResource(ScampResource resource, ResourceAction action)
        {
            ScampUser currentUser = await _securityHelper.GetOrCreateCurrentUser();

            // System admin can do everything EXCEPT create a resource
            // to create a resource, you must be a group admin
            if (action != ResourceAction.Create && currentUser.IsSystemAdmin) return true; //Sysadmin can do everything

            // Resource owner can also do anything to their resource except create
            var owner = resource.Owners.Find(user => user.Id == currentUser.Id);
            // if current user is in list of resource owners, allow action
            if (action != ResourceAction.Create && owner != null)
                return true;

            // Resource's Group Managers can do anything to the resources in groups
            // they manage
            var rscGroup = currentUser.GroupMembership.Find(grp => grp.Id == resource.ResourceGroup.Id);
            // if current user is a manager of the group that owns the resource, allow action
            if (rscGroup != null && rscGroup.isManager)
                return true;

            // if no positive results, default to false and deny action
            return false;
        }

    }
}