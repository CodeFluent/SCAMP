'use strict';
angular.module('scamp')
.controller('homeCtrl', ['$scope', 'adalAuthenticationService', '$location', 'homeSvc', function ($scope, adalService, $location, homeSvc) {
    $scope.currentRouteName = 'home';
    $scope.login = function () {
        adalService.login();
    };
    $scope.logout = function () {
        adalService.logOut();
    };

    $scope.isActive = function (viewLocation) {        
        return viewLocation === $location.path();
    };

    // Flag to avoid multiple parallel fetches
    var fetchingUserProfile = false;
    var fetchUserProfile = function () {
      if (fetchingUserProfile) return;
      console.log("getting user profile data");

      // fetch user profile data from API
      fetchingUserProfile = true;
      homeSvc.getUserProfile().success(function (results) {
        console.log("get successfull");
        $scope.userProfile = results;
        $scope.loadingMessage = "";
        console.log($scope.userProfile);
        $scope.$broadcast('userProfileFetched');
        fetchingUserProfile = false;
      }).error(function (err) {
        console.log("get failed");
        $scope.error = err;
        $scope.loadingMessage = "";
        console.log("error:" + err);
        fetchingUserProfile = false;
      });
    };

    $scope.isLoggedOn = adalService.userInfo.isAuthenticated;

    var checkUserLogin = function () {
      if ($scope.isLoggedOn) {
          if ($location.$$path == '/Home') {
              $location.path("/dashboard");
          }
          fetchUserProfile();
      }
      else {
        $scope.$on('adal:loginSuccess', function () {
            if ($location.$$path == '/Home') {
                // After successfull login, redirect to the dashboard view.
                // Switch to dashboard already before fetching the user profile
                // to avoid the view flickering before the fetch completes.
                $location.path("/dashboard");
            }
            $scope.isLoggedOn = adalService.userInfo.isAuthenticated;
            fetchUserProfile();
        });
      }
    };
    checkUserLogin();
    // Subscribe to route changes and if we are entering the home view again,
    // go ahead and check the user login as if we would have entered the view
    // for the first time.
    $scope.$on('$routeChangeSuccess', function (event, next, current) {
      if (next.loadedTemplateUrl === '/App/Views/Home.html') {
        checkUserLogin();
      }
    });
}]);