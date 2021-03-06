'use strict';

/**
 * @ngdoc function
 * @name srcApp.sections
 * @description
 * # sections
 * Returns the list of sections
 */
angular.module('srcApp')
  .factory('missions', function() {
    return function() {
      return [
        { 'Name': '?',
          'StartDate': '1996-01-12',
          'EndDate': '1996-01-14',
          'Location': 'Hesley Wood', },
        { 'Name': '?',
          'StartDate': '1996-02-09',
          'EndDate': '1996-02-11',
          'Location': 'Thriftwood', },
        { 'Name': '?',
          'StartDate': '1996-03-15',
          'EndDate': '1996-03-17',
          'Location': 'Squirrel Wood', },
        { 'Name': '?',
          'StartDate': '1996-04-12',
          'EndDate': '1996-04-14',
          'Location': 'Drum Hill', },
        { 'Name': '?',
          'StartDate': '1996-05-15',
          'EndDate': '1996-05-17',
          'Location': '?', },
        { 'Name': '?',
          'StartDate': '1996-06-14',
          'EndDate': '1997-06-16',
          'Location': 'Bedale', },
        { 'Name': '?',
          'StartDate': '1996-07-19',
          'EndDate': '1997-07-21',
          'Location': 'Ipswich', },

        { 'Name': 'Operation "Sable"',
          'StartDate': '1996-10-19',
          'EndDate': '1996-10-20',
          'Location': 'Hesley Wood', },

        { 'Name': '?',
          'StartDate': '1997-01-24',
          'EndDate': '1997-01-26',
          'Location': 'Thriftwood', },
        { 'Name': '?',
          'StartDate': '1997-02-28',
          'EndDate': '1997-03-02',
          'Location': 'Squirrel Wood', },
        { 'Name': '?',
          'StartDate': '1997-04-04',
          'EndDate': '1997-04-06',
          'Location': 'Drum Hill', },
        { 'Name': '?',
          'StartDate': '1997-05-09',
          'EndDate': '1997-05-11',
          'Location': 'Overstones', },
        { 'Name': '?',
          'StartDate': '1997-09-05',
          'EndDate': '1997-09-07',
          'Location': 'Hesley Wood', },
        { 'Name': '?',
          'StartDate': '1997-10-10',
          'EndDate': '1997-10-12',
          'Location': 'Swuirrel Wood', },
        { 'Name': '?',
          'StartDate': '1997-11-07',
          'EndDate': '1997-11-09',
          'Location': 'Thrift Wood', },
        { 'Name': '?',
          'StartDate': '1997-12-04',
          'EndDate': '1997-12-06',
          'Location': 'Hesley Wood', },
        ]; };
      });
