
Front End Module: Resource Retrieval for Dashboard

Use Case 1: Get Azure Group Listings
Front-End Module Client: Dashboard
 a. Get group lists for group admin view
    path: /api/groups/admin/:userId
	action: GET
	Expected Response : {
	   groups: [{
		   groupName: {string},
		   groupId: {string},
		   totUnitsUsed: {number},
		   totUnitsAllocated: {number},
		   totUnitsBudgeted: {number}
	   }],
     }
 b. Get group lists for user view
    path: /api/groups/user/:userId
	action: GET
	Expected Response : {
	   groups: [{
		   groupName: {string},
		   groupId: {string},
		   totUnitsUsedByUser: {number},
		   totUnitsRemainingForUser: {number}
	   }],
     }

Use Case 2. Group User Listing
Front-End Module Client: Dashboard
 a. Get list of users for a specified group
    path: /api/group/:groupId/users/
	action: GET
	Expected Response : {
	   users: [{
		   name: {string},
		   Id: {string},
		   totUnitsUsed: {number},
		   totUnitsRemaining: {number}
	   }],
     }

Use Case 3: Get user/student resources
Front-End Module Client: Dashboard
service path: /api/group/:groupId/user/:userId/resources/
action: GET
Expected Response : {
   resources: [{
            Name: {string},
            Id: {string},
            state: {int},
            type: {int},
            totUnitsUsed: {number}
   }]
}

/*FYI, this is still very TBD*/
Use Case 4: Dashboard Usage Summary
Front-End Module Client: Dashboard
a. Get list of user's resource usage
service path: /api/user/:userId/usage/summary
action: GET
Expected Response : {
       unitsBudgeted: {number},/*Will only be visible on the FE for an admin*/
       totUnitsUsed: {number},
	   totGroups: {int}
   }
b. Get summary of user's budget
service path: /api/user/:userId/budget/summary
action: GET
Expected Response : {
       totUnitsAllocated: {number},
       unitsBudgeted: {number},/*Will only be visible on the FE for an admin*/
       totUnitsUsed: {number},
	   totGroups: {int}
   }



Use Case 5: Membership Mgr - Create New Group
Front-End Module Client: Membership Manager
service path: /api/resource/manager/groups/
action: Post
Expected Response : {
       groupName: {string},
       groupOwnerUserId: {string}
       userAdmins: [
                     {
                        userId{string}
                     }
                    ],
       perUserQuota: {number},
       expirationDate: {datetime},
       groupBudget: {number},
       defaultPerUserQuota: {number},
       templates[
                 {
                   templateId: {string}
                 }
                ],
       users: [
                {
                  userId: {string},
                  userQuotaOverrideUnits: {number}
                }
            ]
    }
