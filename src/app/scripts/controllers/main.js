'use strict';

/**
 * @ngdoc function
 * @name srcApp.controller:MainCtrl
 * @description
 * # MainCtrl
 * Controller of the srcApp
 */
angular.module('srcApp')
  .controller('MainCtrl', ['$scope', 'sections', function ($scope, sections) {
    $scope.sections = sections();
  }]);
