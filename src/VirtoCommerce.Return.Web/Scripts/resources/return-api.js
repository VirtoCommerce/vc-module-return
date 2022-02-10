angular.module('Return')
    .factory('Return.webApi', ['$resource', function ($resource) {
        return $resource('api/Return');
}]);
