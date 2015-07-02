'use strict';
angular.module('scamp')
.controller('groupManagerCtrl', ['$scope', 'userSvc', 'groupsSvc', function ($scope, userSvc, groupsSvc) {
    $scope.viewLoading = true;

    // A variable to store group information locally.
    // TODO: Currently $scope.selectedGroup and $scope.groups
    // are referencing to different objects, which is not conventient
    // when dealing with the local groups. The selected group should
    // point to a group object within the list of groups.
    $scope.groups = [];
    $scope.selectedGroup = null;
    var removeLocalGroup = function (groupId) {
        $scope.groups = $scope.groups.filter(function (value) {
            return value.id !== groupId;
        });
    };
    var updateLocalGroup = function (group) {
        for (var i = 0; i < $scope.groups.length; i++) {
            if ($scope.groups[i].id === group.id) {
                $scope.groups[i] = group;
                return;
            }
        }
    };

    var removeLocalGroupUser = function (userId) {
        $scope.selectedGroup.users = $scope.selectedGroup.users.filter(function (value) {
            return value.id !== userId;
        });
    }

    var loadGroupDetails = function (group) {
        $scope.groupDetailsLoading = true;
        groupsSvc.getGroup(group.id)
        .then(function (response) {
            $scope.selectedGroup = response;
            $scope.selectedGroup.disableEdit = ($scope.selectedGroup.budget.ownerId != $scope.userProfile.id);
            $('#summary-tab-link').tab('show');
        })
        .finally(function () {
            $scope.groupDetailsLoading = false;
        });
    };

    $scope.groupSelected = function ($event, group) {
        $scope.selectedGroup = group;
        $scope.groupDetailsLoading = true;
        loadGroupDetails(group);
    };

    $scope.onlyNumbersPattern = /^\d+$/;
    // Set the minimun expiry date for today
    $scope.groupExpiryMinDate = new Date();
    $scope.groupExpiryDateOpened = false;
    $scope.groupExpiryDateOpen = function ($event) {
        $event.preventDefault();
        $event.stopPropagation();

        $scope.groupExpiryDateOpened = true;
    };

    $scope.groupSummaryFormSubmitted = function () {
        $scope.groupDetailsLoading = true;
        if ($scope.selectedGroupUnsaved) {
            groupsSvc.addGroup($scope.selectedGroup)
            .then(function (response) {
                $scope.groups.unshift(response);
                $scope.selectedGroup = response;
                $scope.selectedGroupUnsaved = false;
            })
            .finally(function () {
                $scope.groupDetailsLoading = false;
            });
        } else {
            groupsSvc.updateGroup($scope.selectedGroup)
            .then(function (response) {
                updateLocalGroup(response);
                $scope.selectedGroup = response;
            })
            .finally(function () {
                $scope.groupDetailsLoading = false;
            });
        }
    };

    $scope.addGroup = function () {
        $scope.selectedGroupUnsaved = true;
        $scope.selectedGroup = {
            'id': 'temporary-id-of-unsaved-group',
            'budget': {
                'ownerName': $scope.userProfile.name
            }
        };
        $('#summary-tab-link').tab('show');
    };

    $scope.removeGroup = function () {
        if ($scope.selectedGroupUnsaved) {
            if ($scope.groups.length > 0) {
                $scope.selectedGroup = $scope.groups[0];
                loadGroupDetails($scope.groups[0]);
            } else {
                $scope.selectedGroup = null;
            }
            $scope.selectedGroupUnsaved = false;
        } else {
            $scope.groupDetailsLoading = true;
            groupsSvc.removeGroup($scope.selectedGroup)
            .then(function () {
                removeLocalGroup($scope.selectedGroup.id);
                if ($scope.groups.length > 0) {
                    $scope.selectedGroup = $scope.groups[0];
                    loadGroupDetails($scope.selectedGroup);
                } else {
                    $scope.selectedGroup = null;
                }
            })
            .finally(function () {
                $scope.groupDetailsLoading = false;
            });
        }
    };

    $scope.userFilter = {};
    $scope.userFilter.keyword = '';
    $scope.userFilter.filter = function () {
        var filterExpression = new RegExp($scope.userFilter.keyword, 'i');
        $('.filtered-data tr')
        .hide()
        .filter(function () {
            return filterExpression.test($(this).text());
        })
        .show();
    };
    $scope.userFilter.clear = function () {
        $scope.userFilter.keyword = '';
        $('.filtered-data tr').show();
    };

    $scope.addUser = {};
    $scope.addUser.searchUsers = function (keyword) {
        return userSvc.searchUsers(keyword)
        .then(function (response) {
            return response;
        });
    };
    $scope.addUser.userSelected = function ($item, $model, $label) {
        $scope.addUser.selectedUser = $item;
    };
    $scope.addUser.add = function () {
        $scope.addUser.loading = true;
        groupsSvc.addUser($scope.selectedGroup, $scope.addUser.selectedUser)
        .then(function (response) {
            $scope.selectedGroup.users.unshift(response);
        })
        .finally(function () {
            $scope.addUser.selectedUserName = '';
            $scope.addUser.loading = false;
        });
    };

    $scope.removeUser = function (user) {
        user.loading = true;
        groupsSvc.removeUser($scope.selectedGroup, user)
        .then(function () {
            removeLocalGroupUser(user.id);
        })
        .finally(function() {
            user.loading = false;
        });
    };
    $scope.updateUser = function (user) {
        user.loading = true;
        groupsSvc.updateUser($scope.selectedGroup, user)
        .then(function (response) {
            // Here we are not updating the local user object
            // because after a successful update, the data
            // in the server should match to what was sent.
        })
        .finally(function() {
            user.loading = false;
        });
    };

    var getGroups = function () {
        groupsSvc.getGroups()
        .then(function (response) {
            if (response.constructor === Array && response.length > 0) {
                $scope.groups = response;
                $scope.selectedGroup = response[0];
                loadGroupDetails($scope.selectedGroup);
            }
        })
        .finally(function () {
            $scope.viewLoading = false;
        });
    };
    if ($scope.userProfile) {
        getGroups();
    } else {
        $scope.$on('userProfileFetched', getGroups);
    }
}]);