'use strict';
angular.module('scamp')
.factory('systemSettingsSvc', ['$http', '$q', function ($http, $q) {
    var apiPath = '/api/settings/';
    var apiPathSysAdmins = apiPath + 'sysadmins/';
    var apiPathgrpManagers = apiPath + 'groupmanagers/';
    var apiPathSubscriptions = apiPath + 'subscriptions/';

    return {
        // gets a list of all the SCAMP system admins
        getSysAdmins: function () {
            var deferred = $q.defer();

            $http({ method: 'GET', url: apiPathSysAdmins }).
                success(function (data, status, headers, config) {
                    deferred.resolve(data);
                }).
                error(function (data, status, headers, config) {
                    deferred.reject(status);
                })

            return deferred.promise;
        },

        getGroupManagers: function () {
            console.log("calling: systemSettingsSvc.getGroupManagers");
            var deferred = $q.defer();

            $http({ method: 'GET', url: apiPathgrpManagers }).
                success(function (data, status, headers, config) {
                    deferred.resolve(data);
                }).
                error(function (data, status, headers, config) {
                    deferred.reject(status);
                })

            return deferred.promise;
        },

        // revoke user's SCAMP system admin permissions
        revokeSysAdmin: function (id) {
            console.log("removing system admin permissions on id:" + id);

            var deferred = $q.defer();
           
            $http.delete(apiPathSysAdmins + id).
                success(function (data, status, headers, config) {
                    deferred.resolve(data);
                }).
                error(function (data, status, headers, config) {
                    console.log("error on revokeAdmin");
                    deferred.reject(status, data);
                })

            return deferred.promise;
        },

        // revoke user's SCAMP system admin permissions
        grantSysAdmin: function (id) {
            console.log("adding system admin permissions on id:" + id);

            var deferred = $q.defer();
           
                $http.post(apiPathSysAdmins + id).
                success(function (data, status, headers, config) {
                    deferred.resolve(data);
                }).
                error(function (data, status, headers, config) {
                    deferred.reject(status);
                })

            return deferred.promise;
        },

        getSubscriptions: function () {
            console.log("calling: systemSettingsSvc.getSubscriptions");
            var deferred = $q.defer();

            $http({ method: 'GET', url: apiPathSubscriptions }).
                success(function (data, status, headers, config) {
                    deferred.resolve(data);
                }).
                error(function (data, status, headers, config) {
                    deferred.reject(status);
                })

            return deferred.promise;
        },

        // update subscription
        upsertSubscription: function (subscription) {
            console.log("calling: systemSettingsSvc.upsertSubscription");
            console.log(subscription);

            var deferred = $q.defer();

            $http.put(apiPathSubscriptions, subscription).
            success(function (data, status, headers, config) {
                deferred.resolve(data);
            }).
            error(function (data, status, headers, config) {
                deferred.reject(status);
            })

            return deferred.promise;
        },

        // request deletion of a subscription
        deleteSubscription: function (subscriptionId) {
            console.log("calling: systemSettingsSvc.deleteSubscription");
            console.log(subscriptionId);

            var deferred = $q.defer();

            $http.delete(apiPathSubscriptions + subscriptionId).
            success(function (data, status, headers, config) {
                deferred.resolve(data);
            }).
            error(function (data, status, headers, config) {
                deferred.reject(status);
            })

            return deferred.promise;
        }

    };
}]);