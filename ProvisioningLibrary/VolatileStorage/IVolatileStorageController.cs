﻿using System;
using ProvisioningLibrary;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using System.Collections.Generic;
using ScampTypes.ViewModels;

namespace ProvisioningLibrary
{
    public interface IVolatileStorageController 
    {
        Task<List<ActivityLog>> GetRecentActivityLogForRequest(string requestId);
        Task<List<ActivityLog>> GetRecentActivityLogForResource(string resouceId); 
        Task CreateActivityLog(ActivityLog activityLog);
        Task<ResourceState> GetResourceState(string resourceId);
        Task UpdateResourceState(CurrentResourceState newstate);
        Task UpdateResourceState(string resourceId, ResourceState state);
        Task CreateActivityLog(List<ActivityLog> activityLogs);
    }
}
