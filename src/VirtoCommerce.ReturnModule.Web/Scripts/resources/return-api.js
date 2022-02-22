angular.module('returnModule')
    .factory('Return.webApi', ['$resource', function ($resource) {
        return $resource('api/return', {}, {
            search: { method: 'POST', url: 'api/return/search' }
        });
    }]);
