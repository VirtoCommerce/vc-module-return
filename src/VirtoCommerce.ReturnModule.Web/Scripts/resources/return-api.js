angular.module('virtoCommerce.returnModule')
    .factory('virtoCommerce.returnModule.returns', ['$resource', ($resource) => 
        $resource('api/return/:id', { id: "@Id" }, {
            search: { method: 'POST', url: 'api/return/search' },
            update: { method: 'PUT', url: 'api/return', isArray: false },
            availableQuantities: { method: 'GET', url: 'api/return/available-quantities/:id' }
        })
    ]);
