angular.module('Return')
    .controller('Return.helloWorldController', ['$scope', 'Return.webApi', function ($scope, api) {
        var blade = $scope.blade;
        blade.title = 'Return';

        blade.refresh = function () {
            api.get(function (data) {
                blade.title = 'Return.blades.hello-world.title';
                blade.data = data.result;
                blade.isLoading = false;
            });
        };

        blade.refresh();
    }]);
