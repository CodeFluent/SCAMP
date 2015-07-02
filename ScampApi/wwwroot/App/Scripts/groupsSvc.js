'use strict';
angular.module('scamp')
.factory('groupsSvc', ['$http', '$q', function ($http, $q) {
    var apiPath = '/api/groups';
    var makeRequest = function (method, url, data) {
        return scamp.utils.restAjaxPromise($http, $q, method, url, data);
    };
    return {
        getGroups: function () {
            return makeRequest('GET', apiPath);
        },
        getGroup: function (groupId) {
            return makeRequest('GET', apiPath + '/' + groupId);
        },
        addGroup: function (group) {
            return makeRequest('POST', apiPath, group);
        },
        updateGroup: function (group) {
            return makeRequest('PUT', apiPath + '/' + group.id, group);
        },
        removeGroup: function (group) {
            return makeRequest('DELETE', apiPath + '/' + group.id);
        },
        getUsers: function (groupId) {
            return makeRequest('GET', apiPath + '/' + groupId + '/users');
        },
        addUser: function (group, user) {
            var url = apiPath + '/' + group.id + '/users';
            return makeRequest('POST', url, user);
        },
        removeUser: function (group, user) {
            var url = apiPath + '/' + group.id + '/users/' + user.id;
            return makeRequest('DELETE', url);
        },
        updateUser: function (group, user) {
            var url = apiPath + '/' + group.id + '/users/' + user.id;
            return makeRequest('PUT', url, user);
        }
    };
}]);