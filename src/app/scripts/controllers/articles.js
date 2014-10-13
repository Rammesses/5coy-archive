'use strict';

/**
 * @ngdoc function
 * @name srcApp.controller:ArticlesController
 * @description
 * # ArticlesController
 * Controller of the srcApp
 */
angular.module('srcApp')
  .controller('ArticlesCtrl', ['$scope', '$location', 'sections',
    function ($scope, $location, sections) {

      Array.prototype.findByUrl = function (url) {
            for (var index = 0; index < this.length; index++) {
                if (this[index].Url == url) {
                    return index;
                }
            }

            return -1;
        };

      this.path = $location.path();

      var sections = sections();
      var index = sections.findByUrl(this.path);
      this.section = sections[index];      
  }]);
