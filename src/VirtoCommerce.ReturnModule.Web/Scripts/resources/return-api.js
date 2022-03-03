angular.module('virtoCommerce.returnModule')
    .factory('virtoCommerce.returnModule.returns', ['$resource', function ($resource) {
        return $resource('api/returns', {}, {
            search: { method: 'POST', url: 'api/return/search' },
            update: { method: 'PUT', url: 'api/return' }
        });
    }]);
